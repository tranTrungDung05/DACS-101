using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using DACS.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace DACS.Controllers;

[Route("api/[controller]")]
[ApiController]
public class GpsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IHubContext<GpsHub> _hubContext;

    public GpsController(ApplicationDbContext context, IHubContext<GpsHub> hubContext)
    {
        _context = context;
        _hubContext = hubContext;
    }

    // POST: api/Gps/Update
    [HttpPost("Update")]
    public async Task<IActionResult> Update([FromBody] GpsUpdateDto request)
    {
        if (request == null) return BadRequest();

        // 1. Tìm phương tiện theo ID
        var vehicle = await _context.PhuongTiens
            .Include(p => p.HanhTrinhs)
            .FirstOrDefaultAsync(p => p.IdPhuongTien == request.VehicleID);

        if (vehicle == null) return NotFound($"Vehicle ID {request.VehicleID} not found.");

        // 2. Tìm hoặc tạo HanhTrinh cho xe (Lấy hành trình mới nhất đang active)
        var journey = vehicle.HanhTrinhs
            .OrderByDescending(h => h.NgayDi)
            .FirstOrDefault(h => h.TrangThai == true);

        if (journey == null)
        {
            journey = new HanhTrinh
            {
                PhuongTienIdPhuongTien = vehicle.IdPhuongTien,
                NgayDi = DateTime.Now,
                TrangThai = true
            };
            _context.HanhTrinhs.Add(journey);
            await _context.SaveChangesAsync();
        }

        // 3. Lưu dữ liệu GPS vào DB
        var gpsData = new DuLieuGPS
        {
            HanhTrinhIdHanhTrinh = journey.IdHanhTrinh,
            ThietBiGPSIdThietBi = vehicle.IdThietBi,
            ViDo = (decimal)request.Latitude,
            KinhDo = (decimal)request.Longitude,
            TocDo = (decimal)request.Speed,
            Timestamp = DateTime.Now
        };
        _context.DuLieuGPS.Add(gpsData);
        await _context.SaveChangesAsync();

        // 4. Phát qua SignalR gửi biển số xe
        await _hubContext.Clients.All.SendAsync("ReceiveLocationUpdate", 
            vehicle.BienSo, 
            request.Latitude, 
            request.Longitude, 
            request.Speed);

        return Ok(new { status = "Success", message = $"Location updated for {vehicle.BienSo}" });
    }
}

public class GpsUpdateDto
{
    public int VehicleID { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double Speed { get; set; }
}
