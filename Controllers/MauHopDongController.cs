using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DACS.Models;
using Microsoft.AspNetCore.Authorization;

namespace DACS.Controllers;

[Authorize(Roles = "Admin")]
public class MauHopDongController : Controller
{
    private readonly ApplicationDbContext _context;

    public MauHopDongController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: MauHopDong
    public async Task<IActionResult> Index()
    {
        return View(await _context.MauHopDongs.OrderByDescending(m => m.IdMau).ToListAsync());
    }

    // GET: MauHopDong/Create
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(MauHopDong mau)
    {
        if (ModelState.IsValid)
        {
            if (mau.LaMacDinh)
            {
                // Bỏ mặc định các mẫu cũ
                var oldDefaults = await _context.MauHopDongs.Where(m => m.LaMacDinh).ToListAsync();
                foreach (var old in oldDefaults) old.LaMacDinh = false;
            }

            _context.Add(mau);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(mau);
    }

    // GET: MauHopDong/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();
        var mau = await _context.MauHopDongs.FindAsync(id);
        if (mau == null) return NotFound();
        return View(mau);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, MauHopDong mau)
    {
        if (id != mau.IdMau) return NotFound();

        if (ModelState.IsValid)
        {
            if (mau.LaMacDinh)
            {
                var oldDefaults = await _context.MauHopDongs.Where(m => m.LaMacDinh && m.IdMau != id).ToListAsync();
                foreach (var old in oldDefaults) old.LaMacDinh = false;
            }

            _context.Update(mau);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(mau);
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var mau = await _context.MauHopDongs.FindAsync(id);
        if (mau != null)
        {
            _context.MauHopDongs.Remove(mau);
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }
        return Json(new { success = false });
    }
}
