import csv
import tempfile
import unittest
from pathlib import Path

from scripts.export_trip_gps import export_trip_gps_csv


class ExportTripGpsCsvTests(unittest.TestCase):
    def test_exports_tail_valid_trip_to_lat_lon_speed_csv(self):
        with tempfile.TemporaryDirectory() as tmpdir:
            data_path = Path(tmpdir) / "train.csv"
            output_path = Path(tmpdir) / "trip.csv"

            data_path.write_text(
                "\n".join(
                    [
                        "TRIP_ID,CALL_TYPE,ORIGIN_CALL,ORIGIN_STAND,TAXI_ID,TIMESTAMP,DAY_TYPE,MISSING_DATA,POLYLINE",
                        '"trip-empty","C","","","1","1","A","False","[]"',
                        '"trip-missing","C","","","1","1","A","True","[[-8.1,41.1],[-8.2,41.2]]"',
                        '"trip-good","C","","","1","1","A","False","[[-8.6100,41.1400],[-8.6110,41.1410],[-8.6120,41.1420]]"',
                    ]
                )
            )

            trip_id = export_trip_gps_csv(data_path, output_path)

            self.assertEqual(trip_id, "trip-good")

            with output_path.open(newline="") as csv_file:
                rows = list(csv.DictReader(csv_file))

            self.assertEqual(rows[0].keys(), {"lat", "lon", "speed"})
            self.assertEqual(len(rows), 3)
            self.assertEqual(rows[0]["lat"], "41.14")
            self.assertEqual(rows[0]["lon"], "-8.61")
            self.assertEqual(rows[0]["speed"], "0.0")
            self.assertGreater(float(rows[1]["speed"]), 0.0)
            self.assertGreater(float(rows[2]["speed"]), 0.0)


if __name__ == "__main__":
    unittest.main()
