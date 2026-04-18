using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using DACS.Models;
using Microsoft.EntityFrameworkCore;

namespace DACS.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }
            return View("~/Views/Home/Login.cshtml");
        }

        [HttpPost]
        public async Task<IActionResult> Login(string username, string password)
        {
            var user = await _context.TaiKhoans
                .Include(t => t.ChucVu)
                .FirstOrDefaultAsync(u => u.TenDangNhap == username && u.MatKhau == password);

            if (user != null)
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.TenDangNhap),
                    new Claim(ClaimTypes.NameIdentifier, user.IdTaiKhoan.ToString()),
                    new Claim("FullName", user.HoTen),
                    new Claim(ClaimTypes.Role, user.ChucVu.TenChucVu)
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                return RedirectToAction("Index", "Home");
            }

            TempData["ErrorMessage"] = "Tên đăng nhập hoặc mật khẩu không đúng!";
            return View("~/Views/Home/Login.cshtml");
        }

        [HttpPost]
        public async Task<IActionResult> Register(string fullname, string email, string newUsername, string newPassword)
        {
            // Kiểm tra xem username đã tồn tại chưa
            if (await _context.TaiKhoans.AnyAsync(u => u.TenDangNhap == newUsername))
            {
                TempData["ErrorMessage"] = "Tên đăng nhập đã tồn tại!";
                return View("~/Views/Home/Login.cshtml");
            }

            // Mặc định gán IdChucVu = 1 (hoặc lấy ID của role Nhân viên/Người dùng nếu có)
            // Trong thực tế cần kiểm tra bảng ChucVu trước
            var activeRole = await _context.ChucVus.FirstOrDefaultAsync() ?? new ChucVu { TenChucVu = "User" };
            
            var newUser = new TaiKhoan
            {
                HoTen = fullname,
                Email = email,
                TenDangNhap = newUsername,
                MatKhau = newPassword, // Lưu ý: Trong thực tế cần Hash mật khẩu
                IdChucVu = activeRole.IdChucVu
            };

            _context.TaiKhoans.Add(newUser);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đăng ký tài khoản thành công! Hãy đăng nhập.";
            return View("~/Views/Home/Login.cshtml");
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }
    }
}
