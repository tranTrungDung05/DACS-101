import argparse
from pathlib import Path
import sys

import pandas as pd

PROJECT_ROOT = Path(__file__).resolve().parents[1]
if str(PROJECT_ROOT) not in sys.path:
    sys.path.insert(0, str(PROJECT_ROOT))

from eta_inference import predict_eta_from_points


def main():
    parser = argparse.ArgumentParser(description="Test ETA prediction on a sample GPS trip CSV.")
    parser.add_argument("csv_path", type=Path, help="Path to sample trip CSV with lat/lon columns.")
    parser.add_argument(
        "--prefix-points",
        type=int,
        default=4,
        help="Number of observed GPS points to use as the current trip prefix.",
    )
    args = parser.parse_args()

    df = pd.read_csv(args.csv_path)
    required_columns = {"lat", "lon"}
    if not required_columns.issubset(df.columns):
        raise ValueError(f"CSV must contain columns: {sorted(required_columns)}")

    if len(df) < 4:
        raise ValueError("CSV needs at least 4 points to run a meaningful ETA test.")

    if args.prefix_points < 3:
        raise ValueError("--prefix-points must be at least 3.")

    if args.prefix_points >= len(df):
        raise ValueError("--prefix-points must be smaller than the total number of GPS points.")

    all_points = [(row.lon, row.lat) for row in df.itertuples(index=False)]
    prefix_points = all_points[: args.prefix_points]
    destination = all_points[-1]

    actual_remaining_points = len(all_points) - args.prefix_points
    actual_remaining_seconds = actual_remaining_points * 15

    result = predict_eta_from_points(prefix_points, destination)

    print(f"CSV: {args.csv_path}")
    print(f"Total points: {len(all_points)}")
    print(f"Observed prefix points: {args.prefix_points}")
    print(f"Actual remaining ETA: {actual_remaining_seconds:.2f} seconds ({actual_remaining_seconds / 60:.2f} minutes)")
    print(f"Predicted ETA: {result.eta_seconds:.2f} seconds ({result.eta_minutes:.2f} minutes)")
    print(f"Selected model: {result.selected_model}")
    print("Route stage predictions:")
    for name, value in result.route_stage_predictions.items():
        print(f"  - {name}: {value:.2f} seconds ({value / 60:.2f} minutes)")
    print("Features:")
    for name, value in result.features.items():
        print(f"  - {name}: {value:.6f}")


if __name__ == "__main__":
    main()
