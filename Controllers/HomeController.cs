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

        // Weekly Distance Chart Data
        DateTime startOfWeek = today.AddDays(-(int)today.DayOfWeek + (int)DayOfWeek.Monday);
        if (today.DayOfWeek == DayOfWeek.Sunday) startOfWeek = today.AddDays(-6);
        
        var weeklyData = await _context.DuLieuGPS
            .Where(d => d.Timestamp.Date >= startOfWeek.Date && d.Timestamp.Date < startOfWeek.AddDays(7).Date)
            .GroupBy(d => d.Timestamp.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToListAsync();

        var labels = new string[7];
        var signalData = new int[7];
        string[] dayNames = { "Thứ 2", "Thứ 3", "Thứ 4", "Thứ 5", "Thứ 6", "Thứ 7", "Chủ Nhật" };

        for (int i = 0; i < 7; i++)
        {
            var date = startOfWeek.AddDays(i);
            labels[i] = dayNames[i];
            signalData[i] = weeklyData.FirstOrDefault(d => d.Date == date.Date)?.Count ?? 0;
        }

        ViewBag.WeeklyLabels = labels;
        ViewBag.WeeklyDistances = signalData;

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
