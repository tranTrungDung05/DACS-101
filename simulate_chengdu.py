import json
import time
import requests
import os

API_URL = "http://localhost:5025/api/Gps/Update"
VEHICLE_ID = 13
INTERVAL_SECONDS = 1.0  # Time to wait between points (1s for fast demo)

def main():
    current_dir = os.path.dirname(os.path.abspath(__file__))
    json_path = os.path.join(current_dir, "eta_serving", "sample_trip.json")
    
    if not os.path.exists(json_path):
        print(f"[ERROR] Cannot find sample_trip.json at {json_path}")
        return

    with open(json_path, "r") as f:
        trip_data = json.load(f)

    longitudes = trip_data["current_longi"]
    latitudes = trip_data["current_lati"]
    total_points = len(longitudes)

    print("====================================================")
    print(f"🚀 STARTING CHENGDU REAL-TIME GPS SIMULATOR")
    print(f"Target Vehicle ID: {VEHICLE_ID}")
    print(f"Total Points to stream: {total_points}")
    print(f"Interval: {INTERVAL_SECONDS} seconds per update")
    print("====================================================")

    last_eta_str = "ETA: Đang tính..."
    for i in range(total_points):
        lat = latitudes[i]
        lon = longitudes[i]
        
        # Construct the payload matching GpsController.Update expectation
        payload = {
            "VehicleID": VEHICLE_ID,
            "Latitude": lat,
            "Longitude": lon,
            "Speed": 35.0  # Simulated average speed in km/h
        }

        try:
            started_at = time.perf_counter()
            response = requests.post(API_URL, json=payload, timeout=5)
            elapsed = time.perf_counter() - started_at
            
            if response.ok:
                res_data = response.json()
                eta_val = res_data.get("eta")
                if eta_val:
                    last_eta_str = eta_val
                print(f"[{i+1}/{total_points}] OK | sent: ({lat:.6f}, {lon:.6f}) | {last_eta_str} | elapsed: {elapsed:.2f}s")
            else:
                print(f"[{i+1}/{total_points}] FAILED {response.status_code} | response: {response.text[:150]}")
                
        except Exception as e:
            print(f"[{i+1}/{total_points}] REQUEST ERROR: {e}")

        time.sleep(INTERVAL_SECONDS)

    print("\n🏁 Chengdu simulation completed!")

if __name__ == "__main__":
    main()
