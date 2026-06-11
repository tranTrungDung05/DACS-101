import csv
import json
from pathlib import Path


def load_gps_points(csv_path):
    with open(csv_path, newline="", encoding="utf-8") as handle:
        reader = csv.DictReader(handle)
        return [
            {
                "lat": float(row["lat"]),
                "lon": float(row["lon"]),
                "speed": float(row.get("speed", 0.0)),
            }
            for row in reader
        ]


def build_payload(vehicle_id, point):
    return {
        "VehicleID": vehicle_id,
        "Latitude": point["lat"],
        "Longitude": point["lon"],
        "Speed": point["speed"],
    }


def resolve_request_timeout_seconds(interval_seconds, requested_timeout_seconds):
    return min(interval_seconds, requested_timeout_seconds)


def resolve_eta_destination(vehicle_id, appsettings_path):
    config = json.loads(Path(appsettings_path).read_text(encoding="utf-8"))
    overrides = config.get("EtaDestinations", {})
    vehicle_override = overrides.get(str(vehicle_id))
    if vehicle_override:
        return (vehicle_override["Lat"], vehicle_override["Lon"])

    return (config["EtaDestinationLat"], config["EtaDestinationLon"])


def format_eta_status(eta_result, arrived, vehicle_id=None):
    if eta_result is None:
        return "ETA: da toi noi" if arrived else "ETA: chua du du lieu"

    prefix = "ETA"
    if vehicle_id is not None:
        prefix = f"ETA vehicle {vehicle_id}"

    return f"{prefix}: {eta_result['eta_minutes']:.1f}p ({eta_result['eta_seconds']:.1f}s)"
