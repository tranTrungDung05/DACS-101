import csv
import os
import threading
import time

import requests


API_URL = "http://localhost:5025/api/Gps/Update"
INTERVAL_SECONDS = 15

VEHICLES_CONFIG = [
    {
        "id": 1,
        "name": "Xe ETA 1",
        "gps_file": "sample_trips/eta_trip_vehicle_1.csv",
    },
    {
        "id": 2,
        "name": "Xe ETA 2",
        "gps_file": "sample_trips/eta_trip_vehicle_2.csv",
    },
]


def load_gps_data(csv_path):
    gps_points = []
    if not os.path.exists(csv_path):
        print(f"[WARN] Missing GPS file: {csv_path}")
        return gps_points

    with open(csv_path, "r", newline="") as handle:
        reader = csv.DictReader(handle)
        for index, row in enumerate(reader, start=2):
            try:
                gps_points.append(
                    {
                        "lat": float(row["lat"]),
                        "lon": float(row["lon"]),
                        "speed_kmh": float(row["speed_kmh"]),
                    }
                )
            except (KeyError, ValueError) as exc:
                print(f"[WARN] Skip row {index} in {csv_path}: {exc}")

    print(f"[INFO] Loaded {len(gps_points)} GPS points from {csv_path}")
    return gps_points


def simulate_vehicle(config):
    points = load_gps_data(config["gps_file"])
    if not points:
        print(f"[WARN] {config['name']}: no valid GPS points, skipping.")
        return

    vehicle_id = config["id"]
    vehicle_name = config["name"]

    print(f"[INFO] Starting {vehicle_name} with {len(points)} points.")

    for index, point in enumerate(points, start=1):
        payload = {
            "VehicleID": vehicle_id,
            "Latitude": point["lat"],
            "Longitude": point["lon"],
            "Speed": point["speed_kmh"],
        }

        try:
            response = requests.post(API_URL, json=payload, timeout=5)
            status = "OK" if response.status_code == 200 else f"ERR {response.status_code}"
            print(
                f"[{vehicle_name}] {index}/{len(points)} {status} "
                f"lat={point['lat']:.6f} lon={point['lon']:.6f} speed={point['speed_kmh']:.1f}"
            )
            if response.status_code != 200:
                print(f"[{vehicle_name}] Response: {response.text[:200]}")
        except requests.RequestException as exc:
            print(f"[{vehicle_name}] Request failed: {exc}")

        if index < len(points):
            time.sleep(INTERVAL_SECONDS)

    print(f"[INFO] Completed {vehicle_name}.")


def main():
    threads = []
    for config in VEHICLES_CONFIG:
        thread = threading.Thread(target=simulate_vehicle, args=(config,), daemon=False)
        thread.start()
        threads.append(thread)
        time.sleep(1)

    for thread in threads:
        thread.join()


if __name__ == "__main__":
    main()
