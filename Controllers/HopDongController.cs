using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using DACS.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;

namespace DACS.Controllers;

[Authorize(Roles = "Admin,Manager")]
public class HopDongController : Controller
{
    private readonly ApplicationDbContext _context;

    public HopDongController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: HopDong
    public async Task<IActionResult> Index()
    {
        var contracts = await _context.HopDongs
            .Include(h => h.KhachHang)
            .Include(h => h.TaiKhoan)
            .OrderByDescending(h => h.IdHopDong)
            .ToListAsync();
        return View(contracts);
    }

    // GET: HopDong/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();

        var contract = await _context.HopDongs
            .Include(h => h.KhachHang)
            .Include(h => h.TaiKhoan)
            .Include(h => h.ChiTietHopDongs)
                .ThenInclude(ct => ct.PhuongTien)
            .FirstOrDefaultAsync(m => m.IdHopDong == id);

        if (contract == null) return NotFound();

        return View(contract);
    }

    // GET: HopDong/Create
    public async Task<IActionResult> Create()
    {
        ViewBag.KhachHangs = new SelectList(await _context.KhachHangs.ToListAsync(), "MaCccd", "HoTen");
        // Lấy danh sách xe đang "Sẵn sàng"
        ViewBag.PhuongTiens = await _context.PhuongTiens
            .Where(p => p.TrangThai == true)
            .ToListAsync();
        return View();
    }

    // POST: HopDong/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(HopDong contract, List<ChiTietHopDong> details)
    {
        if (details == null || !details.Any())
        {
            ModelState.AddModelError("", "Vui lòng thêm ít nhất một phương tiện vào hợp đồng.");
        }

        if (ModelState.IsValid && details != null && details.Any())
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Gán thông tin người tạo
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "1");
                contract.IdTaiKhoan = userId;
                contract.NgayTao = DateOnly.FromDateTime(DateTime.Now);
                contract.TrangThai = true;

                _context.Add(contract);
                await _context.SaveChangesAsync();

                decimal totalAmount = 0;
                foreach (var item in details)
                {
                    item.IdHopDong = contract.IdHopDong;
                    totalAmount += item.GiaThue;
                    
                    // Cập nhật trạng thái xe thành Đang bận (false)
                    var vehicle = await _context.PhuongTiens.FindAsync(item.IdPhuongTien);
                    if (vehicle != null)
                    {
                        vehicle.TrangThai = false;
                        _context.Update(vehicle);
                    }

                    _context.Add(item);
                }

                contract.TongTien = totalAmount;
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
                TempData["SuccessMessage"] = "Tạo hợp đồng thành công!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                ModelState.AddModelError("", "Đã xảy ra lỗi khi tạo hợp đồng: " + ex.Message);
            }
        }

        ViewBag.KhachHangs = new SelectList(await _context.KhachHangs.ToListAsync(), "MaCccd", "HoTen", contract.MaCccd);
        ViewBag.PhuongTiens = await _context.PhuongTiens.Where(p => p.TrangThai == true).ToListAsync();
        return View(contract);
    }

    // POST: HopDong/Delete/5
    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var contract = await _context.HopDongs
            .Include(h => h.ChiTietHopDongs)
            .FirstOrDefaultAsync(h => h.IdHopDong == id);

        if (contract != null)
        {
            // Giải phóng các xe trong hợp đồng nếu hợp đồng bị xóa
            foreach (var detail in contract.ChiTietHopDongs)
            {
                var vehicle = await _context.PhuongTiens.FindAsync(detail.IdPhuongTien);
                if (vehicle != null)
                {
                    vehicle.TrangThai = true; // Trở lại trạng thái Sẵn sàng
                    _context.Update(vehicle);
                }
            }

            _context.HopDongs.Remove(contract);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Xóa hợp đồng thành công!" });
        }
        return Json(new { success = false, message = "Không tìm thấy hợp đồng!" });
    }
}
