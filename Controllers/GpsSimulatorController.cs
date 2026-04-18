using DACS;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Text.Json;
using System.Threading.Tasks;
using System.Net.Http;
using System.Collections.Generic;
using System.Linq;
using System;
using DACS.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

namespace DACS.Controllers
{
    public class GpsSimulatorController : Controller
    {
        private readonly IHubContext<GpsHub> _hubContext;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IServiceScopeFactory _scopeFactory;

        public GpsSimulatorController(IHubContext<GpsHub> hubContext, IHttpClientFactory httpClientFactory, IServiceScopeFactory scopeFactory)
        {
            _hubContext = hubContext;
            _httpClientFactory = httpClientFactory;
            _scopeFactory = scopeFactory;
        }

        [HttpGet]
        public async Task<IActionResult> AutoSimulate(string plate, string start = "106.7111,10.8015", string end = "106.7009,10.7769")
        {
            if (string.IsNullOrEmpty(plate)) return BadRequest("Plate is required");

            // 1. Kiểm tra xe tồn tại
            using (var scope = _scopeFactory.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var vehicle = await db.PhuongTiens.FirstOrDefaultAsync(p => p.BienSo == plate);
                if (vehicle == null) return NotFound($"Vehicle {plate} not found");
            }

            // 2. Gọi OSRM API từ server
            var client = _httpClientFactory.CreateClient();
            var url = $"http://router.project-osrm.org/route/v1/driving/{start};{end}?overview=full&geometries=geojson";
            
            var response = await client.GetAsync(url);
            if (!response.IsSuccessStatusCode) return BadRequest("Failed to fetch route from OSRM");

            var content = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(content);
            
            var coordinates = doc.RootElement
                .GetProperty("routes")[0]
                .GetProperty("geometry")
                .GetProperty("coordinates")
                .EnumerateArray()
                .Select(c => new { lng = c[0].GetDouble(), lat = c[1].GetDouble() })
                .ToList();

            // 3. Chạy ngầm mô phỏng và lưu DB
            _ = Task.Run(async () => {
                var random = new Random();
                
                using (var scope = _scopeFactory.CreateScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    
                    // Tìm hoặc tạo HanhTrinh
                    var vehicle = await db.PhuongTiens
                        .Include(p => p.HanhTrinhs)
                        .FirstOrDefaultAsync(p => p.BienSo == plate);
                        
                    var journey = vehicle.HanhTrinhs.OrderByDescending(h => h.NgayDi).FirstOrDefault();
                    
                    if (journey == null)
                    {
                        journey = new HanhTrinh { 
                            PhuongTienIdPhuongTien = vehicle.IdPhuongTien,
                            NgayDi = DateTime.Now,
                            TrangThai = true 
                        };
                        db.HanhTrinhs.Add(journey);
                        await db.SaveChangesAsync();
                    }

                    foreach (var coord in coordinates)
                    {
                        double speed = random.Next(30, 80);
                        
                        // Lưu vào DB
                        var gpsData = new DuLieuGPS {
                            HanhTrinhIdHanhTrinh = journey.IdHanhTrinh,
                            ThietBiGPSIdThietBi = vehicle.IdThietBi,
                            ViDo = (decimal)coord.lat,
                            KinhDo = (decimal)coord.lng,
                            TocDo = (decimal)speed,
                            Timestamp = DateTime.Now
                        };
                        db.DuLieuGPS.Add(gpsData);
                        await db.SaveChangesAsync();

                        // Phát qua SignalR
                        await _hubContext.Clients.All.SendAsync("ReceiveLocationUpdate", plate, coord.lat, coord.lng, speed);
                        
                        await Task.Delay(1500);
                    }
                }
            });

            return Ok(new { 
                status = "Started", 
                message = $"Simulating {plate} and saving to DB. Route has {coordinates.Count} points.",
                start, end 
            });
        }
    }
}
