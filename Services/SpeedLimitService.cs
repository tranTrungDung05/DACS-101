using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Linq;
using System.Globalization;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace DACS.Services
{
    public interface ISpeedLimitService
    {
        Task<double> GetMaxSpeed(int vehicleId, double lat, double lon);
    }

    public class VehicleCache
    {
        public double Lat { get; set; }
        public double Lon { get; set; }
        public double Speed { get; set; }
        public DateTime LastUpdate { get; set; }
    }

    public class SpeedLimitService : ISpeedLimitService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private const double DEFAULT_SPEED = 50.0;
        
        // Cache riêng biệt cho từng xe
        private static readonly ConcurrentDictionary<int, VehicleCache> _caches = new ConcurrentDictionary<int, VehicleCache>();
        
        // Cờ đánh dấu xe nào đang được gọi API ngầm (tránh spam API)
        private static readonly ConcurrentDictionary<int, bool> _isFetching = new ConcurrentDictionary<int, bool>();

        // Danh sách các Server Overpass dự phòng
        private readonly string[] _apiEndpoints = new[] {
            "https://overpass-api.de/api/interpreter",
            "https://overpass.kumi.systems/api/interpreter",
            "https://overpass.nchc.org.tw/api/interpreter"
        };

        public SpeedLimitService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public Task<double> GetMaxSpeed(int vehicleId, double lat, double lon)
        {
            double currentSpeed = DEFAULT_SPEED;
            bool needsUpdate = true;

            // 1. Kiểm tra Cache
            if (_caches.TryGetValue(vehicleId, out var cache))
            {
                currentSpeed = cache.Speed;
                double dist = CalculateSimpleDistance(cache.Lat, cache.Lon, lat, lon);
                
                // Nới lỏng điều kiện Cache: Nếu xe di chuyển < 100m và dữ liệu chưa quá 2 phút
                if (dist < 0.1 && (DateTime.Now - cache.LastUpdate).TotalSeconds < 120)
                {
                    needsUpdate = false;
                }
            }

            // 2. Kích hoạt tiến trình hỏi API NGẦM (Fire and Forget)
            if (needsUpdate)
            {
                // Tránh tình trạng 1 xe gọi API 10 lần cùng lúc khi chưa có kết quả
                if (_isFetching.TryAdd(vehicleId, true))
                {
                    // Task.Run giúp API chạy độc lập, không làm đứng chương trình chính
                    _ = Task.Run(async () => 
                    {
                        try
                        {
                            await FetchSpeedFromOsmAsync(vehicleId, lat, lon, currentSpeed);
                        }
                        finally
                        {
                            // Giải phóng cờ báo khi xong việc
                            _isFetching.TryRemove(vehicleId, out _);
                        }
                    });
                }
            }

            // 3. TRẢ VỀ NGAY LẬP TỨC (0s Timeout) tốc độ hiện tại trong Cache (hoặc mặc định)
            return Task.FromResult(currentSpeed);
        }

        private async Task FetchSpeedFromOsmAsync(int vehicleId, double lat, double lon, double fallbackSpeed)
        {
            double resultSpeed = fallbackSpeed;
            bool success = false;

            // Tạo một HttpClient mới an toàn cho luồng chạy ngầm
            using var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(10); // Cho phép chờ hẳn 10s vì đã chạy ngầm
            client.DefaultRequestHeaders.Add("User-Agent", "DACS-GpsTracking-App/2.0");

            foreach (var endpoint in _apiEndpoints)
            {
                try
                {
                    string query = $@"
                        [out:json][timeout:10];
                        way(around:50,{lat.ToString(CultureInfo.InvariantCulture)},{lon.ToString(CultureInfo.InvariantCulture)})[maxspeed];
                        out body;
                        way(around:50,{lat.ToString(CultureInfo.InvariantCulture)},{lon.ToString(CultureInfo.InvariantCulture)})[highway];
                        out body;
                    ";

                    var content = new FormUrlEncodedContent(new[] { new KeyValuePair<string, string>("data", query) });
                    var response = await client.PostAsync(endpoint, content);
                    
                    if (!response.IsSuccessStatusCode) continue;

                    var jsonString = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(jsonString);
                    var elements = doc.RootElement.GetProperty("elements");

                    if (elements.GetArrayLength() > 0)
                    {
                        double foundSpeed = DEFAULT_SPEED;
                        bool hasMaxSpeed = false;

                        foreach (var el in elements.EnumerateArray())
                        {
                            if (el.TryGetProperty("tags", out var tags) && tags.TryGetProperty("maxspeed", out var ms))
                            {
                                if (double.TryParse(ExtractNumber(ms.GetString()), out double s))
                                {
                                    foundSpeed = s;
                                    hasMaxSpeed = true;
                                    Console.ForegroundColor = ConsoleColor.Green;
                                    Console.WriteLine($"[OSM-NGẦM] ✅ Xe {vehicleId}: Cập nhật {foundSpeed}km/h (Bản đồ) tại ({lat:F4}, {lon:F4})");
                                    Console.ResetColor();
                                    break;
                                }
                            }
                        }

                        if (!hasMaxSpeed)
                        {
                            var first = elements.EnumerateArray().First();
                            if (first.TryGetProperty("tags", out var tags) && tags.TryGetProperty("highway", out var hw))
                            {
                                foundSpeed = MapHighwayToSpeedVn(hw.GetString());
                                Console.ForegroundColor = ConsoleColor.Yellow;
                                Console.WriteLine($"[OSM-NGẦM] ⚠️ Xe {vehicleId}: Cập nhật {foundSpeed}km/h (Suy luận: {hw.GetString()}) tại ({lat:F4}, {lon:F4})");
                                Console.ResetColor();
                            }
                        }

                        resultSpeed = foundSpeed;
                        success = true;
                        break; // Thành công thì thoát vòng lặp server
                    }
                }
                catch (Exception)
                {
                    continue; // Lỗi thì thử Server tiếp theo
                }
            }

            if (!success)
            {
                // Nếu cả 3 server đều chết, in ra màu Xám để khỏi rối mắt
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine($"[OSM-NGẦM] ❌ Xe {vehicleId}: Không phản hồi. Giữ nguyên {resultSpeed}km/h");
                Console.ResetColor();
            }

            // Âm thầm cập nhật Cache mới
            var newCache = new VehicleCache {
                Lat = lat, Lon = lon, Speed = resultSpeed, LastUpdate = DateTime.Now
            };
            _caches.AddOrUpdate(vehicleId, newCache, (id, old) => newCache);
        }

        private double CalculateSimpleDistance(double lat1, double lon1, double lat2, double lon2)
        {
            return Math.Sqrt(Math.Pow(lat2 - lat1, 2) + Math.Pow(lon2 - lon1, 2)) * 111;
        }

        private string ExtractNumber(string? input)
        {
            if (string.IsNullOrEmpty(input)) return "0";
            return new string(input.TakeWhile(c => char.IsDigit(c)).ToArray());
        }

        private double MapHighwayToSpeedVn(string? highwayType)
        {
            return highwayType switch
            {
                "motorway" => 120,
                "trunk" => 90,
                "primary" => 80,
                "secondary" => 60,
                "tertiary" => 50,
                "residential" => 40,
                _ => DEFAULT_SPEED
            };
        }
    }
}
