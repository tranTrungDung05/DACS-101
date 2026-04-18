using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using DACS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace DACS.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;

    public HomeController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var vehicles = await _context.PhuongTiens
            .Include(p => p.HanhTrinhs)
                .ThenInclude(h => h.DuLieuGPS)
            .ToListAsync();

        return View(vehicles);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    public async Task<IActionResult> Dashboard()
    {
        var today = DateTime.Today;
        
        ViewBag.TotalVehicles = await _context.PhuongTiens.CountAsync();
        ViewBag.ActiveDevices = await _context.ThietBiGPS.CountAsync(t => t.TrangThai == true);
        ViewBag.ViolationsToday = await _context.PhieuViPhams.CountAsync(v => v.NgayViPham.Date == today);
        
        // Status breakdown
        ViewBag.ReadyCount = await _context.PhuongTiens.CountAsync(p => p.TrangThai == true);
        ViewBag.BusyCount = await _context.PhuongTiens.CountAsync(p => p.TrangThai == false);

        return View();
    }

    public IActionResult Login()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
