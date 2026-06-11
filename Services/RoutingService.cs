using System.Globalization;
using System.Text.Json;

namespace DACS.Services;

public record RoutePoint(double Latitude, double Longitude, long? Timestamp = null);

public record RouteResult(
    IReadOnlyList<RoutePoint> Points,
    double DistanceMeters,
    double DurationSeconds);

public record RouteState(
    ETAGpsPoint Destination,
    IReadOnlyList<RoutePoint> RoutePoints,
    IReadOnlyList<ETAGpsPoint> EtaSampleTrip,
    DateTime RefreshedAtUtc);

public interface IRoutingService
{
    Task<RouteResult?> GetRouteAsync(ETAGpsPoint start, ETAGpsPoint destination, CancellationToken cancellationToken = default);
    bool HasVehicleDeviatedFromRoute(ETAGpsPoint currentPoint, IReadOnlyList<RoutePoint> routePoints, double thresholdMeters);
    IReadOnlyList<ETAGpsPoint> BuildEtaSampleTrip(RouteResult route, long startUnixSeconds, int maxPoints = 24);
}

public class RoutingService : IRoutingService
{
    private readonly IHttpClientFactory _httpClientFactory;

    public RoutingService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<RouteResult?> GetRouteAsync(ETAGpsPoint start, ETAGpsPoint destination, CancellationToken cancellationToken = default)
    {
        var client = _httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(5);

        var url =
            $"https://router.project-osrm.org/route/v1/driving/" +
            $"{start.Longitude.ToString(CultureInfo.InvariantCulture)},{start.Latitude.ToString(CultureInfo.InvariantCulture)};" +
            $"{destination.Longitude.ToString(CultureInfo.InvariantCulture)},{destination.Latitude.ToString(CultureInfo.InvariantCulture)}" +
            "?overview=full&geometries=polyline";

        var response = await client.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        if (!document.RootElement.TryGetProperty("routes", out var routes) || routes.GetArrayLength() == 0)
        {
            return null;
        }

        var route = routes[0];
        var geometry = route.GetProperty("geometry").GetString() ?? string.Empty;
        var points = DecodePolyline(geometry);

        if (points.Count == 0)
        {
            points.Add(new RoutePoint(start.Latitude, start.Longitude));
            points.Add(new RoutePoint(destination.Latitude, destination.Longitude));
        }

        return new RouteResult(
            points,
            route.GetProperty("distance").GetDouble(),
            route.GetProperty("duration").GetDouble());
    }

    public bool HasVehicleDeviatedFromRoute(ETAGpsPoint currentPoint, IReadOnlyList<RoutePoint> routePoints, double thresholdMeters)
    {
        return GetMinimumDistanceMeters(currentPoint, routePoints) > thresholdMeters;
    }

    public IReadOnlyList<ETAGpsPoint> BuildEtaSampleTrip(RouteResult route, long startUnixSeconds, int maxPoints = 24)
    {
        var reducedPoints = ReducePoints(route.Points, maxPoints);
        if (reducedPoints.Count == 0)
        {
            return Array.Empty<ETAGpsPoint>();
        }

        var result = new List<ETAGpsPoint>(reducedPoints.Count);
        var totalSegments = Math.Max(1, reducedPoints.Count - 1);

        for (var index = 0; index < reducedPoints.Count; index++)
        {
            var point = reducedPoints[index];
            var timestamp = startUnixSeconds + (long)Math.Round(route.DurationSeconds * index / totalSegments);
            result.Add(new ETAGpsPoint(point.Latitude, point.Longitude, timestamp));
        }

        return result;
    }

    private static List<RoutePoint> DecodePolyline(string encoded)
    {
        var poly = new List<RoutePoint>();
        if (string.IsNullOrWhiteSpace(encoded))
        {
            return poly;
        }

        var index = 0;
        var latitude = 0;
        var longitude = 0;

        while (index < encoded.Length)
        {
            latitude += DecodeNextValue(encoded, ref index);
            longitude += DecodeNextValue(encoded, ref index);
            poly.Add(new RoutePoint(latitude / 1E5, longitude / 1E5));
        }

        return poly;
    }

    private static int DecodeNextValue(string encoded, ref int index)
    {
        var result = 0;
        var shift = 0;
        int b;

        do
        {
            b = encoded[index++] - 63;
            result |= (b & 0x1F) << shift;
            shift += 5;
        }
        while (b >= 0x20 && index < encoded.Length + 1);

        return (result & 1) != 0 ? ~(result >> 1) : (result >> 1);
    }

    private static double GetMinimumDistanceMeters(ETAGpsPoint currentPoint, IReadOnlyList<RoutePoint> routePoints)
    {
        if (routePoints.Count == 0)
        {
            return double.MaxValue;
        }

        var minDistanceMeters = double.MaxValue;

        foreach (var point in routePoints)
        {
            var distanceMeters = HaversineMeters(
                currentPoint.Latitude,
                currentPoint.Longitude,
                point.Latitude,
                point.Longitude);

            minDistanceMeters = Math.Min(minDistanceMeters, distanceMeters);
        }

        return minDistanceMeters;
    }

    private static List<RoutePoint> ReducePoints(IReadOnlyList<RoutePoint> points, int maxPoints)
    {
        if (points.Count <= maxPoints)
        {
            return points.ToList();
        }

        var reduced = new List<RoutePoint>(maxPoints);
        for (var index = 0; index < maxPoints; index++)
        {
            var mappedIndex = (int)Math.Round((points.Count - 1) * (index / (double)(maxPoints - 1)));
            reduced.Add(points[mappedIndex]);
        }

        return reduced;
    }

    private static double HaversineMeters(double lat1, double lon1, double lat2, double lon2)
    {
        const double earthRadiusMeters = 6371000;
        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);
        var a =
            Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
            Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
            Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return earthRadiusMeters * c;
    }

    private static double ToRadians(double degrees)
    {
        return degrees * (Math.PI / 180);
    }
}
