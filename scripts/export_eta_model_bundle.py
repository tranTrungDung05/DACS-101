import ast
import json
import math
from pathlib import Path

import numpy as np
import pandas as pd
from xgboost import XGBRegressor


DATA_PATH = Path("train.csv")
ARTIFACT_DIR = Path("artifacts/eta_models")

SAMPLE_ROWS = 100_000
MIN_PREFIX_POINTS = 3
PREFIX_STEP = 5
LONG_TRIP_THRESHOLD_SECONDS = 3_600
VERY_LONG_THRESHOLD_SECONDS = 7_200

TOP_6_FEATURES = [
    "observed_points",
    "distance_to_destination",
    "distance_from_start",
    "progress_ratio",
    "recent_speed_avg_kmh",
    "avg_speed_since_start_kmh",
]

XGB_PARAMS = {
    "n_estimators": 500,
    "max_depth": 6,
    "learning_rate": 0.05,
    "subsample": 0.8,
    "colsample_bytree": 0.8,
    "objective": "reg:squarederror",
    "random_state": 42,
    "n_jobs": -1,
}


def parse_polyline(polyline_text: str):
    points = ast.literal_eval(polyline_text)
    return points if isinstance(points, list) else []


def haversine_distance_km(lon1, lat1, lon2, lat2):
    lon1, lat1, lon2, lat2 = map(math.radians, [lon1, lat1, lon2, lat2])
    dlon = lon2 - lon1
    dlat = lat2 - lat1
    a = math.sin(dlat / 2) ** 2 + math.cos(lat1) * math.cos(lat2) * math.sin(dlon / 2) ** 2
    c = 2 * math.asin(math.sqrt(a))
    earth_radius_km = 6371.0
    return earth_radius_km * c


def build_prefix_samples(points, trip_id, min_prefix_points=3, step=5):
    rows = []
    total_points = len(points)
    start_lon, start_lat = points[0]
    dest_lon, dest_lat = points[-1]

    for prefix_points in range(min_prefix_points, total_points, step):
        observed = points[:prefix_points]
        remaining_segments = total_points - prefix_points
        elapsed_seconds = (prefix_points - 1) * 15
        current_lon, current_lat = observed[-1]

        distance_to_destination = haversine_distance_km(current_lon, current_lat, dest_lon, dest_lat)
        distance_from_start = haversine_distance_km(start_lon, start_lat, current_lon, current_lat)
        total_trip_distance_proxy = distance_from_start + distance_to_destination
        progress_ratio = distance_from_start / total_trip_distance_proxy if total_trip_distance_proxy > 0 else 0.0
        avg_speed_since_start_kmh = distance_from_start / (elapsed_seconds / 3600)

        segment_speeds = []
        recent_points = observed[-4:]
        for i in range(1, len(recent_points)):
            lon_a, lat_a = recent_points[i - 1]
            lon_b, lat_b = recent_points[i]
            segment_distance_km = haversine_distance_km(lon_a, lat_a, lon_b, lat_b)
            segment_speed_kmh = segment_distance_km / (15 / 3600)
            segment_speeds.append(segment_speed_kmh)

        recent_speed_avg_kmh = sum(segment_speeds) / len(segment_speeds)

        rows.append(
            {
                "TRIP_ID": trip_id,
                "observed_points": prefix_points,
                "remaining_eta_seconds": remaining_segments * 15,
                "distance_to_destination": distance_to_destination,
                "distance_from_start": distance_from_start,
                "progress_ratio": progress_ratio,
                "avg_speed_since_start_kmh": avg_speed_since_start_kmh,
                "recent_speed_avg_kmh": recent_speed_avg_kmh,
            }
        )

    return rows


