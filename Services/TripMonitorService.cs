using DACS.Models;
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
    private const int TRIP_GAP_MINUTES = 5;          // Ngưỡng nghỉ = 5 phút

    public TripMonitorService(IServiceScopeFactory scopeFactory, IConfiguration configuration, ILogger<TripMonitorService> logger)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _logger = logger;
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

            var response = await client.PostAsync($"{behaviorServiceUrl}/predict-split", jsonContent, ct);
            if (response.IsSuccessStatusCode)
            {
                var resultJson = await response.Content.ReadAsStringAsync(ct);
                var result = JsonSerializer.Deserialize<JsonElement>(resultJson);
                var predictionName = result.GetProperty("prediction_name").GetString();

                journey.PhanLoaiHanhVi = predictionName;
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
