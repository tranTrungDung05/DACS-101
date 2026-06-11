# Dynamic Route ETA Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace raw trip-history ETA input with a dynamically refreshed optimal-route sample trip while keeping ETA inference inside the existing `eta_serving` service.

**Architecture:** Add a focused C# routing layer that fetches an OSRM route from the vehicle's current position to its configured destination, stores an in-memory route state per journey, detects off-route movement, and regenerates a reduced route-derived sample trip for `IETAService`. Keep `GpsController` as the request entrypoint and preserve the existing SignalR ETA event contract.

**Tech Stack:** ASP.NET Core 8, C#, xUnit/unittest-style existing tests, SignalR, `HttpClient`, OSRM public routing API, existing Python `eta_serving`

---

## File Map

- Create: `Services/RoutingService.cs`
  Purpose: OSRM route fetch, route geometry decode, sample-trip generation, off-route detection helpers, in-memory route models.
- Modify: `Program.cs`
  Purpose: register the new routing service.
- Modify: `Controllers/GpsController.cs`
  Purpose: replace raw ETA point buffer flow with route-state flow and reroute logic.
- Create: `tests/test_dynamic_route_eta_contract.py`
  Purpose: guard the C# source integration contract for route-based ETA flow.

### Task 1: Add contract tests for dynamic route ETA flow

**Files:**
- Create: `tests/test_dynamic_route_eta_contract.py`
- Test: `tests/test_dynamic_route_eta_contract.py`

- [ ] **Step 1: Write the failing test**

```python
from pathlib import Path
import unittest


class DynamicRouteEtaContractTests(unittest.TestCase):
    def test_gps_controller_uses_routing_service_and_route_state(self):
        source = Path("Controllers/GpsController.cs").read_text(encoding="utf-8")

        self.assertIn("IRoutingService", source)
        self.assertIn("RouteStateByJourney", source)
        self.assertIn("GetOrRefreshRouteStateAsync", source)
        self.assertIn("HasVehicleDeviatedFromRoute", source)
        self.assertIn("BuildEtaSampleTrip", source)

    def test_gps_controller_no_longer_uses_raw_eta_point_buffer(self):
        source = Path("Controllers/GpsController.cs").read_text(encoding="utf-8")

        self.assertNotIn("EtaRawPointBuffer", source)
        self.assertNotIn("AppendAndGetEtaPoints", source)

    def test_routing_service_contains_osrm_and_sample_trip_support(self):
        source = Path("Services/RoutingService.cs").read_text(encoding="utf-8")

        self.assertIn("router.project-osrm.org", source)
        self.assertIn("BuildEtaSampleTrip", source)
        self.assertIn("HasVehicleDeviatedFromRoute", source)
        self.assertIn("DecodePolyline", source)


if __name__ == "__main__":
    unittest.main()
```

- [ ] **Step 2: Run test to verify it fails**

Run: `python -m unittest tests.test_dynamic_route_eta_contract -v`

Expected: FAIL because `Services/RoutingService.cs` does not exist yet and `Controllers/GpsController.cs` does not contain the route-state flow.

- [ ] **Step 3: Write minimal implementation scaffolding**

```csharp
// Services/RoutingService.cs
namespace DACS.Services;

public interface IRoutingService {}

public class RoutingService : IRoutingService {}
```

```csharp
// Controllers/GpsController.cs
// Add placeholders only long enough to satisfy the first contract test:
// - RouteStateByJourney
// - GetOrRefreshRouteStateAsync
// - HasVehicleDeviatedFromRoute
// - BuildEtaSampleTrip
```

- [ ] **Step 4: Run test to verify it passes**

Run: `python -m unittest tests.test_dynamic_route_eta_contract -v`

Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add tests/test_dynamic_route_eta_contract.py Services/RoutingService.cs Controllers/GpsController.cs
git commit -m "test: add dynamic route ETA contract coverage"
```

### Task 2: Build the routing service with OSRM route fetch and route helpers

**Files:**
- Create: `Services/RoutingService.cs`
- Modify: `Program.cs`
- Test: `tests/test_dynamic_route_eta_contract.py`

- [ ] **Step 1: Expand the failing test to cover service registration and route helper API**

```python
def test_program_registers_routing_service(self):
    source = Path("Program.cs").read_text(encoding="utf-8")

    self.assertIn("AddScoped<IRoutingService, RoutingService>()", source)

def test_routing_service_exposes_route_models_and_helpers(self):
    source = Path("Services/RoutingService.cs").read_text(encoding="utf-8")

    self.assertIn("public record RoutePoint", source)
    self.assertIn("public record RouteResult", source)
    self.assertIn("public record RouteState", source)
    self.assertIn("Task<RouteResult?> GetRouteAsync", source)
```

- [ ] **Step 2: Run test to verify it fails**

Run: `python -m unittest tests.test_dynamic_route_eta_contract -v`

Expected: FAIL because `Program.cs` is missing the registration and the route models are not implemented yet.

- [ ] **Step 3: Write minimal implementation**

```csharp
using System.Globalization;
using System.Text.Json;

