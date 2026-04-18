using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using DACS.Models;
using System.Threading.Tasks;

namespace DACS.Controllers
{
    [Authorize(Roles = "Admin,Manager")]
    public class KhachHangController : Controller
    {
        private readonly ApplicationDbContext _context;

        public KhachHangController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: KhachHang
        public async Task<IActionResult> Index()
        {
            var customers = await _context.KhachHangs
                .OrderByDescending(k => k.MaCccd)
                .ToListAsync();
            return View(customers);
        }

        // GET: KhachHang/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: KhachHang/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(KhachHang khachHang)
        {
            if (ModelState.IsValid)
            {
                // Kiểm tra CCCD trùng
                if (await _context.KhachHangs.AnyAsync(k => k.MaCccd == khachHang.MaCccd))
                {
                    ModelState.AddModelError("MaCccd", "Mã CCCD này đã tồn tại trong hệ thống.");
                    return View(khachHang);
                }

                _context.Add(khachHang);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Thêm khách hàng mới thành công!";
                return RedirectToAction(nameof(Index));
            }
            return View(khachHang);
        }

        // GET: KhachHang/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            var khachHang = await _context.KhachHangs.FindAsync(id);
            if (khachHang == null) return NotFound();

            return View(khachHang);
        }

        // POST: KhachHang/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, KhachHang khachHang)
        {
            if (id != khachHang.MaCccd) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(khachHang);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Cập nhật thông tin khách hàng thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!KhachHangExists(khachHang.MaCccd)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(khachHang);
        }

        // POST: KhachHang/Delete/5
        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            var khachHang = await _context.KhachHangs
                .Include(k => k.HopDongs)
                .FirstOrDefaultAsync(k => k.MaCccd == id);

            if (khachHang != null)
            {
                // Kiểm tra xem khách hàng có hợp đồng nào không
                if (khachHang.HopDongs.Any())
                {
                    return Json(new { success = false, message = "Không thể xóa khách hàng đang có hợp đồng!" });
                }

                _context.KhachHangs.Remove(khachHang);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Xóa khách hàng thành công!" });
            }
            return Json(new { success = false, message = "Không tìm thấy khách hàng!" });
        }

        private bool KhachHangExists(string id)
        {
            return _context.KhachHangs.Any(e => e.MaCccd == id);
        }
    }
}
