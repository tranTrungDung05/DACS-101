import argparse
import ast
import csv
import math
from pathlib import Path


SECONDS_PER_POINT = 15


def haversine_distance_km(lon1: float, lat1: float, lon2: float, lat2: float) -> float:
    lon1, lat1, lon2, lat2 = map(math.radians, [lon1, lat1, lon2, lat2])
    dlon = lon2 - lon1
    dlat = lat2 - lat1
    a = math.sin(dlat / 2) ** 2 + math.cos(lat1) * math.cos(lat2) * math.sin(dlon / 2) ** 2
    c = 2 * math.asin(math.sqrt(a))
    earth_radius_km = 6371.0
    return earth_radius_km * c


def parse_polyline(polyline_text: str) -> list[list[float]]:
    points = ast.literal_eval(polyline_text)
    if not isinstance(points, list):
        return []
    return points


def is_valid_trip(row: dict[str, str]) -> bool:
    if row.get("MISSING_DATA", "").lower() == "true":
        return False

    points = parse_polyline(row["POLYLINE"])
    return len(points) >= 2


def iter_lines_reverse(data_path: Path, chunk_size: int = 1024 * 1024):
    with data_path.open("rb") as file_obj:
        file_obj.seek(0, 2)
        position = file_obj.tell()
        buffer = b""

        while position > 0:
            read_size = min(chunk_size, position)
            position -= read_size
            file_obj.seek(position)
            buffer = file_obj.read(read_size) + buffer

            lines = buffer.splitlines()
            if position > 0:
                buffer = lines[0]
                lines = lines[1:]
            else:
                buffer = b""

            for line in reversed(lines):
                if line:
                    yield line.decode("utf-8")


def find_last_valid_trip(data_path: Path) -> tuple[str, list[list[float]]]:
    with data_path.open(newline="") as csv_file:
        fieldnames = next(csv.reader(csv_file))

    for line in iter_lines_reverse(data_path):
        if line.startswith("TRIP_ID,"):
            continue

        row = next(csv.DictReader([line], fieldnames=fieldnames))
        if not is_valid_trip(row):
            continue

        return row["TRIP_ID"], parse_polyline(row["POLYLINE"])

    raise ValueError(f"No valid trip with at least 2 GPS points found in {data_path}.")


def build_gps_rows(points: list[list[float]]) -> list[dict[str, float]]:
    rows: list[dict[str, float]] = []

    for index, point in enumerate(points):
        lon, lat = point

        if index == 0:
            speed_kmh = 0.0
        else:
            prev_lon, prev_lat = points[index - 1]
            distance_km = haversine_distance_km(prev_lon, prev_lat, lon, lat)
            speed_kmh = distance_km / (SECONDS_PER_POINT / 3600)

        rows.append(
            {
                "lat": lat,
                "lon": lon,
                "speed": speed_kmh,
            }
        )

    return rows


def export_trip_gps_csv(data_path: Path, output_path: Path) -> str:
    trip_id, points = find_last_valid_trip(data_path)
    rows = build_gps_rows(points)

    output_path.parent.mkdir(parents=True, exist_ok=True)
    with output_path.open("w", newline="") as csv_file:
        writer = csv.DictWriter(csv_file, fieldnames=["lat", "lon", "speed"])
        writer.writeheader()
        writer.writerows(rows)

    return trip_id


def build_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Export lat/lon/speed CSV for the last valid trip in train.csv."
    )
    parser.add_argument(
        "--data-path",
        type=Path,
        default=Path("train.csv"),
        help="Path to the Porto train.csv file.",
    )
    parser.add_argument(
        "--output",
        type=Path,
        default=None,
        help="Output CSV path. Default: sample_trips/<trip_id>_gps.csv",
    )
    return parser.parse_args()


def main() -> None:
    args = build_args()
    trip_id, _ = find_last_valid_trip(args.data_path)
    output_path = args.output or Path("sample_trips") / f"{trip_id}_gps.csv"
    export_trip_gps_csv(args.data_path, output_path)
    print(f"Exported trip {trip_id} to {output_path}")


if __name__ == "__main__":
    main()
