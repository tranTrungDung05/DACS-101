using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using DACS.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace DACS.Controllers
{
    [Authorize(Roles = "Admin")]
    public class PhuongTienController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PhuongTienController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: PhuongTien
        public async Task<IActionResult> Index()
        {
            var vehicles = await _context.PhuongTiens
                .Include(p => p.ThietBiGPS)
                .OrderByDescending(p => p.IdPhuongTien)
                .ToListAsync();
            return View(vehicles);
        }

        // GET: PhuongTien/Create
        public async Task<IActionResult> Create()
        {
            var availableGps = await _context.ThietBiGPS
                .Where(t => !t.PhuongTiens.Any())
                .ToListAsync();
            ViewBag.IdThietBi = new SelectList(availableGps, "IdThietBi", "MaImei");
            return View();
        }

        // POST: PhuongTien/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PhuongTien vehicle)
        {
            if (ModelState.IsValid)
            {
                // Kiểm tra biển số trùng
                if (await _context.PhuongTiens.AnyAsync(p => p.BienSo == vehicle.BienSo))
                {
                    ModelState.AddModelError("BienSo", "Biển số xe này đã tồn tại trong hệ thống.");
                    var availableGpsOnErr = await _context.ThietBiGPS
                        .Where(t => !t.PhuongTiens.Any())
                        .ToListAsync();
                    ViewBag.IdThietBi = new SelectList(availableGpsOnErr, "IdThietBi", "MaImei", vehicle.IdThietBi);
                    return View(vehicle);
                }

                _context.Add(vehicle);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Thêm phương tiện mới thành công!";
                return RedirectToAction(nameof(Index));
            }
            var availableGpsFinal = await _context.ThietBiGPS
                .Where(t => !t.PhuongTiens.Any())
                .ToListAsync();
            ViewBag.IdThietBi = new SelectList(availableGpsFinal, "IdThietBi", "MaImei", vehicle.IdThietBi);
            return View(vehicle);
        }

        // GET: PhuongTien/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var vehicle = await _context.PhuongTiens.FindAsync(id);
            if (vehicle == null) return NotFound();

            var availableGps = await _context.ThietBiGPS
                .Where(t => !t.PhuongTiens.Any() || t.IdThietBi == vehicle.IdThietBi)
                .ToListAsync();
            ViewBag.IdThietBi = new SelectList(availableGps, "IdThietBi", "MaImei", vehicle.IdThietBi);
            return View(vehicle);
        }

        // POST: PhuongTien/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, PhuongTien vehicle)
        {
            if (id != vehicle.IdPhuongTien) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    // Kiểm tra biển số trùng (trình hợp đổi biển số)
                    if (await _context.PhuongTiens.AnyAsync(p => p.BienSo == vehicle.BienSo && p.IdPhuongTien != id))
                    {
                        ModelState.AddModelError("BienSo", "Biển số xe này đã tồn tại trong hệ thống.");
                        var availableGpsOnErr = await _context.ThietBiGPS
                            .Where(t => !t.PhuongTiens.Any() || t.IdThietBi == vehicle.IdThietBi)
                            .ToListAsync();
                        ViewBag.IdThietBi = new SelectList(availableGpsOnErr, "IdThietBi", "MaImei", vehicle.IdThietBi);
                        return View(vehicle);
                    }

                    _context.Update(vehicle);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Cập nhật thông tin phương tiện thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!VehicleExists(vehicle.IdPhuongTien)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            var availableGpsFinal = await _context.ThietBiGPS
                .Where(t => !t.PhuongTiens.Any() || t.IdThietBi == vehicle.IdThietBi)
                .ToListAsync();
            ViewBag.IdThietBi = new SelectList(availableGpsFinal, "IdThietBi", "MaImei", vehicle.IdThietBi);
            return View(vehicle);
        }

        // POST: PhuongTien/Delete/5
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var vehicle = await _context.PhuongTiens
                .Include(p => p.ChiTietHopDongs)
                .FirstOrDefaultAsync(p => p.IdPhuongTien == id);

            if (vehicle != null)
            {
                // Kiểm tra xem phương tiện có đang trong hợp đồng nào không
                if (vehicle.ChiTietHopDongs.Any())
                {
                    return Json(new { success = false, message = "Không thể xóa phương tiện đang có trong hợp đồng!" });
                }

                _context.PhuongTiens.Remove(vehicle);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Xóa phương tiện thành công!" });
            }
            return Json(new { success = false, message = "Không tìm thấy phương tiện!" });
        }

        private bool VehicleExists(int id)
        {
            return _context.PhuongTiens.Any(e => e.IdPhuongTien == id);
        }
    }
}