def load_clean_trips():
    print(f"Loading {SAMPLE_ROWS:,} rows from {DATA_PATH} ...")
    df = pd.read_csv(DATA_PATH, nrows=SAMPLE_ROWS)

    sample = df.loc[:, ["TRIP_ID", "TAXI_ID", "TIMESTAMP", "MISSING_DATA", "POLYLINE"]].copy()
    sample["parsed_polyline"] = sample["POLYLINE"].apply(parse_polyline)
    sample["num_points"] = sample["parsed_polyline"].apply(len)
    sample["trip_duration_seconds"] = (sample["num_points"] - 1).clip(lower=0) * 15

    clean_df = sample.copy()
    clean_df["missing_flag"] = clean_df["MISSING_DATA"].astype(str).str.lower() == "true"
    clean_df = clean_df[(~clean_df["missing_flag"]) & (clean_df["num_points"] > 1)].copy()

    print(f"Clean trips kept: {len(clean_df):,}")
    return clean_df


def build_training_frame(clean_df: pd.DataFrame):
    prefix_rows = []

    for trip in clean_df.itertuples(index=False):
        prefix_rows.extend(
            build_prefix_samples(
                points=trip.parsed_polyline,
                trip_id=trip.TRIP_ID,
                min_prefix_points=MIN_PREFIX_POINTS,
                step=PREFIX_STEP,
            )
        )

    baseline_df = pd.DataFrame(prefix_rows)
    print(f"Prefix samples created: {len(baseline_df):,}")
    return baseline_df


def fit_log_target_model(train_df: pd.DataFrame) -> XGBRegressor:
    X = train_df[TOP_6_FEATURES]
    y = np.log1p(train_df["remaining_eta_seconds"])

    model = XGBRegressor(**XGB_PARAMS)
    model.fit(X, y)
    return model


def export_bundle():
    clean_df = load_clean_trips()
    baseline_df = build_training_frame(clean_df)

    long_df = baseline_df[baseline_df["remaining_eta_seconds"] >= LONG_TRIP_THRESHOLD_SECONDS].copy()
    very_long_df = baseline_df[baseline_df["remaining_eta_seconds"] >= VERY_LONG_THRESHOLD_SECONDS].copy()

    print("Training global_log_top6 ...")
    global_model = fit_log_target_model(baseline_df)
    print("Training specialized_long_log_top6 ...")
    long_model = fit_log_target_model(long_df)
    print("Training specialized_very_long_log_top6 ...")
    very_long_model = fit_log_target_model(very_long_df)

    ARTIFACT_DIR.mkdir(parents=True, exist_ok=True)

    global_path = ARTIFACT_DIR / "global_log_top6.json"
    long_path = ARTIFACT_DIR / "specialized_long_log_top6.json"
    very_long_path = ARTIFACT_DIR / "specialized_very_long_log_top6.json"

    global_model.save_model(global_path)
    long_model.save_model(long_path)
    very_long_model.save_model(very_long_path)

    manifest = {
        "dataset": "Porto Taxi",
        "data_path": str(DATA_PATH),
        "sample_rows": SAMPLE_ROWS,
        "min_prefix_points": MIN_PREFIX_POINTS,
        "prefix_step": PREFIX_STEP,
        "feature_set_name": "top_6_features",
        "feature_columns": TOP_6_FEATURES,
        "target_transform": "log1p(remaining_eta_seconds)",
        "thresholds": {
            "long_trip_seconds": LONG_TRIP_THRESHOLD_SECONDS,
            "very_long_trip_seconds": VERY_LONG_THRESHOLD_SECONDS,
        },
        "models": {
            "global_log_top6": {
                "path": str(global_path),
                "train_subset": "all_prefix_samples",
            },
            "specialized_long_log_top6": {
                "path": str(long_path),
                "train_subset": "remaining_eta_seconds >= 3600",
            },
            "specialized_very_long_log_top6": {
                "path": str(very_long_path),
                "train_subset": "remaining_eta_seconds >= 7200",
            },
        },
        "xgboost_params": XGB_PARAMS,
    }

    manifest_path = ARTIFACT_DIR / "manifest.json"
    manifest_path.write_text(json.dumps(manifest, indent=2))

    print(f"Saved artifact bundle to {ARTIFACT_DIR.resolve()}")
    print(f"- {global_path.name}")
    print(f"- {long_path.name}")
    print(f"- {very_long_path.name}")
    print(f"- {manifest_path.name}")


if __name__ == "__main__":
    export_bundle()
