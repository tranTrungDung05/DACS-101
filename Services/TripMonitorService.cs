using DACS.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;

namespace DACS.Services;

/// <summary>
/// Background Service: Mỗi phút quét các hành trình đang mở,
/// nếu hành trình nào > 5 phút không nhận GPS mới → tự động đóng + phân tích hành vi.
/// </summary>
public class TripMonitorService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<TripMonitorService> _logger;

    private const int CHECK_INTERVAL_SECONDS = 60;  // Quét mỗi 60 giây
    private const int TRIP_GAP_MINUTES = 1;          // Ngưỡng nghỉ = 5 phút

    private readonly IHubContext<GpsHub> _hubContext;

    public TripMonitorService(IServiceScopeFactory scopeFactory, IConfiguration configuration, ILogger<TripMonitorService> logger, IHubContext<GpsHub> hubContext)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _logger = logger;
        _hubContext = hubContext;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("[TripMonitor] Đã khởi động. Quét mỗi {interval}s, ngưỡng nghỉ {gap} phút.", CHECK_INTERVAL_SECONDS, TRIP_GAP_MINUTES);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(CHECK_INTERVAL_SECONDS), stoppingToken);

            try
            {
                await CheckAndCloseStaleTrips(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[TripMonitor] Lỗi khi quét hành trình.");
            }
        }
    }

    private async Task CheckAndCloseStaleTrips(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Tìm tất cả hành trình đang mở (TrangThai = true)
        var openTrips = await db.HanhTrinhs
            .Include(h => h.PhuongTien)
            .Where(h => h.TrangThai == true)
            .ToListAsync(ct);

        foreach (var trip in openTrips)
        {
            // Lấy GPS cuối cùng của hành trình này
            var lastGps = await db.DuLieuGPS
                .Where(d => d.HanhTrinhIdHanhTrinh == trip.IdHanhTrinh)
                .OrderByDescending(d => d.Timestamp)
                .FirstOrDefaultAsync(ct);

            if (lastGps == null) continue; // Hành trình chưa có GPS nào

            var gap = DateTime.Now - lastGps.Timestamp;
            if (gap.TotalMinutes <= TRIP_GAP_MINUTES) continue; // Vẫn còn trong ngưỡng

            // ĐÓNG hành trình
            var bienSo = trip.PhuongTien?.BienSo ?? $"Xe #{trip.IdPhuongTien}";
            trip.TrangThai = false;
            trip.NgayDen = lastGps.Timestamp;
            await db.SaveChangesAsync(ct);

            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine($"[TripMonitor] Đóng hành trình #{trip.IdHanhTrinh} ({bienSo}) - Không có GPS mới trong {gap.TotalMinutes:N0} phút");
            Console.ResetColor();

            // TỰ ĐỘNG PHÂN TÍCH HÀNH VI
            await AnalyzeBehavior(trip.IdHanhTrinh, bienSo, ct);
        }
    }

    private async Task AnalyzeBehavior(int hanhTrinhId, string bienSo, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var httpFactory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();

        var journey = await db.HanhTrinhs
            .Include(h => h.DuLieuGPS)
            .Include(h => h.DuLieuGiaTocKes)
            .FirstOrDefaultAsync(h => h.IdHanhTrinh == hanhTrinhId, ct);

        if (journey == null) return;

        var gpsData = journey.DuLieuGPS.OrderBy(d => d.Timestamp).ToList();
        var accelData = journey.DuLieuGiaTocKes.OrderBy(d => d.Timestamp).ToList();

        if (gpsData.Count < 2 || accelData.Count < 2)
        {
            Console.WriteLine($"[TripMonitor] Hành trình #{hanhTrinhId} ({bienSo}): Không đủ dữ liệu ({gpsData.Count} GPS, {accelData.Count} Accel) - bỏ qua.");
            return;
        }

        var startTime = gpsData.First().Timestamp;

        var payload = new
        {
            gps_points = gpsData.Select(d => new
            {
                timestamp_s = (d.Timestamp - startTime).TotalSeconds,
                lat = (double)d.ViDo,
                lon = (double)d.KinhDo,
                speed_kmh = (double)d.TocDo
            }),
            accel_points = accelData.Select(d => new
            {
                timestamp_s = (d.Timestamp - startTime).TotalSeconds,
                accel_long_g = (double)d.GiaTocDoc,
                accel_lat_g = (double)d.GiaTocNgang,
                accel_lat_smooth_g = d.GiaTocNgangMuot.HasValue ? d.GiaTocNgangMuot.Value : (double?)null
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

            var response = await client.PostAsync($"{behaviorServiceUrl}/predict-split", jsonContent, ct);
            if (response.IsSuccessStatusCode)
            {
                var resultJson = await response.Content.ReadAsStringAsync(ct);
                var result = JsonSerializer.Deserialize<JsonElement>(resultJson);
                var predictionName = result.GetProperty("prediction_name").GetString();

                journey.PhanLoaiHanhVi = predictionName;

                // --- CẬP NHẬT ĐIỂM AN TOÀN KHÁCH HÀNG ---
                var activeContract = await db.HopDongs
                    .Include(h => h.KhachHang)
                    .Where(h => h.TrangThai && h.ChiTietHopDongs.Any(ct => ct.IdPhuongTien == journey.IdPhuongTien))
                    .OrderByDescending(h => h.NgayTao)
                    .FirstOrDefaultAsync(ct);

                if (activeContract != null && activeContract.KhachHang != null)
                {
                    var customer = activeContract.KhachHang;
                    int oldScore = customer.DiemAnToan;

                    if (predictionName == "NORMAL")
                        customer.DiemAnToan = Math.Min(100, customer.DiemAnToan + 5);
                    else if (predictionName == "AGGRESSIVE")
                        customer.DiemAnToan -= 20;
                    else if (predictionName == "DROWSY")
                        customer.DiemAnToan -= 15;

                    Console.WriteLine($"[TripMonitor - SCORE] Khách hàng {customer.HoTen}: {oldScore} -> {customer.DiemAnToan}");

                    bool alreadyHasViolation = await db.PhieuViPhams.AnyAsync(v => v.IdHanhTrinh == journey.IdHanhTrinh, ct);
                    if (predictionName != "NORMAL" && customer.DiemAnToan < 50 && !alreadyHasViolation)
                    {
                        var dangerousRule = await db.QuyDinhs.FirstOrDefaultAsync(q => q.TenQuyDinh.Contains("nguy hiểm"), ct);
                        if (dangerousRule == null)
                        {
                            dangerousRule = new QuyDinh { TenQuyDinh = "Hành vi lái xe nguy hiểm", MucPhat = 1000000, MoTa = "Điểm an toàn thấp dưới 50" };
                            db.QuyDinhs.Add(dangerousRule);
                            await db.SaveChangesAsync(ct);
                        }

                        var violationTicket = new PhieuViPham
                        {
                            IdHanhTrinh = journey.IdHanhTrinh,
                            MaCccd = customer.MaCccd,
                            NgayViPham = DateTime.Now,
                            TienPhat = dangerousRule.MucPhat,
                            MoTa = $"Vi phạm do điểm an toàn thấp ({customer.DiemAnToan}) sau hành trình #{journey.IdHanhTrinh}",
                            TrangThai = true
                        };
                        db.PhieuViPhams.Add(violationTicket);
                        await _hubContext.Clients.All.SendAsync("ReceiveViolationAlert", bienSo, "Hành vi nguy hiểm", 0, 0);
                    }
                }

                await db.SaveChangesAsync(ct);

                Console.ForegroundColor = predictionName == "NORMAL" ? ConsoleColor.Green :
                                          predictionName == "AGGRESSIVE" ? ConsoleColor.Red : ConsoleColor.Yellow;
                Console.WriteLine($"[TripMonitor] ✅ Hành trình #{hanhTrinhId} ({bienSo}): {predictionName} ({gpsData.Count} GPS + {accelData.Count} Accel)");
                Console.ResetColor();
            }
            else
            {
                Console.WriteLine($"[TripMonitor] ⚠️ Hành trình #{hanhTrinhId}: behavior_service HTTP {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[TripMonitor] ⚠️ Hành trình #{hanhTrinhId}: {ex.Message}");
        }
    }
}
