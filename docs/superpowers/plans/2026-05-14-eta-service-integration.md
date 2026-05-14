# ETA Service Integration Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add realtime ETA prediction to the GPS ingestion flow so each new GPS update can trigger a Python ETA model inference and broadcast a simple ETA message to the frontend over SignalR.

**Architecture:** Keep ETA inference in a dedicated Python HTTP service under `Services`, mirroring the existing `behavior_service.py` deployment style. Add a focused C# `IETAService` wrapper that `GpsController` calls after saving GPS data, then broadcast an ETA event that the map view renders for each vehicle.

**Tech Stack:** ASP.NET Core MVC, SignalR, Entity Framework Core, Python `http.server`, `numpy`, `pandas`, `xgboost`

---

## File Structure

- Create: `Services/eta_inference.py`
- Create: `Services/eta_model_loader.py`
- Create: `Services/eta_service.py`
- Create: `Services/artifacts/eta_models/manifest.json`
- Create: `Services/artifacts/eta_models/global_log_top6.json`
- Create: `Services/artifacts/eta_models/specialized_long_log_top6.json`
- Create: `Services/artifacts/eta_models/specialized_very_long_log_top6.json`
- Create: `Services/ETAService.cs`
- Modify: `Controllers/GpsController.cs`
- Modify: `Program.cs`
- Modify: `appsettings.json`
- Modify: `Views/Home/Index.cshtml`

### Task 1: Add the Python ETA inference unit

**Files:**
- Create: `Services/eta_inference.py`
- Create: `Services/eta_model_loader.py`
- Create: `Services/artifacts/eta_models/manifest.json`
- Create: `Services/artifacts/eta_models/global_log_top6.json`
- Create: `Services/artifacts/eta_models/specialized_long_log_top6.json`
- Create: `Services/artifacts/eta_models/specialized_very_long_log_top6.json`
- Test: `python -m py_compile Services/eta_inference.py Services/eta_model_loader.py`

- [ ] **Step 1: Write the failing import/compile check**

```bash
python -m py_compile Services/eta_inference.py Services/eta_model_loader.py
```

Expected: FAIL with `No such file or directory` because the ETA Python files do not exist yet.

- [ ] **Step 2: Add `Services/eta_model_loader.py` with model bundle loading**

```python
import json
from dataclasses import dataclass
from pathlib import Path

from xgboost import XGBRegressor


@dataclass
class ETAModelBundle:
    feature_columns: list[str]
    long_trip_seconds: int
    very_long_trip_seconds: int
    global_model: XGBRegressor
    long_model: XGBRegressor
    very_long_model: XGBRegressor
    manifest: dict


def _load_xgb_model(model_path: Path) -> XGBRegressor:
    model = XGBRegressor()
    model.load_model(model_path)
    return model


def load_eta_model_bundle(artifact_dir: str | Path = "Services/artifacts/eta_models") -> ETAModelBundle:
    artifact_dir = Path(artifact_dir)
    manifest = json.loads((artifact_dir / "manifest.json").read_text())

    global_model = _load_xgb_model(artifact_dir / "global_log_top6.json")
    long_model = _load_xgb_model(artifact_dir / "specialized_long_log_top6.json")
    very_long_model = _load_xgb_model(artifact_dir / "specialized_very_long_log_top6.json")

    return ETAModelBundle(
        feature_columns=manifest["feature_columns"],
        long_trip_seconds=manifest["thresholds"]["long_trip_seconds"],
        very_long_trip_seconds=manifest["thresholds"]["very_long_trip_seconds"],
        global_model=global_model,
        long_model=long_model,
        very_long_model=very_long_model,
        manifest=manifest,
    )
```

- [ ] **Step 3: Add `Services/eta_inference.py` with feature building and model selection**

