using System.Text;
using System.Text.Json;

namespace DACS.Services;

public interface IETAService
{
    Task<ETAResult?> PredictAsync(IReadOnlyList<ETAGpsPoint> gpsPoints, ETAGpsPoint destination, CancellationToken cancellationToken = default);
}

public record ETAGpsPoint(double Latitude, double Longitude, long? Timestamp = null);
public record ETAResult(double EtaSeconds, double EtaMinutes, string SelectedModel);

public class ETAService : IETAService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ETAService> _logger;

    public ETAService(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<ETAService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<ETAResult?> PredictAsync(IReadOnlyList<ETAGpsPoint> gpsPoints, ETAGpsPoint destination, CancellationToken cancellationToken = default)
    {
        if (gpsPoints.Count < 3)
        {
            return null;
        }

        var etaServiceUrl = _configuration.GetValue<string>("EtaServiceUrl") ?? "http://127.0.0.1:8001";

        var payload = new
        {
            gps_points = gpsPoints.Select(point => new { 
                lat = point.Latitude, 
                lon = point.Longitude,
                timestamp = point.Timestamp
            }),
            destination = new { lat = destination.Latitude, lon = destination.Longitude }
        };

        try
        {
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(5);

            var response = await client.PostAsync(
                $"{etaServiceUrl}/predict",
                new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"),
                cancellationToken
            );

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("ETA service returned {StatusCode}: {Body}", response.StatusCode, body);
                return null;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            return new ETAResult(
                document.RootElement.GetProperty("eta_seconds").GetDouble(),
                document.RootElement.GetProperty("eta_minutes").GetDouble(),
                document.RootElement.GetProperty("selected_model").GetString() ?? "unknown"
            );
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("ETA service request timed out.");
            return null;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Unable to reach ETA service.");
            return null;
        }
    }
}
