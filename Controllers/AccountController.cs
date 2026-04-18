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

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return RedirectToAction("Login");

            var user = await _context.TaiKhoans
                .Include(t => t.ChucVu)
                .FirstOrDefaultAsync(u => u.IdTaiKhoan == int.Parse(userId));

            if (user == null) return NotFound();

            return View(user);
        }

        [HttpGet]
        public IActionResult ChangePassword()
        {
            if (User.Identity?.IsAuthenticated != true) return RedirectToAction("Login");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProfile(string hoTen, string email)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return RedirectToAction("Login");

            var user = await _context.TaiKhoans.FindAsync(int.Parse(userId));
            if (user == null) return NotFound();

            user.HoTen = hoTen;
            user.Email = email;
            await _context.SaveChangesAsync();

            // Update claims if needed (FullName claim)
            // Note: For simplicity, we'll just redirect and the user will see updated data from DB next time they load Profile.
            // But usually we'd want to refresh the identity to update the claim immediately if we use it in Layout.
            
            TempData["SuccessMessage"] = "Cập nhật thông tin thành công!";
            return RedirectToAction("Profile");
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword(string oldPassword, string newPassword)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return RedirectToAction("Login");

            var user = await _context.TaiKhoans.FindAsync(int.Parse(userId));
            if (user == null) return NotFound();

            if (user.MatKhau != oldPassword)
            {
                TempData["ErrorMessage"] = "Mật khẩu hiện tại không đúng!";
                return View();
            }

            user.MatKhau = newPassword;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đổi mật khẩu thành công!";
            return RedirectToAction("Profile");
        }
    }
}