```python
import math
from dataclasses import dataclass
from typing import Sequence

import numpy as np
import pandas as pd

from eta_model_loader import ETAModelBundle, load_eta_model_bundle


GPSPoint = tuple[float, float]


@dataclass
class ETAPrediction:
    eta_seconds: float
    eta_minutes: float
    selected_model: str
    route_stage_predictions: dict[str, float]
    features: dict[str, float]


def haversine_distance_km(lon1: float, lat1: float, lon2: float, lat2: float) -> float:
    lon1, lat1, lon2, lat2 = map(math.radians, [lon1, lat1, lon2, lat2])
    dlon = lon2 - lon1
    dlat = lat2 - lat1
    a = math.sin(dlat / 2) ** 2 + math.cos(lat1) * math.cos(lat2) * math.sin(dlon / 2) ** 2
    c = 2 * math.asin(math.sqrt(a))
    return 6371.0 * c


def build_eta_features(points: Sequence[GPSPoint], destination: GPSPoint) -> dict[str, float]:
    if len(points) < 3:
        raise ValueError("Need at least 3 GPS points to build ETA features.")

    start_lon, start_lat = points[0]
    current_lon, current_lat = points[-1]
    dest_lon, dest_lat = destination

    observed_points = len(points)
    elapsed_seconds = (observed_points - 1) * 15
    distance_to_destination = haversine_distance_km(current_lon, current_lat, dest_lon, dest_lat)
    distance_from_start = haversine_distance_km(start_lon, start_lat, current_lon, current_lat)
    total_trip_distance_proxy = distance_from_start + distance_to_destination
    progress_ratio = distance_from_start / total_trip_distance_proxy if total_trip_distance_proxy > 0 else 0.0
    avg_speed_since_start_kmh = distance_from_start / (elapsed_seconds / 3600)

    segment_speeds = []
    for index in range(1, len(points[-4:])):
        lon_a, lat_a = points[-4:][index - 1]
        lon_b, lat_b = points[-4:][index]
        segment_distance_km = haversine_distance_km(lon_a, lat_a, lon_b, lat_b)
        segment_speeds.append(segment_distance_km / (15 / 3600))

    recent_speed_avg_kmh = sum(segment_speeds) / len(segment_speeds)

    return {
        "observed_points": float(observed_points),
        "distance_to_destination": float(distance_to_destination),
        "distance_from_start": float(distance_from_start),
        "progress_ratio": float(progress_ratio),
        "recent_speed_avg_kmh": float(recent_speed_avg_kmh),
        "avg_speed_since_start_kmh": float(avg_speed_since_start_kmh),
    }


def _predict_seconds(model, feature_frame: pd.DataFrame) -> float:
    prediction = np.expm1(model.predict(feature_frame))[0]
    return float(max(prediction, 0.0))


def predict_eta_from_points(
    points: Sequence[GPSPoint],
    destination: GPSPoint,
    bundle: ETAModelBundle | None = None,
) -> ETAPrediction:
    bundle = bundle or load_eta_model_bundle()
    features = build_eta_features(points, destination)
    feature_frame = pd.DataFrame([[features[column] for column in bundle.feature_columns]], columns=bundle.feature_columns)

    global_prediction = _predict_seconds(bundle.global_model, feature_frame)
    stage_predictions = {"global_log_top6": global_prediction}

    if global_prediction < bundle.long_trip_seconds:
        selected_model = "global_log_top6"
        final_prediction = global_prediction
    else:
        long_prediction = _predict_seconds(bundle.long_model, feature_frame)
        stage_predictions["specialized_long_log_top6"] = long_prediction
        if long_prediction < bundle.very_long_trip_seconds:
            selected_model = "specialized_long_log_top6"
            final_prediction = long_prediction
        else:
            very_long_prediction = _predict_seconds(bundle.very_long_model, feature_frame)
            stage_predictions["specialized_very_long_log_top6"] = very_long_prediction
            selected_model = "specialized_very_long_log_top6"
            final_prediction = very_long_prediction

    return ETAPrediction(
        eta_seconds=final_prediction,
        eta_minutes=final_prediction / 60.0,
        selected_model=selected_model,
        route_stage_predictions=stage_predictions,
        features=features,
    )
```

- [ ] **Step 4: Copy the ETA artifact files from `/home/duy/ml_project/artifacts/eta_models` into `Services/artifacts/eta_models`**

