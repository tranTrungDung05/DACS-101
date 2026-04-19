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

        // 1. Tìm phương tiện theo ID (Bao gồm cả thiết bị và hành trình)
        var vehicle = await _context.PhuongTiens
            .Include(p => p.HanhTrinhs)
            .Include(p => p.ThietBiGPS)
            .FirstOrDefaultAsync(p => p.IdPhuongTien == request.VehicleID);

        if (vehicle == null) return NotFound($"Vehicle ID {request.VehicleID} not found.");

        // 2. Kiểm tra nếu thiết bị GPS đang tắt
        if (vehicle.ThietBiGPS == null || !vehicle.ThietBiGPS.TrangThai)
        {
            return BadRequest(new { status = "Error", message = $"Thiết bị GPS của xe {vehicle.BienSo} đang tắt hoặc không khả dụng." });
        }

        // 3. Kiểm tra trạng thái phương tiện
        // Nếu xe "Sẵn sàng" (True) -> Đang ở bãi, không cập nhật GPS.
        // Chỉ cập nhật khi xe "Đang bận" (False) -> Đang được sử dụng/di chuyển.
        if (vehicle.TrangThai)
        {
            return BadRequest(new { status = "Error", message = $"Phương tiện {vehicle.BienSo} đang ở trạng thái sẵn sàng (tại bãi), không nhận cập nhật hành trình." });
        }

        // 4. Tìm hoặc tạo HanhTrinh cho xe (Lấy hành trình mới nhất đang active)
        var journey = vehicle.HanhTrinhs
            .OrderByDescending(h => h.NgayDi)
            .FirstOrDefault(h => h.TrangThai == true);

        if (journey == null)
        {
            journey = new HanhTrinh
            {
                IdPhuongTien = vehicle.IdPhuongTien,
                NgayDi = DateTime.Now,
                NgayDen = DateTime.Now,
                TongQuangDuong = 0,
                TrangThai = true
            };
            _context.HanhTrinhs.Add(journey);
            await _context.SaveChangesAsync();
        }

        // 3. Tính toán quãng đường (nếu có điểm GPS trước đó)
        var lastGps = await _context.DuLieuGPS
            .Where(d => d.HanhTrinhIdHanhTrinh == journey.IdHanhTrinh)
            .OrderByDescending(d => d.Timestamp)
            .FirstOrDefaultAsync();

        if (lastGps != null)
        {
            double dist = CalculateDistance(
                (double)lastGps.ViDo, (double)lastGps.KinhDo,
                request.Latitude, request.Longitude
            );
            journey.TongQuangDuong += (decimal)dist;
        }

        // 4. Cập nhật thời điểm mới nhất của hành trình
        journey.NgayDen = DateTime.Now;

        // 5. Lưu dữ liệu GPS vào DB
        var gpsData = new DuLieuGPS
        {
            HanhTrinhIdHanhTrinh = journey.IdHanhTrinh,
            IdThietBi = vehicle.IdThietBi,
            ViDo = (decimal)request.Latitude,
            KinhDo = (decimal)request.Longitude,
            TocDo = (decimal)request.Speed,
            Timestamp = DateTime.Now
        };
        _context.DuLieuGPS.Add(gpsData);
        await _context.SaveChangesAsync();

        // 6. Phát qua SignalR gửi biển số xe
        await _hubContext.Clients.All.SendAsync("ReceiveLocationUpdate", 
            vehicle.BienSo, 
            request.Latitude, 
            request.Longitude, 
            request.Speed);

        return Ok(new { status = "Success", message = $"Location updated for {vehicle.BienSo}. Total distance: {journey.TongQuangDuong:N2} km" });
    }

    private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        var R = 6371; // Bán kính Trái Đất theo km
        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);
        var a =
            Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
            Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
            Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }

    private double ToRadians(double deg)
    {
        return deg * (Math.PI / 180);
    }
}

public class GpsUpdateDto
{
    public int VehicleID { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double Speed { get; set; }
}
