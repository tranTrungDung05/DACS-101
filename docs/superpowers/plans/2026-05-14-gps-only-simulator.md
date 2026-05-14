# GPS-Only ETA Simulator Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a separate GPS-only simulator that reads per-vehicle CSV trips and posts only GPS payloads every 15 seconds to drive the ETA realtime flow.

**Architecture:** Keep the existing `simulator.py` untouched and add a new single-purpose script for ETA demos. Store normalized trip CSVs inside the repo so the simulator does not depend on paths outside the project.

**Tech Stack:** Python, `requests`, CSV files, threaded per-vehicle playback

---

## File Structure

- Create: `gps_eta_simulator.py`
- Create: `sample_trips/eta_trip_vehicle_1.csv`
- Create: `sample_trips/eta_trip_vehicle_2.csv`

### Task 1: Prepare GPS-only trip CSV assets

**Files:**
- Create: `sample_trips/eta_trip_vehicle_1.csv`
- Create: `sample_trips/eta_trip_vehicle_2.csv`
- Test: inspect first rows after copy/normalize

- [ ] **Step 1: Identify source GPS trip files**

Run:

```bash
find /home/duy/ml_project -maxdepth 2 -type f \( -name '*gps*.csv' -o -name '*trip*.csv' \) | sort
```

Expected: locate candidate GPS CSV files to normalize for the simulator.

- [ ] **Step 2: Copy two source GPS trip files into the repo**

Run:

```bash
mkdir -p sample_trips
cp /home/duy/uah_trip_gps_stream.csv sample_trips/eta_trip_vehicle_1_raw.csv
cp /home/duy/uah_trip_aggressive_gps_200_300s.csv sample_trips/eta_trip_vehicle_2_raw.csv
```

Expected: two raw GPS files exist locally under `sample_trips/`.

- [ ] **Step 3: Normalize the first CSV to `lat,lon,speed_kmh`**

Run:

```bash
python - <<'PY'
import csv
from pathlib import Path

source = Path("sample_trips/eta_trip_vehicle_1_raw.csv")
target = Path("sample_trips/eta_trip_vehicle_1.csv")

with source.open() as infile, target.open("w", newline="") as outfile:
    reader = csv.DictReader(infile)
    writer = csv.DictWriter(outfile, fieldnames=["lat", "lon", "speed_kmh"])
    writer.writeheader()
    for row in reader:
        writer.writerow(
            {
                "lat": row["lat"],
                "lon": row["lon"],
                "speed_kmh": row["speed_kmh"],
            }
        )
PY
```

Expected: `sample_trips/eta_trip_vehicle_1.csv` contains exactly the three target columns.

- [ ] **Step 4: Normalize the second CSV to `lat,lon,speed_kmh`**

Run:

```bash
python - <<'PY'
import csv
from pathlib import Path

source = Path("sample_trips/eta_trip_vehicle_2_raw.csv")
target = Path("sample_trips/eta_trip_vehicle_2.csv")

with source.open() as infile, target.open("w", newline="") as outfile:
    reader = csv.DictReader(infile)
    writer = csv.DictWriter(outfile, fieldnames=["lat", "lon", "speed_kmh"])
    writer.writeheader()
    for row in reader:
        writer.writerow(
            {
                "lat": row["lat"],
                "lon": row["lon"],
                "speed_kmh": row["speed_kmh"],
            }
        )
PY
```

Expected: `sample_trips/eta_trip_vehicle_2.csv` contains exactly the three target columns.

- [ ] **Step 5: Verify the normalized files**

Run:

```bash
sed -n '1,5p' sample_trips/eta_trip_vehicle_1.csv
sed -n '1,5p' sample_trips/eta_trip_vehicle_2.csv
```

Expected: both files start with:

```text
lat,lon,speed_kmh
```

- [ ] **Step 6: Remove temporary raw copies**

Run:

```bash
rm -f sample_trips/eta_trip_vehicle_1_raw.csv sample_trips/eta_trip_vehicle_2_raw.csv
```

Expected: only normalized CSVs remain in `sample_trips/`.

- [ ] **Step 7: Commit**

```bash
git add sample_trips/eta_trip_vehicle_1.csv sample_trips/eta_trip_vehicle_2.csv
git commit -m "Add GPS trip samples for ETA simulator"
```

### Task 2: Add the GPS-only simulator script

**Files:**
- Create: `gps_eta_simulator.py`
- Test: `python -m py_compile gps_eta_simulator.py`

- [ ] **Step 1: Write the failing compile check**

Run:

```bash
python -m py_compile gps_eta_simulator.py
```

Expected: FAIL because the file does not exist yet.

- [ ] **Step 2: Create `gps_eta_simulator.py`**

```python
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
```

- [ ] **Step 3: Run the compile check**

Run:

```bash
python -m py_compile gps_eta_simulator.py
```

Expected: PASS with no output.

- [ ] **Step 4: Smoke-run the simulator against the local API**

Run:

```bash
python gps_eta_simulator.py
```

Expected:
- each configured vehicle starts in its own thread
- logs show only GPS payload progress
- no accelerometer fields are involved

- [ ] **Step 5: Commit**

```bash
git add gps_eta_simulator.py
git commit -m "Add GPS-only ETA simulator"
```

## Self-Review

- Spec coverage:
  - Separate simulator file: covered by Task 2.
  - Multiple vehicles in parallel: covered by Task 2.
  - One CSV per vehicle: covered by Task 1 and Task 2.
  - CSV format `lat,lon,speed_kmh`: covered by Task 1.
  - 15-second send cadence: covered by Task 2.
  - GPS-only payload: covered by Task 2.
- Placeholder scan:
  - No `TODO` or unspecified code blocks remain.
- Type consistency:
  - CSV columns are consistently `lat`, `lon`, `speed_kmh`.
  - Payload fields are consistently `VehicleID`, `Latitude`, `Longitude`, `Speed`.