```bash
mkdir -p Services/artifacts/eta_models
cp /home/duy/ml_project/artifacts/eta_models/manifest.json Services/artifacts/eta_models/
cp /home/duy/ml_project/artifacts/eta_models/global_log_top6.json Services/artifacts/eta_models/
cp /home/duy/ml_project/artifacts/eta_models/specialized_long_log_top6.json Services/artifacts/eta_models/
cp /home/duy/ml_project/artifacts/eta_models/specialized_very_long_log_top6.json Services/artifacts/eta_models/
```

Expected: the four files exist under `Services/artifacts/eta_models`.

- [ ] **Step 5: Re-run the compile check**

```bash
python -m py_compile Services/eta_inference.py Services/eta_model_loader.py
```

Expected: PASS with no output.

- [ ] **Step 6: Commit**

```bash
git add Services/eta_inference.py Services/eta_model_loader.py Services/artifacts/eta_models
git commit -m "Add ETA Python inference bundle"
```

### Task 2: Expose the ETA Python HTTP service

**Files:**
- Create: `Services/eta_service.py`
- Test: `python -m py_compile Services/eta_service.py`

- [ ] **Step 1: Write the failing compile check for the service entrypoint**

```bash
python -m py_compile Services/eta_service.py
```

Expected: FAIL with `No such file or directory` because `eta_service.py` does not exist yet.

- [ ] **Step 2: Add `Services/eta_service.py` with health and predict endpoints**

```python
import json
from http import HTTPStatus
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from typing import Any

from eta_inference import predict_eta_from_points
from eta_model_loader import load_eta_model_bundle


HOST = "127.0.0.1"
PORT = 8001
BUNDLE = load_eta_model_bundle()


class ETAServiceHandler(BaseHTTPRequestHandler):
    server_version = "ETAService/1.0"

    def _send_json(self, payload: dict[str, Any], status: int = HTTPStatus.OK) -> None:
        body = json.dumps(payload, ensure_ascii=True).encode("utf-8")
        self.send_response(status)
        self.send_header("Content-Type", "application/json; charset=utf-8")
        self.send_header("Content-Length", str(len(body)))
        self.end_headers()
        self.wfile.write(body)

    def _read_json_body(self) -> dict[str, Any]:
        content_length = int(self.headers.get("Content-Length", "0"))
        if content_length <= 0:
            return {}
        raw = self.rfile.read(content_length)
        return json.loads(raw.decode("utf-8"))

    def do_GET(self) -> None:
        if self.path == "/health":
            self._send_json({"status": "ok", "feature_columns": BUNDLE.feature_columns})
            return
        self._send_json({"error": "Not found"}, HTTPStatus.NOT_FOUND)

    def do_POST(self) -> None:
        if self.path != "/predict":
            self._send_json({"error": "Not found"}, HTTPStatus.NOT_FOUND)
            return

        try:
            payload = self._read_json_body()
            gps_points = payload.get("gps_points", [])
            destination = payload.get("destination")

            if len(gps_points) < 3:
                self._send_json({"error": "Need at least 3 GPS points."}, HTTPStatus.BAD_REQUEST)
                return
            if not destination:
                self._send_json({"error": "Missing destination."}, HTTPStatus.BAD_REQUEST)
                return

            point_tuples = [(float(point["lon"]), float(point["lat"])) for point in gps_points]
            destination_tuple = (float(destination["lon"]), float(destination["lat"]))
            prediction = predict_eta_from_points(point_tuples, destination_tuple, bundle=BUNDLE)

            self._send_json(
                {
                    "eta_seconds": prediction.eta_seconds,
                    "eta_minutes": prediction.eta_minutes,
                    "selected_model": prediction.selected_model,
                }
            )
        except Exception as exc:
            self._send_json({"error": str(exc)}, HTTPStatus.BAD_REQUEST)


if __name__ == "__main__":
    server = ThreadingHTTPServer((HOST, PORT), ETAServiceHandler)
    print(f"ETA service listening on http://{HOST}:{PORT}", flush=True)
    server.serve_forever()
```

- [ ] **Step 3: Run the compile check**

```bash
python -m py_compile Services/eta_service.py
```

Expected: PASS with no output.

- [ ] **Step 4: Start the service and verify health**

