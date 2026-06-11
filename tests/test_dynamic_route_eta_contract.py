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

    def test_program_registers_routing_service(self):
        source = Path("Program.cs").read_text(encoding="utf-8")

        self.assertIn("AddScoped<IRoutingService, RoutingService>()", source)

    def test_routing_service_exposes_route_models_and_helpers(self):
        source = Path("Services/RoutingService.cs").read_text(encoding="utf-8")

        self.assertIn("public record RoutePoint", source)
        self.assertIn("public record RouteResult", source)
        self.assertIn("public record RouteState", source)
        self.assertIn("Task<RouteResult?> GetRouteAsync", source)

    def test_gps_controller_uses_route_sample_trip_for_eta_prediction(self):
        source = Path("Controllers/GpsController.cs").read_text(encoding="utf-8")

        self.assertIn("var routeState = await GetOrRefreshRouteStateAsync(", source)
        self.assertIn("routeState.EtaSampleTrip", source)
        self.assertIn("_etaService.PredictAsync(routeState.EtaSampleTrip, etaDestination", source)
        self.assertIn("RouteStateByJourney.TryRemove(journey.IdHanhTrinh", source)

    def test_gps_controller_contains_route_refresh_helper(self):
        source = Path("Controllers/GpsController.cs").read_text(encoding="utf-8")

        self.assertIn("private async Task<RouteState?> GetOrRefreshRouteStateAsync(", source)
        self.assertIn("ShouldRefreshRoute", source)
        self.assertIn("_routingService.GetRouteAsync", source)
        self.assertIn("_routingService.HasVehicleDeviatedFromRoute", source)

    def test_routing_service_generates_reduced_timestamped_sample_trip(self):
        source = Path("Services/RoutingService.cs").read_text(encoding="utf-8")

        self.assertIn("startUnixSeconds + (long)Math.Round(route.DurationSeconds * index / totalSegments)", source)
        self.assertIn("ReducePoints(route.Points, maxPoints)", source)

    def test_routing_service_checks_distance_against_all_route_points(self):
        source = Path("Services/RoutingService.cs").read_text(encoding="utf-8")

        self.assertIn("foreach (var point in routePoints)", source)
        self.assertIn("Math.Min(minDistanceMeters", source)


if __name__ == "__main__":
    unittest.main()
