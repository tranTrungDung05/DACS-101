using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using DACS.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace DACS.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UserController : Controller
    {
        private readonly ApplicationDbContext _context;

        public UserController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: User
        public async Task<IActionResult> Index()
        {
            var users = await _context.TaiKhoans
                .Include(u => u.ChucVu)
                .ToListAsync();
            return View(users);
        }

        // GET: User/Create
        public async Task<IActionResult> Create()
        {
            ViewBag.IdChucVu = new SelectList(await _context.ChucVus.ToListAsync(), "IdChucVu", "TenChucVu");
            return View();
        }

        // POST: User/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TaiKhoan user)
        {
            if (ModelState.IsValid)
            {
                // Kiểm tra tên đăng nhập tồn tại
                if (await _context.TaiKhoans.AnyAsync(u => u.TenDangNhap == user.TenDangNhap))
                {
                    ModelState.AddModelError("TenDangNhap", "Tên đăng nhập đã tồn tại.");
                    ViewBag.IdChucVu = new SelectList(await _context.ChucVus.ToListAsync(), "IdChucVu", "TenChucVu", user.IdChucVu);
                    return View(user);
                }

                _context.Add(user);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Thêm người dùng thành công!";
                return RedirectToAction(nameof(Index));
            }
            ViewBag.IdChucVu = new SelectList(await _context.ChucVus.ToListAsync(), "IdChucVu", "TenChucVu", user.IdChucVu);
            return View(user);
        }

        // GET: User/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var user = await _context.TaiKhoans.FindAsync(id);
            if (user == null) return NotFound();

            ViewBag.IdChucVu = new SelectList(await _context.ChucVus.ToListAsync(), "IdChucVu", "TenChucVu", user.IdChucVu);
            return View(user);
        }

        // POST: User/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, TaiKhoan user)
        {
            if (id != user.IdTaiKhoan) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(user);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Cập nhật người dùng thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UserExists(user.IdTaiKhoan)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            ViewBag.IdChucVu = new SelectList(await _context.ChucVus.ToListAsync(), "IdChucVu", "TenChucVu", user.IdChucVu);
            return View(user);
        }

        // POST: User/Delete/5
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _context.TaiKhoans.FindAsync(id);
            if (user != null)
            {
                // Không cho phép xóa chính mình hoặc admin gốc (optional logic)
                _context.TaiKhoans.Remove(user);
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            return Json(new { success = false });
        }

        private bool UserExists(int id)
        {
            return _context.TaiKhoans.Any(e => e.IdTaiKhoan == id);
        }
    }
}
