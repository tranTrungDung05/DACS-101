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
        Task<(double lat, double lon)> SnapToRoad(double lat, double lon);
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
        private const double MAX_SNAP_DISTANCE_METERS = 25.0; // Ngưỡng để không bị "giả"
        
        private static readonly ConcurrentDictionary<int, VehicleCache> _caches = new ConcurrentDictionary<int, VehicleCache>();
        private static readonly ConcurrentDictionary<int, bool> _isFetching = new ConcurrentDictionary<int, bool>();

        private readonly string[] _apiEndpoints = new[] {
            "https://overpass-api.de/api/interpreter",
            "https://overpass.kumi.systems/api/interpreter",
            "https://overpass.nchc.org.tw/api/interpreter"
        };

        public SpeedLimitService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<(double lat, double lon)> SnapToRoad(double lat, double lon)
        {
            try
            {
                using var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(2);
                
                string url = $"http://router.project-osrm.org/nearest/v1/driving/{lon.ToString(CultureInfo.InvariantCulture)},{lat.ToString(CultureInfo.InvariantCulture)}?number=1";
                
                var response = await client.GetAsync(url);
                if (!response.IsSuccessStatusCode) return (lat, lon);

                var jsonString = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(jsonString);
                
                if (doc.RootElement.GetProperty("code").GetString() == "Ok")
                {
                    var waypoint = doc.RootElement.GetProperty("waypoints")[0];
                    double distance = waypoint.GetProperty("distance").GetDouble();

                    if (distance <= MAX_SNAP_DISTANCE_METERS)
                    {
                        var location = waypoint.GetProperty("location");
                        double snappedLon = location[0].GetDouble();
                        double snappedLat = location[1].GetDouble();
                        return (snappedLat, snappedLon);
                    }
                }
            }
            catch (Exception) { }
            return (lat, lon);
        }

        public Task<double> GetMaxSpeed(int vehicleId, double lat, double lon)
        {
            double currentSpeed = DEFAULT_SPEED;
            bool needsUpdate = true;

            if (_caches.TryGetValue(vehicleId, out var cache))
            {
                currentSpeed = cache.Speed;
                double dist = CalculateSimpleDistance(cache.Lat, cache.Lon, lat, lon);
                if (dist < 0.1 && (DateTime.Now - cache.LastUpdate).TotalSeconds < 120)
                {
                    needsUpdate = false;
                }
            }

            if (needsUpdate)
            {
                if (_isFetching.TryAdd(vehicleId, true))
                {
                    _ = Task.Run(async () => 
                    {
                        try { await FetchSpeedFromOsmAsync(vehicleId, lat, lon, currentSpeed); }
                        finally { _isFetching.TryRemove(vehicleId, out _); }
                    });
                }
            }
            return Task.FromResult(currentSpeed);
        }

        private async Task FetchSpeedFromOsmAsync(int vehicleId, double lat, double lon, double fallbackSpeed)
        {
            double resultSpeed = fallbackSpeed;
            bool success = false;
            using var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(10);
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
                                string speedStr = ExtractNumber(ms.GetString());
                                if (double.TryParse(speedStr, out double s) && s > 0)
                                {
                                    foundSpeed = s;
                                    hasMaxSpeed = true;
                                    Console.WriteLine($"[OSM-NGẦM] ✅ Xe {vehicleId}: Cập nhật {foundSpeed}km/h (Bản đồ)");
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
                                Console.WriteLine($"[OSM-NGẦM] ⚠️ Xe {vehicleId}: Cập nhật {foundSpeed}km/h (Suy luận)");
                            }
                        }
                        resultSpeed = foundSpeed;
                        success = true;
                        break;
                    }
                }
                catch (Exception) { continue; }
            }

            var newCache = new VehicleCache { Lat = lat, Lon = lon, Speed = resultSpeed, LastUpdate = DateTime.Now };
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
            return highwayType switch {
                "motorway" => 120, "trunk" => 90, "primary" => 80, "secondary" => 60, "tertiary" => 50, "residential" => 40, _ => DEFAULT_SPEED
            };
        }
    }
}
