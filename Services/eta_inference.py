import argparse
import json
import math
from dataclasses import dataclass
from datetime import datetime, timezone
from pathlib import Path
from typing import Sequence

import numpy as np
import pandas as pd

from Services.eta_model_loader import ETAModelBundle, load_eta_model_bundle


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
    earth_radius_km = 6371.0
    return earth_radius_km * c


def bearing_degrees(lon1: float, lat1: float, lon2: float, lat2: float) -> float:
    lon1_rad, lat1_rad, lon2_rad, lat2_rad = map(math.radians, [lon1, lat1, lon2, lat2])
    dlon = lon2_rad - lon1_rad
    x = math.sin(dlon) * math.cos(lat2_rad)
    y = (
        math.cos(lat1_rad) * math.sin(lat2_rad)
        - math.sin(lat1_rad) * math.cos(lat2_rad) * math.cos(dlon)
    )
    bearing = math.degrees(math.atan2(x, y))
    return (bearing + 360.0) % 360.0


def angular_difference_degrees(angle_a: float, angle_b: float) -> float:
    return abs((angle_a - angle_b + 180.0) % 360.0 - 180.0)


def _derive_calendar_features(trip_start_timestamp: int | None) -> tuple[int, int, bool]:
    if trip_start_timestamp is None:
        trip_start_dt = datetime.now(timezone.utc)
    else:
        trip_start_dt = datetime.fromtimestamp(trip_start_timestamp, tz=timezone.utc)

    start_hour = trip_start_dt.hour
    day_of_week = trip_start_dt.weekday()
    is_weekend = day_of_week >= 5
    return start_hour, day_of_week, is_weekend


def build_eta_features(
    points: Sequence[GPSPoint],
    destination: GPSPoint,
    trip_start_timestamp: int | None = None,
) -> dict[str, float]:
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

    prev_lon, prev_lat = points[-2]
    recent_distance_km = haversine_distance_km(prev_lon, prev_lat, current_lon, current_lat)
    recent_speed_kmh = recent_distance_km / (15 / 3600)

    current_heading_deg = bearing_degrees(prev_lon, prev_lat, current_lon, current_lat)
    destination_bearing_deg = bearing_degrees(current_lon, current_lat, dest_lon, dest_lat)
    heading_error_deg = angular_difference_degrees(current_heading_deg, destination_bearing_deg)
    heading_alignment = math.cos(math.radians(heading_error_deg))

    segment_speeds = []
    recent_points = points[-4:]
    for i in range(1, len(recent_points)):
        lon_a, lat_a = recent_points[i - 1]
        lon_b, lat_b = recent_points[i]
        segment_distance_km = haversine_distance_km(lon_a, lat_a, lon_b, lat_b)
        segment_speed_kmh = segment_distance_km / (15 / 3600)
        segment_speeds.append(segment_speed_kmh)

    recent_speed_avg_kmh = sum(segment_speeds) / len(segment_speeds)
    recent_low_speed_count = sum(speed < 5.0 for speed in segment_speeds)

    start_hour, day_of_week, is_weekend = _derive_calendar_features(trip_start_timestamp)

    return {
        "observed_points": float(observed_points),
        "start_longitude": float(start_lon),
        "start_latitude": float(start_lat),
        "current_longitude": float(current_lon),
        "current_latitude": float(current_lat),
        "destination_longitude": float(dest_lon),
        "destination_latitude": float(dest_lat),
        "distance_to_destination": float(distance_to_destination),
        "distance_from_start": float(distance_from_start),
        "progress_ratio": float(progress_ratio),
        "avg_speed_since_start_kmh": float(avg_speed_since_start_kmh),
        "recent_speed_kmh": float(recent_speed_kmh),
        "recent_speed_avg_kmh": float(recent_speed_avg_kmh),
        "heading_alignment": float(heading_alignment),
        "recent_low_speed_count": float(recent_low_speed_count),
        "start_hour": float(start_hour),
        "day_of_week": float(day_of_week),
        "is_weekend": float(is_weekend),
    }


def _predict_seconds(model, feature_frame: pd.DataFrame) -> float:
    prediction = np.expm1(model.predict(feature_frame))[0]
    return float(max(prediction, 0.0))


