import argparse
import csv
import json
import time
from concurrent.futures import Future, ThreadPoolExecutor
from pathlib import Path

import requests


DEFAULT_API_URL = "http://localhost:5025/api/Gps/Update"
DEFAULT_ETA_API_URL = "http://127.0.0.1:8001/predict"
DEFAULT_APPSETTINGS_PATH = Path("appsettings.json")
DEFAULT_CSV_PATH = Path("1404157147620000079_gps.csv")
DEFAULT_INTERVAL_SECONDS = 15
DEFAULT_VEHICLE_ID = 1
DEFAULT_REQUEST_TIMEOUT_SECONDS = 15
ETA_ARRIVAL_THRESHOLD_KM = 0.03


def load_gps_points(csv_path: str | Path) -> list[dict[str, float]]:
    path = Path(csv_path)
    if not path.exists():
        raise FileNotFoundError(f"Missing GPS CSV: {path}")

    gps_points: list[dict[str, float]] = []
    with path.open("r", encoding="utf-8", newline="") as handle:
        reader = csv.DictReader(handle)
        for index, row in enumerate(reader, start=2):
            try:
                gps_points.append(
                    {
                        "lat": float(row["lat"]),
                        "lon": float(row["lon"]),
                        "speed": float(row.get("speed", row.get("speed_kmh", 0.0))),
                    }
                )
            except (KeyError, TypeError, ValueError) as exc:
                print(f"[WARN] Skip row {index} in {path}: {exc}")

    return gps_points


def build_payload(vehicle_id: int, point: dict[str, float]) -> dict[str, float | int]:
    return {
        "VehicleID": vehicle_id,
        "Latitude": point["lat"],
        "Longitude": point["lon"],
        "Speed": point["speed"],
    }


def resolve_request_timeout_seconds(interval_seconds: int, requested_timeout_seconds: int) -> int:
    return max(1, min(interval_seconds, requested_timeout_seconds))


def resolve_eta_destination(vehicle_id: int, appsettings_path: str | Path = DEFAULT_APPSETTINGS_PATH) -> tuple[float, float]:
    config = json.loads(Path(appsettings_path).read_text(encoding="utf-8"))
    vehicle_destinations = config.get("EtaDestinations", {})
    vehicle_section = vehicle_destinations.get(str(vehicle_id), {})

    lat = vehicle_section.get("Lat", config["EtaDestinationLat"])
    lon = vehicle_section.get("Lon", config["EtaDestinationLon"])
    return float(lat), float(lon)


def calculate_distance_km(lat1: float, lon1: float, lat2: float, lon2: float) -> float:
    from math import atan2, cos, radians, sin, sqrt

    earth_radius_km = 6371.0
    dlat = radians(lat2 - lat1)
    dlon = radians(lon2 - lon1)
    a = sin(dlat / 2) ** 2 + cos(radians(lat1)) * cos(radians(lat2)) * sin(dlon / 2) ** 2
    c = 2 * atan2(sqrt(a), sqrt(1 - a))
    return earth_radius_km * c


def request_eta_prediction(
    eta_api_url: str,
    observed_points: list[dict[str, float]],
    destination: tuple[float, float],
    request_timeout_seconds: int,
) -> dict[str, float] | None:
    if len(observed_points) < 3:
        return None

    payload = {
        "gps_points": [{"lat": point["lat"], "lon": point["lon"]} for point in observed_points],
        "destination": {"lat": destination[0], "lon": destination[1]},
    }
    response = requests.post(eta_api_url, json=payload, timeout=request_timeout_seconds)
    response.raise_for_status()
    return response.json()


def format_eta_status(
    eta_result: dict[str, float] | None,
    has_arrived: bool,
    vehicle_id: int | None = None,
) -> str:
    if has_arrived:
        return "ETA: da toi noi"
    if eta_result is None:
        return "ETA: chua du du lieu"

    prefix = f"ETA vehicle {vehicle_id}: " if vehicle_id is not None else "ETA: "
    return f"{prefix}{eta_result['eta_minutes']:.1f}p ({eta_result['eta_seconds']:.1f}s)"


