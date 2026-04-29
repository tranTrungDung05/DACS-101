using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using DACS.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using DACS.Services;

namespace DACS.Controllers;

[Route("api/[controller]")]
[ApiController]
public class GpsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IHubContext<GpsHub> _hubContext;
    private readonly IConfiguration _configuration;
    private readonly ISpeedLimitService _speedLimitService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IServiceScopeFactory _scopeFactory;

    public GpsController(ApplicationDbContext context, IHubContext<GpsHub> hubContext, IConfiguration configuration, ISpeedLimitService speedLimitService, IHttpClientFactory httpClientFactory, IServiceScopeFactory scopeFactory)
    {
        _context = context;
        _hubContext = hubContext;
        _configuration = configuration;
        _speedLimitService = speedLimitService;
        _httpClientFactory = httpClientFactory;
        _scopeFactory = scopeFactory;
    }

    // POST: api/Gps/Update
    [HttpPost("Update")]
    public async Task<IActionResult> Update([FromBody] GpsUpdateDto request)
    {
        if (request == null) return BadRequest();

        // --- LẤY TỐC ĐỘ GIỚI HẠN TỪ OSM TRƯỚC (Để Log luôn hiện ra) ---
        // Truyền thêm request.VehicleID để quản lý Cache theo từng xe
        var maxSpeed = await _speedLimitService.GetMaxSpeed(request.VehicleID, request.Latitude, request.Longitude);

        // 1. Tìm phương tiện theo ID (Bao gồm cả thiết bị và hành trình)
        var vehicle = await _context.PhuongTiens
            .Include(p => p.HanhTrinhs)
            .Include(p => p.ThietBiGPS)
            .FirstOrDefaultAsync(p => p.IdPhuongTien == request.VehicleID);

        if (vehicle == null) 
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"[INFO] Đã lấy được tốc độ giới hạn ({maxSpeed}km/h) nhưng không tìm thấy Vehicle ID {request.VehicleID} trong DB.");
            Console.ResetColor();
            return NotFound($"Vehicle ID {request.VehicleID} not found.");
        }

        // 2. Kiểm tra nếu thiết bị GPS đang tắt
        if (vehicle.ThietBiGPS == null || !vehicle.ThietBiGPS.TrangThai)
        {
            return BadRequest(new { status = "Error", message = $"Thiết bị GPS của xe {vehicle.BienSo} đang tắt hoặc không khả dụng." });
        }

        // 3. Kiểm tra trạng thái phương tiện
        if (vehicle.TrangThai)
        {
            return BadRequest(new { status = "Error", message = $"Phương tiện {vehicle.BienSo} đang ở trạng thái sẵn sàng (tại bãi), không nhận cập nhật hành trình." });
        }

        // 4. Tìm hoặc tạo HanhTrinh cho xe (TỰ ĐỘNG CẮT HÀNH TRÌNH)
        const int TRIP_GAP_MINUTES = 5; // Ngưỡng: nghỉ > 5 phút = hành trình mới

        var journey = vehicle.HanhTrinhs
            .OrderByDescending(h => h.NgayDi)
            .FirstOrDefault(h => h.TrangThai == true);

        if (journey != null)
        {
            // Kiểm tra khoảng cách thời gian với điểm GPS cuối cùng
            var lastGpsForGap = await _context.DuLieuGPS
                .Where(d => d.HanhTrinhIdHanhTrinh == journey.IdHanhTrinh)
                .OrderByDescending(d => d.Timestamp)
                .FirstOrDefaultAsync();

            if (lastGpsForGap != null)
            {
                var gap = DateTime.Now - lastGpsForGap.Timestamp;
                if (gap.TotalMinutes > TRIP_GAP_MINUTES)
                {
                    // ĐÓNG hành trình cũ
                    journey.TrangThai = false;
                    journey.NgayDen = lastGpsForGap.Timestamp;
                    await _context.SaveChangesAsync();

                    Console.ForegroundColor = ConsoleColor.Magenta;
                    Console.WriteLine($"[TRIP] Đóng hành trình #{journey.IdHanhTrinh} ({vehicle.BienSo}) - Nghỉ {gap.TotalMinutes:N0} phút > ngưỡng {TRIP_GAP_MINUTES} phút");
                    Console.ResetColor();

                    // TỰ ĐỘNG PHÂN TÍCH hành trình vừa đóng (chạy nền, không block request)
                    var closedJourneyId = journey.IdHanhTrinh;
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await AutoAnalyzeBehavior(closedJourneyId, vehicle.BienSo);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[BEHAVIOR] Lỗi phân tích tự động hành trình #{closedJourneyId}: {ex.Message}");
                        }
                    });

                    // Đặt null để tạo hành trình mới ở bước tiếp theo
                    journey = null;
                }
            }
        }

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

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"[TRIP] Mở hành trình mới #{journey.IdHanhTrinh} cho xe {vehicle.BienSo}");
            Console.ResetColor();
        }

        // 5. Tính toán quãng đường
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

        journey.NgayDen = DateTime.Now;

        // 6. Lưu dữ liệu GPS vào DB
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

        // 6b. Lưu dữ liệu Gia tốc kế vào DB (nếu simulator gửi lên)
        if (request.AccelLongG.HasValue && request.AccelLatG.HasValue)
        {
            var accelData = new DuLieuGiaTocKe
            {
                HanhTrinhIdHanhTrinh = journey.IdHanhTrinh,
                IdThietBi = vehicle.IdThietBi,
                GiaTocDoc = (decimal)request.AccelLongG.Value,
                GiaTocNgang = (decimal)request.AccelLatG.Value,
                Timestamp = DateTime.Now
            };
            _context.DuLieuGiaTocKes.Add(accelData);
        }

        // --- XỬ LÝ VI PHẠM TỐC ĐỘ ---
        if (request.Speed > maxSpeed)
        {
            // Tìm khách hàng hiện tại của xe qua hợp đồng đang hoạt động
            var activeContract = await _context.HopDongs
                .Include(h => h.KhachHang) // Load KhachHang
                .Include(h => h.ChiTietHopDongs)
                .Where(h => h.TrangThai && h.ChiTietHopDongs.Any(ct => ct.IdPhuongTien == vehicle.IdPhuongTien))
                .OrderByDescending(h => h.NgayTao)
                .FirstOrDefaultAsync();

            if (activeContract != null)
            {
                // Tìm quy định "Quá tốc độ"
                var speedingRule = await _context.QuyDinhs
                    .FirstOrDefaultAsync(q => q.TenQuyDinh.Contains("tốc độ"));
                
                if (speedingRule == null)
                {
                    speedingRule = new QuyDinh { TenQuyDinh = "Quá tốc độ quy định", MucPhat = 500000, MoTa = "Vi phạm vượt quá tốc độ cho phép" };
                    _context.QuyDinhs.Add(speedingRule);
                    await _context.SaveChangesAsync();
                }

                // Kiểm tra/Tạo Phiếu vi phạm cho KHÁCH HÀNG này trong NGÀY HÔM NAY
                var today = DateTime.Now.Date;
                var violationTicket = await _context.PhieuViPhams
                    .FirstOrDefaultAsync(p => p.MaCccd == activeContract.MaCccd && p.NgayViPham.Date == today);

                if (violationTicket == null)
                {
                    violationTicket = new PhieuViPham
                    {
                        HanhTrinh = journey, // Vẫn lưu vết hành trình đầu tiên phát sinh lỗi
                        KhachHang = activeContract.KhachHang,
                        NgayViPham = DateTime.Now,
                        TienPhat = speedingRule.MucPhat,
                        MoTa = $"Vi phạm tổng hợp ngày {today:dd/MM/yyyy}",
                        TrangThai = true
                    };
                    _context.PhieuViPhams.Add(violationTicket);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    // Cộng dồn tiền phạt vào phiếu của ngày hôm nay
                    violationTicket.TienPhat += speedingRule.MucPhat;
                    // Cập nhật mô tả nếu cần
                    violationTicket.MoTa = $"Tổng hợp vi phạm ngày {today:dd/MM/yyyy}";
                }

                // Ghi nhận Chi tiết vi phạm (Lưu lại bằng chứng: Tốc độ và Tọa độ)
                var violationDetail = new ChiTietViPham
                {
                    PhieuViPhamIdPhieuViPham = violationTicket.IdPhieuViPham,
                    QuyDinhIdQuyDinh = speedingRule.IdQuyDinh,
                    TocDoViPham = (decimal)request.Speed,
                    ViDo = (decimal)request.Latitude,
                    KinhDo = (decimal)request.Longitude,
                    ThoiGianXayRa = DateTime.Now
                };
                _context.ChiTietViPhams.Add(violationDetail);

                // Phát SignalR cảnh báo vi phạm
                await _hubContext.Clients.All.SendAsync("ReceiveViolationAlert", 
                    vehicle.BienSo, 
                    "Quá tốc độ", 
                    request.Speed, 
                    maxSpeed);
            }
        }

        await _context.SaveChangesAsync();

        // 7. Phát qua SignalR gửi biển số xe
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

    /// <summary>
    /// Tự động phân tích hành vi khi đóng hành trình (chạy nền với DbContext riêng)
    /// </summary>
    private async Task AutoAnalyzeBehavior(int hanhTrinhId, string bienSo)
    {
        // Tạo scope mới vì method này chạy nền, không dùng chung DbContext của request
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var httpFactory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();

        var journey = await db.HanhTrinhs
            .Include(h => h.DuLieuGPS)
            .Include(h => h.DuLieuGiaTocKes)
            .FirstOrDefaultAsync(h => h.IdHanhTrinh == hanhTrinhId);

        if (journey == null) return;

        var gpsData = journey.DuLieuGPS.OrderBy(d => d.Timestamp).ToList();
        var accelData = journey.DuLieuGiaTocKes.OrderBy(d => d.Timestamp).ToList();

        if (gpsData.Count < 2 || accelData.Count < 2)
        {
            Console.WriteLine($"[BEHAVIOR] Hành trình #{hanhTrinhId} ({bienSo}): Không đủ dữ liệu ({gpsData.Count} GPS, {accelData.Count} Accel) - bỏ qua.");
            return;
        }

        var startTime = gpsData.First().Timestamp;

        var payload = new {
            gps_points = gpsData.Select(d => new {
                timestamp_s = (d.Timestamp - startTime).TotalSeconds,
                lat = (double)d.ViDo,
                lon = (double)d.KinhDo,
                speed_kmh = (double)d.TocDo
            }),
            accel_points = accelData.Select(d => new {
                timestamp_s = (d.Timestamp - startTime).TotalSeconds,
                accel_long_g = (double)d.GiaTocDoc,
                accel_lat_g = (double)d.GiaTocNgang
            })
        };

        var behaviorServiceUrl = _configuration.GetValue<string>("BehaviorServiceUrl") ?? "http://127.0.0.1:8000";

        try
        {
            var client = httpFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(10);

            var jsonContent = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json"
            );

            var response = await client.PostAsync($"{behaviorServiceUrl}/predict-split", jsonContent);
            if (response.IsSuccessStatusCode)
            {
                var resultJson = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<JsonElement>(resultJson);
                var predictionName = result.GetProperty("prediction_name").GetString();

                journey.PhanLoaiHanhVi = predictionName;
                await db.SaveChangesAsync();

                Console.ForegroundColor = predictionName == "NORMAL" ? ConsoleColor.Green :
                                          predictionName == "AGGRESSIVE" ? ConsoleColor.Red : ConsoleColor.Yellow;
                Console.WriteLine($"[BEHAVIOR] ✅ Hành trình #{hanhTrinhId} ({bienSo}): {predictionName} ({gpsData.Count} GPS + {accelData.Count} Accel)");
                Console.ResetColor();
            }
            else
            {
                Console.WriteLine($"[BEHAVIOR] ⚠️ Hành trình #{hanhTrinhId}: behavior_service trả về HTTP {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[BEHAVIOR] ⚠️ Hành trình #{hanhTrinhId}: Không kết nối được behavior_service - {ex.Message}");
        }
    }

    // POST: api/Gps/AnalyzeBehavior/{hanhTrinhId}
    // Đọc dữ liệu GPS + Gia tốc kế từ DB, gọi behavior_service để phân tích
    [HttpPost("AnalyzeBehavior/{hanhTrinhId}")]
    public async Task<IActionResult> AnalyzeBehavior(int hanhTrinhId)
    {
        // 1. Lấy hành trình kèm dữ liệu GPS và Gia tốc kế từ DB
        var journey = await _context.HanhTrinhs
            .Include(h => h.PhuongTien)
            .Include(h => h.DuLieuGPS)
            .Include(h => h.DuLieuGiaTocKes)
            .FirstOrDefaultAsync(h => h.IdHanhTrinh == hanhTrinhId);

        if (journey == null)
            return NotFound($"Không tìm thấy hành trình ID {hanhTrinhId}");

        var gpsData = journey.DuLieuGPS.OrderBy(d => d.Timestamp).ToList();
        var accelData = journey.DuLieuGiaTocKes.OrderBy(d => d.Timestamp).ToList();

        if (gpsData.Count < 2)
            return BadRequest("Hành trình không đủ dữ liệu GPS để phân tích (cần ít nhất 2 điểm).");

        if (accelData.Count < 2)
            return BadRequest("Hành trình không đủ dữ liệu gia tốc kế để phân tích (cần ít nhất 2 điểm).");

        // 2. Chuyển đổi dữ liệu DB sang format mà behavior_service yêu cầu
        var startTime = gpsData.First().Timestamp;

        var gpsPoints = gpsData.Select(d => new {
            timestamp_s = (d.Timestamp - startTime).TotalSeconds,
            lat = (double)d.ViDo,
            lon = (double)d.KinhDo,
            speed_kmh = (double)d.TocDo
        }).ToList();

        var accelPoints = accelData.Select(d => new {
            timestamp_s = (d.Timestamp - startTime).TotalSeconds,
            accel_long_g = (double)d.GiaTocDoc,
            accel_lat_g = (double)d.GiaTocNgang
        }).ToList();

        // 3. Gọi behavior_service (Python) để phân tích
        var behaviorServiceUrl = _configuration.GetValue<string>("BehaviorServiceUrl") ?? "http://127.0.0.1:8000";

        var payload = new {
            gps_points = gpsPoints,
            accel_points = accelPoints
        };

        try
        {
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(10);

            var jsonContent = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json"
            );

            var response = await client.PostAsync($"{behaviorServiceUrl}/predict-split", jsonContent);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                return StatusCode(502, $"Behavior service trả về lỗi: {errorBody}");
            }

            var resultJson = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JsonElement>(resultJson);

            // 4. Lưu kết quả phân tích vào HanhTrinh trong DB
            var predictionName = result.GetProperty("prediction_name").GetString();
            journey.PhanLoaiHanhVi = predictionName;

            await _context.SaveChangesAsync();

            // 5. Log kết quả ra console
            Console.ForegroundColor = predictionName == "NORMAL" ? ConsoleColor.Green :
                                      predictionName == "AGGRESSIVE" ? ConsoleColor.Red : ConsoleColor.Yellow;
            Console.WriteLine($"[BEHAVIOR] Hành trình #{hanhTrinhId} ({journey.PhuongTien?.BienSo}): {predictionName}");
            Console.ResetColor();

            return Ok(new {
                hanhTrinhId = hanhTrinhId,
                bienSo = journey.PhuongTien?.BienSo,
                phanLoai = predictionName,
                soLuongGPS = gpsData.Count,
                soLuongAccel = accelData.Count,
                chiTiet = resultJson
            });
        }
        catch (TaskCanceledException)
        {
            return StatusCode(504, "Behavior service không phản hồi (timeout). Hãy kiểm tra xem service đã chạy chưa.");
        }
        catch (HttpRequestException ex)
        {
            return StatusCode(502, $"Không thể kết nối behavior_service: {ex.Message}. Hãy chạy: python Services/behavior_service.py");
        }
    }
}

public class GpsUpdateDto
{
    public int VehicleID { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double Speed { get; set; }

    /// <summary>Gia tốc dọc (g) - từ cảm biến gia tốc kế</summary>
    public double? AccelLongG { get; set; }

    /// <summary>Gia tốc ngang (g) - từ cảm biến gia tốc kế</summary>
    public double? AccelLatG { get; set; }
}