```bash
python Services/eta_service.py
```

Expected: server starts and prints `ETA service listening on http://127.0.0.1:8001`.

In another terminal:

```bash
curl http://127.0.0.1:8001/health
```

Expected: JSON containing `"status": "ok"`.

- [ ] **Step 5: Commit**

```bash
git add Services/eta_service.py
git commit -m "Add ETA HTTP service"
```

### Task 3: Add the C# ETA client and configuration

**Files:**
- Create: `Services/ETAService.cs`
- Modify: `Program.cs`
- Modify: `appsettings.json`
- Test: `DACS.csproj`

- [ ] **Step 1: Write the failing build**

```bash
dotnet build
```

Expected: PASS before changes, establishing the baseline build for the repo.

- [ ] **Step 2: Add `Services/ETAService.cs`**

```csharp
using System.Text;
using System.Text.Json;

namespace DACS.Services;

public interface IETAService
{
    Task<ETAResult?> PredictAsync(IReadOnlyList<ETAGpsPoint> gpsPoints, CancellationToken cancellationToken = default);
}

public record ETAGpsPoint(double Latitude, double Longitude);
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

    public async Task<ETAResult?> PredictAsync(IReadOnlyList<ETAGpsPoint> gpsPoints, CancellationToken cancellationToken = default)
    {
        if (gpsPoints.Count < 3)
        {
            return null;
        }

        var etaServiceUrl = _configuration.GetValue<string>("EtaServiceUrl") ?? "http://127.0.0.1:8001";
        var destinationLat = _configuration.GetValue<double>("EtaDestinationLat");
        var destinationLon = _configuration.GetValue<double>("EtaDestinationLon");

        var payload = new
        {
            gps_points = gpsPoints.Select(point => new { lat = point.Latitude, lon = point.Longitude }),
            destination = new { lat = destinationLat, lon = destinationLon }
        };

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

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        return new ETAResult(
            document.RootElement.GetProperty("eta_seconds").GetDouble(),
            document.RootElement.GetProperty("eta_minutes").GetDouble(),
            document.RootElement.GetProperty("selected_model").GetString() ?? "unknown"
        );
    }
}
```

- [ ] **Step 3: Register the ETA service in `Program.cs`**

```csharp
builder.Services.AddScoped<IETAService, ETAService>();
```

Expected placement: immediately next to `AddScoped<ISpeedLimitService, SpeedLimitService>();`.

- [ ] **Step 4: Add ETA configuration in `appsettings.json`**

```json
"EtaServiceUrl": "http://127.0.0.1:8001",
"EtaDestinationLat": 10.8015,
"EtaDestinationLon": 106.7111,
```

Expected placement: top-level keys near the existing service/runtime settings.

- [ ] **Step 5: Run the build again**

```bash
dotnet build
```

Expected: PASS with no compile errors from `ETAService.cs` or `Program.cs`.

- [ ] **Step 6: Commit**

```bash
git add Services/ETAService.cs Program.cs appsettings.json
git commit -m "Add C# ETA client service"
```

### Task 4: Trigger ETA from `GpsController` and broadcast it over SignalR

**Files:**
- Modify: `Controllers/GpsController.cs`
- Test: `DACS.csproj`

- [ ] **Step 1: Write the failing build after introducing the constructor dependency**

```bash
dotnet build
```

Expected: PASS before edits, confirming the branch is stable before modifying the controller.

- [ ] **Step 2: Inject `IETAService` into `GpsController`**

```csharp
private readonly IETAService _etaService;

public GpsController(
    ApplicationDbContext context,
    IHubContext<GpsHub> hubContext,
    IConfiguration configuration,
    ISpeedLimitService speedLimitService,
    IHttpClientFactory httpClientFactory,
    IServiceScopeFactory scopeFactory,
    IETAService etaService)
{
    _context = context;
    _hubContext = hubContext;
    _configuration = configuration;
    _speedLimitService = speedLimitService;
    _httpClientFactory = httpClientFactory;
    _scopeFactory = scopeFactory;
    _etaService = etaService;
}
```

- [ ] **Step 3: Add a focused helper to build ETA input from the journey GPS data**

