import unittest
from pathlib import Path


class EtaApiContractTests(unittest.TestCase):
    def test_app_source_uses_csharp_eta_payload_shape(self):
        source = Path("Services/app.py").read_text(encoding="utf-8")

        self.assertIn("gps_points", source)
        self.assertIn("destination: DestinationPoint", source)
        self.assertIn("request.gps_points", source)
        self.assertIn("request.destination.lon", source)
        self.assertIn("request.destination.lat", source)


if __name__ == "__main__":
    unittest.main()