def predict_eta_from_features(
    features: dict[str, float],
    bundle: ETAModelBundle | None = None,
) -> ETAPrediction:
    bundle = bundle or load_eta_model_bundle()
    feature_frame = pd.DataFrame([[features[column] for column in bundle.feature_columns]], columns=bundle.feature_columns)

    prediction = _predict_seconds(bundle.model, feature_frame)

    return ETAPrediction(
        eta_seconds=prediction,
        eta_minutes=prediction / 60.0,
        selected_model=bundle.model_name,
        route_stage_predictions={bundle.model_name: prediction},
        features=features,
    )


def predict_eta_from_points(
    points: Sequence[GPSPoint],
    destination: GPSPoint,
    trip_start_timestamp: int | None = None,
    bundle: ETAModelBundle | None = None,
) -> ETAPrediction:
    if len(points) < 3:
        raise ValueError("Need at least 3 GPS points to predict ETA.")

    features = build_eta_features(points, destination, trip_start_timestamp=trip_start_timestamp)
    return predict_eta_from_features(features, bundle=bundle)


def _load_points_from_csv(csv_path: Path) -> tuple[list[GPSPoint], GPSPoint]:
    df = pd.read_csv(csv_path)
    required_columns = {"lat", "lon"}
    if not required_columns.issubset(df.columns):
        raise ValueError(f"CSV must contain columns: {sorted(required_columns)}")

    if len(df) < 4:
        raise ValueError("CSV needs at least 4 points.")

    all_points = [(row.lon, row.lat) for row in df.itertuples(index=False)]
    return all_points[:-1], all_points[-1]


def _load_points_from_json(json_path: Path) -> tuple[list[GPSPoint], GPSPoint, int | None]:
    payload = json.loads(json_path.read_text(encoding="utf-8"))
    if "points" not in payload or "destination" not in payload:
        raise ValueError("JSON must contain `points` and `destination`.")

    points = [tuple(point) for point in payload["points"]]
    destination = tuple(payload["destination"])
    trip_start_timestamp = payload.get("trip_start_timestamp")
    return points, destination, trip_start_timestamp


def main():
    parser = argparse.ArgumentParser(description="Debug ETA inference inputs and predictions.")
    parser.add_argument("--csv", type=Path, help="CSV with lat/lon columns. Last row is treated as destination.")
    parser.add_argument("--json", type=Path, help="JSON with `points` and `destination` in (lon, lat) format.")
    parser.add_argument(
        "--trip-start-timestamp",
        type=int,
        default=None,
        help="Unix timestamp for trip start time. Used to derive start_hour/day_of_week/is_weekend.",
    )
    parser.add_argument(
        "--prefix-points",
        type=int,
        default=None,
        help="Optional number of observed points to keep from the start of the trip.",
    )
    args = parser.parse_args()

    if not args.csv and not args.json:
        raise ValueError("Provide either --csv or --json.")
    if args.csv and args.json:
        raise ValueError("Use only one of --csv or --json at a time.")

    if args.csv:
        points, destination = _load_points_from_csv(args.csv)
        trip_start_timestamp = args.trip_start_timestamp
        source_label = str(args.csv)
    else:
        points, destination, json_timestamp = _load_points_from_json(args.json)
        trip_start_timestamp = args.trip_start_timestamp if args.trip_start_timestamp is not None else json_timestamp
        source_label = str(args.json)

    if args.prefix_points is not None:
        if args.prefix_points < 3:
            raise ValueError("--prefix-points must be at least 3.")
        if args.prefix_points > len(points):
            raise ValueError("--prefix-points cannot exceed the available observed points.")
        points = points[: args.prefix_points]

    result = predict_eta_from_points(points, destination, trip_start_timestamp=trip_start_timestamp)

    print(f"Input source: {source_label}")
    print(f"Observed points: {len(points)}")
    print(f"Destination: {destination}")
    print(f"Selected model: {result.selected_model}")
    print(f"ETA: {result.eta_seconds:.2f} seconds ({result.eta_minutes:.2f} minutes)")
    print("Features:")
    for name, value in result.features.items():
        print(f"  - {name}: {value:.6f}")


if __name__ == "__main__":
    main()