namespace DACS.Services;

public record RoutePoint(double Latitude, double Longitude, long? Timestamp = null);
public record RouteResult(IReadOnlyList<RoutePoint> Points, double DistanceMeters, double DurationSeconds);
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
        var route = document.RootElement.GetProperty("routes")[0];
        var geometry = route.GetProperty("geometry").GetString() ?? string.Empty;
        var points = DecodePolyline(geometry);

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

    private static List<RoutePoint> DecodePolyline(string encoded) { return new(); }
    private static double GetMinimumDistanceMeters(ETAGpsPoint currentPoint, IReadOnlyList<RoutePoint> routePoints) { return 0; }
    private static List<RoutePoint> ReducePoints(IReadOnlyList<RoutePoint> points, int maxPoints) { return points.ToList(); }
}
```

```csharp
// Program.cs
builder.Services.AddScoped<IRoutingService, RoutingService>();
```

- [ ] **Step 4: Run test to verify it passes**

Run: `python -m unittest tests.test_dynamic_route_eta_contract -v`

Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add Services/RoutingService.cs Program.cs tests/test_dynamic_route_eta_contract.py
git commit -m "feat: add OSRM routing service scaffolding"
```

### Task 3: Switch GpsController from raw ETA history to route state

**Files:**
- Modify: `Controllers/GpsController.cs`
- Test: `tests/test_dynamic_route_eta_contract.py`

- [ ] **Step 1: Expand the failing test to require route-state-driven ETA calls**

```python
def test_gps_controller_uses_route_sample_trip_for_eta_prediction(self):
    source = Path("Controllers/GpsController.cs").read_text(encoding="utf-8")

    self.assertIn("var routeState = await GetOrRefreshRouteStateAsync(", source)
    self.assertIn("routeState.EtaSampleTrip", source)
    self.assertIn("_etaService.PredictAsync(routeState.EtaSampleTrip, etaDestination", source)
    self.assertIn("RouteStateByJourney.TryRemove(journey.IdHanhTrinh", source)
```

- [ ] **Step 2: Run test to verify it fails**

Run: `python -m unittest tests.test_dynamic_route_eta_contract -v`

Expected: FAIL because `GpsController.cs` still sends `etaPoints` or does not yet use route state end-to-end.

- [ ] **Step 3: Write minimal implementation**

```csharp
// Controllers/GpsController.cs
private const double RouteDeviationThresholdMeters = 75;
private const int RouteRefreshCooldownSeconds = 15;
private static readonly ConcurrentDictionary<int, RouteState> RouteStateByJourney = new();

private readonly IRoutingService _routingService;

public GpsController(..., IETAService etaService, IRoutingService routingService)
{
    ...
    _etaService = etaService;
    _routingService = routingService;
}

// When closing a stale trip:
RouteStateByJourney.TryRemove(journey.IdHanhTrinh, out _);

// Replace eta buffer flow:
var etaDestination = ResolveEtaDestination(vehicle.IdPhuongTien);
var currentEtaPoint = new ETAGpsPoint(rawLatitude, rawLongitude, DateTimeOffset.UtcNow.ToUnixTimeSeconds());
var routeState = await GetOrRefreshRouteStateAsync(journey.IdHanhTrinh, currentEtaPoint, etaDestination, HttpContext.RequestAborted);

if (distanceToEtaDestinationKm <= EtaArrivalThresholdKm)
{
    ...
}
else if (routeState != null && routeState.EtaSampleTrip.Count >= 3)
{
    var etaResult = await _etaService.PredictAsync(routeState.EtaSampleTrip, etaDestination, HttpContext.RequestAborted);
    ...
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `python -m unittest tests.test_dynamic_route_eta_contract -v`

Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add Controllers/GpsController.cs tests/test_dynamic_route_eta_contract.py
git commit -m "feat: switch GPS ETA flow to route state"
```

### Task 4: Implement route refresh and deviation detection helpers

**Files:**
- Modify: `Controllers/GpsController.cs`
- Modify: `Services/RoutingService.cs`
- Test: `tests/test_dynamic_route_eta_contract.py`

- [ ] **Step 1: Expand the failing test to require reroute behavior helpers**

```python
def test_gps_controller_contains_route_refresh_helper(self):
    source = Path("Controllers/GpsController.cs").read_text(encoding="utf-8")

    self.assertIn("private async Task<RouteState?> GetOrRefreshRouteStateAsync(", source)
    self.assertIn("ShouldRefreshRoute", source)
    self.assertIn("_routingService.GetRouteAsync", source)
    self.assertIn("_routingService.HasVehicleDeviatedFromRoute", source)
```

- [ ] **Step 2: Run test to verify it fails**

Run: `python -m unittest tests.test_dynamic_route_eta_contract -v`

Expected: FAIL because the helper methods are not fully implemented yet.

- [ ] **Step 3: Write minimal implementation**

