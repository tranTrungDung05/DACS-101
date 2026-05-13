import math
from dataclasses import dataclass
from typing import Sequence

import numpy as np
import pandas as pd

from eta_model_loader import ETAModelBundle, load_eta_model_bundle


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
    return 6371.0 * c


def build_eta_features(points: Sequence[GPSPoint], destination: GPSPoint) -> dict[str, float]:
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

    recent_points = list(points[-4:])
    segment_speeds = []
    for index in range(1, len(recent_points)):
        lon_a, lat_a = recent_points[index - 1]
        lon_b, lat_b = recent_points[index]
        segment_distance_km = haversine_distance_km(lon_a, lat_a, lon_b, lat_b)
        segment_speeds.append(segment_distance_km / (15 / 3600))

    recent_speed_avg_kmh = sum(segment_speeds) / len(segment_speeds)

    return {
        "observed_points": float(observed_points),
        "distance_to_destination": float(distance_to_destination),
        "distance_from_start": float(distance_from_start),
        "progress_ratio": float(progress_ratio),
        "recent_speed_avg_kmh": float(recent_speed_avg_kmh),
        "avg_speed_since_start_kmh": float(avg_speed_since_start_kmh),
    }


def _predict_seconds(model, feature_frame: pd.DataFrame) -> float:
    prediction = np.expm1(model.predict(feature_frame))[0]
    return float(max(prediction, 0.0))


def predict_eta_from_features(
    features: dict[str, float],
    bundle: ETAModelBundle | None = None,
) -> ETAPrediction:
    bundle = bundle or load_eta_model_bundle()
    feature_frame = pd.DataFrame(
        [[features[column] for column in bundle.feature_columns]],
        columns=bundle.feature_columns,
    )

    global_prediction = _predict_seconds(bundle.global_model, feature_frame)
    stage_predictions = {"global_log_top6": global_prediction}

    if global_prediction < bundle.long_trip_seconds:
        selected_model = "global_log_top6"
        final_prediction = global_prediction
    else:
        long_prediction = _predict_seconds(bundle.long_model, feature_frame)
        stage_predictions["specialized_long_log_top6"] = long_prediction

        if long_prediction < bundle.very_long_trip_seconds:
            selected_model = "specialized_long_log_top6"
            final_prediction = long_prediction
        else:
            very_long_prediction = _predict_seconds(bundle.very_long_model, feature_frame)
            stage_predictions["specialized_very_long_log_top6"] = very_long_prediction
            selected_model = "specialized_very_long_log_top6"
            final_prediction = very_long_prediction

    return ETAPrediction(
        eta_seconds=final_prediction,
        eta_minutes=final_prediction / 60.0,
        selected_model=selected_model,
        route_stage_predictions=stage_predictions,
        features=features,
    )


def predict_eta_from_points(
    points: Sequence[GPSPoint],
    destination: GPSPoint,
    bundle: ETAModelBundle | None = None,
) -> ETAPrediction:
    features = build_eta_features(points, destination)
    return predict_eta_from_features(features, bundle=bundle)