```csharp
private async Task<List<ETAGpsPoint>> LoadJourneyEtaPointsAsync(int journeyId)
{
    return await _context.DuLieuGPS
        .Where(d => d.HanhTrinhIdHanhTrinh == journeyId)
        .OrderBy(d => d.Timestamp)
        .Select(d => new ETAGpsPoint((double)d.ViDo, (double)d.KinhDo))
        .ToListAsync();
}
```

- [ ] **Step 4: Call the ETA service after `await _context.SaveChangesAsync();` in `Update`**

```csharp
var etaPoints = await LoadJourneyEtaPointsAsync(journey.IdHanhTrinh);
if (etaPoints.Count >= 3)
{
    var etaResult = await _etaService.PredictAsync(etaPoints, HttpContext.RequestAborted);
    if (etaResult != null)
    {
        var etaMessage = $"thời gian còn lại đến đích: {etaResult.EtaMinutes:0.0}p";
        await _hubContext.Clients.All.SendAsync("ReceiveEtaUpdate", vehicle.BienSo, etaMessage);
    }
}
```

Expected behavior:
- fewer than 3 points: do nothing
- ETA failure/timeouts: request still succeeds
- ETA success: one new SignalR ETA event per GPS update

- [ ] **Step 5: Run the build again**

```bash
dotnet build
```

Expected: PASS with no controller compile errors.

- [ ] **Step 6: Commit**

```bash
git add Controllers/GpsController.cs
git commit -m "Broadcast ETA from GPS updates"
```

### Task 5: Render ETA updates on the map page

**Files:**
- Modify: `Views/Home/Index.cshtml`
- Test: manual browser verification on the home page

- [ ] **Step 1: Add ETA state to the client-side vehicle store**

```javascript
db[plate] = { pos: [lat, lng], waypoints: [[lat, lng]], detailedRoute: null, violation: null, etaMessage: null };
```

Expected placement: the existing object literal inside `ReceiveLocationUpdate`.

- [ ] **Step 2: Add the new SignalR event handler**

```javascript
connection.on("ReceiveEtaUpdate", (plate, message) => {
    if (!db[plate]) {
        return;
    }

    db[plate].etaMessage = message;

    const item = document.getElementById('item-' + plate.replace(/[-.]/g, ''));
    if (item) {
        let etaNode = item.querySelector('.eta-text');
        if (!etaNode) {
            etaNode = document.createElement('div');
            etaNode.className = 'eta-text text-primary small mt-1';
            item.querySelector('.small.text-muted')?.insertAdjacentElement('afterend', etaNode);
        }
        etaNode.innerText = message;
    }
});
```

- [ ] **Step 3: Seed an ETA placeholder in the sidebar markup for each vehicle row**

```html
<div class="eta-text text-primary small mt-1"></div>
```

Expected placement: inside the vehicle sidebar card, below the existing last-update line.

- [ ] **Step 4: Manually verify the realtime flow**

Run:

```bash
dotnet run
python Services/eta_service.py
```

Then send GPS updates and confirm:
- `ReceiveLocationUpdate` still moves the vehicle marker
- no ETA appears before the 3rd GPS point
- after the 3rd GPS point, the sidebar shows text like `thời gian còn lại đến đích: 12.5p`

- [ ] **Step 5: Commit**

```bash
git add Views/Home/Index.cshtml
git commit -m "Show realtime ETA in map sidebar"
```

## Self-Review

- Spec coverage:
  - Dedicated Python ETA service: covered by Task 1 and Task 2.
  - C# wrapper service and config: covered by Task 3.
  - Trigger ETA from `GpsController` only when at least 3 GPS points exist: covered by Task 4.
  - SignalR ETA push to frontend: covered by Task 4 and Task 5.
  - Simple frontend ETA message: covered by Task 5.
- Placeholder scan:
  - No `TODO`, `TBD`, or generic “add tests later” placeholders remain.
- Type consistency:
  - C# types use `ETAGpsPoint`, `ETAResult`, `IETAService`, and `ETAService` consistently.
  - SignalR event name is consistently `ReceiveEtaUpdate`.
