from pathlib import Path

import pandas as pd


DATASET_ROOT = Path("UAH-DRIVESET-v1")
OUTPUT_DIR = Path("output")
OUTPUT_DIR.mkdir(exist_ok=True)

TRIP_RELATIVE_PATH = Path("D1/20151111123124-25km-D1-NORMAL-MOTORWAY")
START_SECOND = 10
NUM_SECONDS = 50


def load_gps(path: Path) -> pd.DataFrame:
    gps = pd.read_csv(
        path,
        sep=r"\s+",
        header=None,
        names=[
            "timestamp_s",
            "speed_kmh",
            "lat",
            "lon",
            "altitude_m",
            "hdop",
            "vdop",
            "bearing_deg",
            "sat_accuracy",
            "gps_x",
            "gps_y",
            "gps_z",
        ],
    )
    gps["elapsed_s"] = gps["timestamp_s"] - gps["timestamp_s"].iloc[0]
    gps["stream_second"] = gps["elapsed_s"].round().astype(int)
    return gps


def load_imu(path: Path) -> pd.DataFrame:
    imu = pd.read_csv(
        path,
        sep=r"\s+",
        header=None,
        names=[
            "timestamp_s",
            "unused",
            "accel_long_g",
            "accel_long_smooth_g",
            "accel_long_diff_g",
            "accel_lat_g",
            "accel_lat_smooth_g",
            "accel_lat_diff_g",
            "accel_vertical_g",
            "pitch_rad",
            "roll_rad",
        ],
    )
    imu["elapsed_s"] = imu["timestamp_s"] - imu["timestamp_s"].iloc[0]
    imu["stream_second"] = imu["elapsed_s"].astype(int)
    return imu


def build_stream_slices(trip_dir: Path, start_second: int, num_seconds: int) -> tuple[pd.DataFrame, pd.DataFrame]:
    trip_id = trip_dir.name
    gps = load_gps(trip_dir / "RAW_GPS.txt")
    imu = load_imu(trip_dir / "RAW_ACCELEROMETERS.txt")

    gps = gps[(gps["stream_second"] >= start_second) & (gps["stream_second"] < start_second + num_seconds)].copy()
    imu = imu[(imu["stream_second"] >= start_second) & (imu["stream_second"] < start_second + num_seconds)].copy()

    gps.insert(0, "trip_id", trip_id)
    imu.insert(0, "trip_id", trip_id)

    gps["timestamp_s"] = gps["elapsed_s"]
    imu["timestamp_s"] = imu["elapsed_s"]

    gps_columns = [
        "trip_id",
        "timestamp_s",
        "lat",
        "lon",
        "speed_kmh",
    ]
    imu_columns = [
        "trip_id",
        "timestamp_s",
        "accel_long_g",
        "accel_lat_g",
    ]
    return gps[gps_columns].reset_index(drop=True), imu[imu_columns].reset_index(drop=True)


def main() -> None:
    trip_dir = DATASET_ROOT / TRIP_RELATIVE_PATH
    if not trip_dir.exists():
        raise FileNotFoundError(f"Missing trip folder: {trip_dir}")

    gps_stream, accel_stream = build_stream_slices(trip_dir, START_SECOND, NUM_SECONDS)

    gps_path = OUTPUT_DIR / "uah_trip_gps_stream.csv"
    accel_path = OUTPUT_DIR / "uah_trip_accel_stream.csv"

    gps_stream.to_csv(gps_path, index=False)
    accel_stream.to_csv(accel_path, index=False)

    print(f"Saved: {gps_path}")
    print(gps_stream.head(10).to_string(index=False))
    print()
    print(f"Saved: {accel_path}")
    print(accel_stream.head(10).to_string(index=False))


if __name__ == "__main__":
    main()