def send_gps_point(
    api_url: str,
    eta_api_url: str,
    payload: dict[str, float | int],
    point: dict[str, float],
    observed_points: list[dict[str, float]],
    destination: tuple[float, float],
    vehicle_id: int,
    index: int,
    total_points: int,
    request_timeout_seconds: int,
) -> None:
    started_at = time.perf_counter()

    try:
        response = requests.post(api_url, json=payload, timeout=request_timeout_seconds)
        status = "OK" if response.ok else f"ERR {response.status_code}"
        elapsed_seconds = time.perf_counter() - started_at
        print(
            f"[{index}/{total_points}] {status} "
            f"lat={point['lat']:.6f} lon={point['lon']:.6f} speed={point['speed']:.1f} "
            f"elapsed={elapsed_seconds:.2f}s"
        )
        if not response.ok:
            print(f"[WARN] Response: {response.text[:200]}")
            return

        has_arrived = calculate_distance_km(
            point["lat"],
            point["lon"],
            destination[0],
            destination[1],
        ) <= ETA_ARRIVAL_THRESHOLD_KM

        try:
            eta_result = request_eta_prediction(
                eta_api_url=eta_api_url,
                observed_points=observed_points,
                destination=destination,
                request_timeout_seconds=request_timeout_seconds,
            )
            print(format_eta_status(eta_result, has_arrived, vehicle_id=vehicle_id))
        except requests.RequestException as exc:
            print(f"[WARN] ETA request failed at point {index}: {exc}")
    except requests.RequestException as exc:
        elapsed_seconds = time.perf_counter() - started_at
        print(f"[ERROR] Request failed at point {index} after {elapsed_seconds:.2f}s: {exc}")


def stream_gps_points(
    vehicle_id: int,
    csv_path: str | Path,
    api_url: str = DEFAULT_API_URL,
    eta_api_url: str = DEFAULT_ETA_API_URL,
    appsettings_path: str | Path = DEFAULT_APPSETTINGS_PATH,
    interval_seconds: int = DEFAULT_INTERVAL_SECONDS,
    request_timeout_seconds: int = DEFAULT_REQUEST_TIMEOUT_SECONDS,
) -> None:
    points = load_gps_points(csv_path)
    if not points:
        print(f"[WARN] No valid GPS points found in {csv_path}")
        return

    destination = resolve_eta_destination(vehicle_id, appsettings_path)

    print(
        f"[INFO] Streaming {len(points)} GPS points from {csv_path} "
        f"to vehicle {vehicle_id} every {interval_seconds}s"
    )
    print(f"[INFO] ETA destination for vehicle {vehicle_id}: lat={destination[0]:.6f} lon={destination[1]:.6f}")

    effective_timeout_seconds = resolve_request_timeout_seconds(interval_seconds, request_timeout_seconds)
    started_at = time.perf_counter()
    futures: list[Future[None]] = []

    with ThreadPoolExecutor(max_workers=4) as executor:
        for index, point in enumerate(points, start=1):
            scheduled_at = started_at + ((index - 1) * interval_seconds)
            delay_seconds = scheduled_at - time.perf_counter()
            if delay_seconds > 0:
                time.sleep(delay_seconds)

            payload = build_payload(vehicle_id, point)
            observed_points = points[:index]
            futures.append(
                executor.submit(
                    send_gps_point,
                    api_url,
                    eta_api_url,
                    payload,
                    point,
                    observed_points,
                    destination,
                    vehicle_id,
                    index,
                    len(points),
                    effective_timeout_seconds,
                )
            )

        for future in futures:
            future.result()


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Replay GPS points from a CSV into GpsController to trigger realtime ETA updates."
    )
    parser.add_argument("--vehicle-id", type=int, default=DEFAULT_VEHICLE_ID, help="Vehicle ID in the DACS database.")
    parser.add_argument("--csv", type=Path, default=DEFAULT_CSV_PATH, help="Path to GPS CSV file.")
    parser.add_argument("--api-url", default=DEFAULT_API_URL, help="GpsController update endpoint.")
    parser.add_argument("--eta-api-url", default=DEFAULT_ETA_API_URL, help="ETA prediction endpoint.")
    parser.add_argument("--appsettings-path", type=Path, default=DEFAULT_APPSETTINGS_PATH, help="Path to appsettings.json for ETA destination lookup.")
    parser.add_argument(
        "--interval-seconds",
        type=int,
        default=DEFAULT_INTERVAL_SECONDS,
        help="Delay between GPS points.",
    )
    parser.add_argument(
        "--request-timeout-seconds",
        type=int,
        default=DEFAULT_REQUEST_TIMEOUT_SECONDS,
        help="HTTP timeout per GPS update request. Effective timeout is capped by interval-seconds.",
    )
    return parser.parse_args()


def main() -> None:
    args = parse_args()
    stream_gps_points(
        vehicle_id=args.vehicle_id,
        csv_path=args.csv,
        api_url=args.api_url,
        eta_api_url=args.eta_api_url,
        appsettings_path=args.appsettings_path,
        interval_seconds=args.interval_seconds,
        request_timeout_seconds=args.request_timeout_seconds,
    )


if __name__ == "__main__":
    main()
