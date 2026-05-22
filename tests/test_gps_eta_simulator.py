import tempfile
import unittest
from pathlib import Path

from gps_eta_simulator import (
    build_payload,
    format_eta_status,
    load_gps_points,
    resolve_eta_destination,
    resolve_request_timeout_seconds,
)


class GpsEtaSimulatorTests(unittest.TestCase):
    def test_load_gps_points_accepts_repo_csv_header(self):
        with tempfile.TemporaryDirectory() as temp_dir:
            csv_path = Path(temp_dir) / "trip.csv"
            csv_path.write_text(
                "lat,lon,speed\n"
                "41.140629,-8.615538,0.0\n"
                "41.140746,-8.615421,3.9\n",
                encoding="utf-8",
            )

            points = load_gps_points(csv_path)

        self.assertEqual(2, len(points))
        self.assertEqual(41.140629, points[0]["lat"])
        self.assertEqual(-8.615538, points[0]["lon"])
        self.assertEqual(0.0, points[0]["speed"])

    def test_build_payload_only_includes_gps_fields(self):
        payload = build_payload(
            vehicle_id=13,
            point={"lat": 41.140629, "lon": -8.615538, "speed": 12.5},
        )

        self.assertEqual(
            {
                "VehicleID": 13,
                "Latitude": 41.140629,
                "Longitude": -8.615538,
                "Speed": 12.5,
            },
            payload,
        )

    def test_request_timeout_is_capped_by_send_interval(self):
        self.assertEqual(15, resolve_request_timeout_seconds(interval_seconds=15, requested_timeout_seconds=20))
        self.assertEqual(10, resolve_request_timeout_seconds(interval_seconds=15, requested_timeout_seconds=10))

    def test_resolve_eta_destination_uses_vehicle_override_then_fallback(self):
        with tempfile.TemporaryDirectory() as temp_dir:
            appsettings_path = Path(temp_dir) / "appsettings.json"
            appsettings_path.write_text(
                """
                {
                  "EtaDestinationLat": 41.149881,
                  "EtaDestinationLon": -8.620893,
                  "EtaDestinations": {
                    "2": { "Lat": 40.466999, "Lon": -3.471226 }
                  }
                }
                """.strip(),
                encoding="utf-8",
            )

            self.assertEqual((40.466999, -3.471226), resolve_eta_destination(2, appsettings_path))
            self.assertEqual((41.149881, -8.620893), resolve_eta_destination(1, appsettings_path))

    def test_format_eta_status_handles_pending_arrived_and_eta(self):
        self.assertEqual("ETA: chua du du lieu", format_eta_status(None, False))
        self.assertEqual("ETA: da toi noi", format_eta_status(None, True))
        self.assertEqual("ETA vehicle 1: 9.1p (544.3s)", format_eta_status({"eta_minutes": 9.0716, "eta_seconds": 544.3}, False, vehicle_id=1))


if __name__ == "__main__":
    unittest.main()
