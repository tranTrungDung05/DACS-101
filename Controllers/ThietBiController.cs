using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using DACS.Models;

namespace DACS.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ThietBiController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ThietBiController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: ThietBi
        public async Task<IActionResult> Index()
        {
            var devices = await _context.ThietBiGPS
                .OrderByDescending(d => d.IdThietBi)
                .ToListAsync();
            return View(devices);
        }

        // GET: ThietBi/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: ThietBi/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ThietBiGPS thietBi)
        {
            if (ModelState.IsValid)
            {
                // Kiểm tra IMEI trùng
                if (await _context.ThietBiGPS.AnyAsync(t => t.MaImei == thietBi.MaImei))
                {
                    ModelState.AddModelError("MaImei", "Mã IMEI này đã tồn tại trong hệ thống.");
                    return View(thietBi);
                }

                _context.Add(thietBi);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Thêm thiết bị mới thành công!";
                return RedirectToAction(nameof(Index));
            }
            return View(thietBi);
        }

        // GET: ThietBi/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var thietBi = await _context.ThietBiGPS.FindAsync(id);
            if (thietBi == null) return NotFound();

            return View(thietBi);
        }

        // POST: ThietBi/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ThietBiGPS thietBi)
        {
            if (id != thietBi.IdThietBi) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    // Kiểm tra IMEI trùng (trường hợp đổi IMEI)
                    if (await _context.ThietBiGPS.AnyAsync(t => t.MaImei == thietBi.MaImei && t.IdThietBi != id))
                    {
                        ModelState.AddModelError("MaImei", "Mã IMEI này đã tồn tại trong hệ thống.");
                        return View(thietBi);
                    }

                    _context.Update(thietBi);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Cập nhật thông tin thiết bị thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ThietBiExists(thietBi.IdThietBi)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(thietBi);
        }

        // POST: ThietBi/Delete/5
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var thietBi = await _context.ThietBiGPS
                .Include(t => t.PhuongTiens)
                .FirstOrDefaultAsync(t => t.IdThietBi == id);
                
            if (thietBi != null)
            {
                // Kiểm tra xem thiết bị có đang gắn vào xe nào không
                if (thietBi.PhuongTiens.Any())
                {
                    return Json(new { success = false, message = "Không thể xóa thiết bị đang được gắn vào phương tiện!" });
                }

                _context.ThietBiGPS.Remove(thietBi);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Xóa thiết bị thành công!" });
            }
            return Json(new { success = false, message = "Không tìm thấy thiết bị!" });
        }

        private bool ThietBiExists(int id)
        {
            return _context.ThietBiGPS.Any(e => e.IdThietBi == id);
        }
    }
}