```csharp
private async Task<RouteState?> GetOrRefreshRouteStateAsync(
    int journeyId,
    ETAGpsPoint currentPoint,
    ETAGpsPoint destination,
    CancellationToken cancellationToken)
{
    if (RouteStateByJourney.TryGetValue(journeyId, out var existingState) &&
        !ShouldRefreshRoute(existingState, currentPoint, destination))
    {
        return existingState;
    }

    var route = await _routingService.GetRouteAsync(currentPoint, destination, cancellationToken);
    if (route == null)
    {
        return existingState;
    }

    var etaSampleTrip = _routingService.BuildEtaSampleTrip(route, currentPoint.Timestamp ?? DateTimeOffset.UtcNow.ToUnixTimeSeconds());
    var refreshedState = new RouteState(destination, route.Points, etaSampleTrip, DateTime.UtcNow);
    RouteStateByJourney[journeyId] = refreshedState;
    return refreshedState;
}

private bool ShouldRefreshRoute(RouteState state, ETAGpsPoint currentPoint, ETAGpsPoint destination)
{
    if (Math.Abs(state.Destination.Latitude - destination.Latitude) > 0.000001 ||
        Math.Abs(state.Destination.Longitude - destination.Longitude) > 0.000001)
    {
        return true;
    }

    if ((DateTime.UtcNow - state.RefreshedAtUtc).TotalSeconds < RouteRefreshCooldownSeconds)
    {
        return false;
    }

    return _routingService.HasVehicleDeviatedFromRoute(currentPoint, state.RoutePoints, RouteDeviationThresholdMeters);
}
```

```csharp
// Services/RoutingService.cs
// Replace placeholders with real implementations for:
// - DecodePolyline
// - GetMinimumDistanceMeters
// - ReducePoints
```

- [ ] **Step 4: Run test to verify it passes**

Run: `python -m unittest tests.test_dynamic_route_eta_contract -v`

Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add Controllers/GpsController.cs Services/RoutingService.cs tests/test_dynamic_route_eta_contract.py
git commit -m "feat: add route deviation refresh logic"
```

### Task 5: Complete route geometry helpers and sample-trip generation

**Files:**
- Modify: `Services/RoutingService.cs`
- Test: `tests/test_dynamic_route_eta_contract.py`

- [ ] **Step 1: Expand the failing test to verify implementation details**

```python
def test_routing_service_generates_reduced_timestamped_sample_trip(self):
    source = Path("Services/RoutingService.cs").read_text(encoding="utf-8")

    self.assertIn("startUnixSeconds + (long)Math.Round(route.DurationSeconds * index / totalSegments)", source)
    self.assertIn("ReducePoints(route.Points, maxPoints)", source)

def test_routing_service_checks_distance_against_all_route_points(self):
    source = Path("Services/RoutingService.cs").read_text(encoding="utf-8")

    self.assertIn("foreach (var point in routePoints)", source)
    self.assertIn("Math.Min(minDistanceMeters", source)
```

- [ ] **Step 2: Run test to verify it fails**

Run: `python -m unittest tests.test_dynamic_route_eta_contract -v`

Expected: FAIL if helper internals are still placeholders or if reduction logic is missing.

- [ ] **Step 3: Write minimal implementation**

```csharp
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

private static double GetMinimumDistanceMeters(ETAGpsPoint currentPoint, IReadOnlyList<RoutePoint> routePoints)
{
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
```

- [ ] **Step 4: Run test to verify it passes**

Run: `python -m unittest tests.test_dynamic_route_eta_contract -v`

Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add Services/RoutingService.cs tests/test_dynamic_route_eta_contract.py
git commit -m "feat: generate reduced route sample trips"
```

### Task 6: Verify end-to-end build and regression coverage

**Files:**
- Modify: `Controllers/GpsController.cs`
- Modify: `Services/RoutingService.cs`
- Modify: `Program.cs`
- Test: `tests/test_dynamic_route_eta_contract.py`
- Test: `tests/test_eta_api_contract.py`
- Test: `tests/test_gps_eta_simulator.py`

- [ ] **Step 1: Run the focused Python test suite**

Run: `python -m unittest tests.test_dynamic_route_eta_contract tests.test_eta_api_contract tests.test_gps_eta_simulator -v`

Expected: PASS with zero failures.

- [ ] **Step 2: Run the .NET build**

Run: `dotnet build`

Expected: `Build succeeded.` with exit code `0`.

- [ ] **Step 3: Review the final diff for accidental scope creep**

Run: `git diff -- Controllers/GpsController.cs Services/RoutingService.cs Program.cs tests/test_dynamic_route_eta_contract.py`

Expected: only route ETA changes, no unrelated edits.

- [ ] **Step 4: Commit**

```bash
git add Controllers/GpsController.cs Services/RoutingService.cs Program.cs tests/test_dynamic_route_eta_contract.py
git commit -m "feat: add dynamic route-based ETA rerouting"
```
