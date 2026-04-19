using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DACS.Models;

namespace DACS.Controllers;

public class ViolationController : Controller
{
    private readonly ApplicationDbContext _context;

    public ViolationController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: Violation
    public async Task<IActionResult> Index()
    {
        var tickets = await _context.PhieuViPhams
            .Include(p => p.HanhTrinh)
                .ThenInclude(h => h.PhuongTien)
            .Include(p => p.KhachHang)
            .OrderByDescending(p => p.NgayViPham)
            .ToListAsync();
        return View(tickets);
    }

    // GET: Violation/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var ticket = await _context.PhieuViPhams
            .Include(p => p.HanhTrinh)
                .ThenInclude(h => h.PhuongTien)
            .Include(p => p.KhachHang)
            .Include(p => p.ChiTietViPhams)
                .ThenInclude(ct => ct.QuyDinh)
            .FirstOrDefaultAsync(p => p.IdPhieuViPham == id);

        if (ticket == null) return NotFound();

        return View(ticket);
    }

    // POST: Violation/UpdateStatus
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, bool trangThai)
    {
        var ticket = await _context.PhieuViPhams.FindAsync(id);
        if (ticket == null) return NotFound();

        ticket.TrangThai = trangThai;
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }
}
