import json
from http import HTTPStatus
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path
from typing import Any
from urllib.parse import parse_qs, urlparse

import joblib
import pandas as pd


MODEL_PATH = Path("./Services/best_driver_behavior_model.joblib")
HOST = "127.0.0.1"
PORT = 8000

LABEL_NAMES = {
    0: "NORMAL",
    1: "AGGRESSIVE",
    2: "DROWSY",
}


class BehaviorPredictor:
    def __init__(self, model_path: Path) -> None:
        if not model_path.exists():
            raise FileNotFoundError(f"Missing model file: {model_path}")

        bundle = joblib.load(model_path)
        self.model_name = bundle["model_name"]
        self.feature_cols = bundle["feature_cols"]
        self.pipeline = bundle["pipeline"]

    @staticmethod
    def _require_columns(frame: pd.DataFrame, columns: list[str]) -> None:
        missing = [column for column in columns if column not in frame.columns]
        if missing:
            raise ValueError(f"Missing required columns: {missing}")

    @staticmethod
    def _compute_distance_km(frame: pd.DataFrame) -> float:
        ordered = frame.sort_values("timestamp_s").copy()
        dt_hours = ordered["timestamp_s"].diff().fillna(0) / 3600.0
        return float((ordered["speed_kmh"] * dt_hours).sum())

    @staticmethod
    def _normalize_gps_frame(frame: pd.DataFrame) -> pd.DataFrame:
        if frame.empty:
            raise ValueError("gps_points is empty")

        candidates = {
            "timestamp_s": ["timestamp_s", "stream_second"],
            "speed_kmh": ["speed_kmh"],
            "lat": ["lat"],
            "lon": ["lon"],
        }

        normalized = pd.DataFrame()
        for target, options in candidates.items():
            source = next((name for name in options if name in frame.columns), None)
            if source is None:
                if target in {"lat", "lon"}:
                    continue
                raise ValueError(f"Missing GPS column for {target}: tried {options}")
            normalized[target] = pd.to_numeric(frame[source], errors="coerce")

        return normalized.dropna(subset=["timestamp_s", "speed_kmh"])

    @staticmethod
    def _normalize_accel_frame(frame: pd.DataFrame) -> pd.DataFrame:
        if frame.empty:
            raise ValueError("accel_points is empty")

        candidates = {
            "timestamp_s": ["timestamp_s", "stream_second"],
            "accel_long_g": ["accel_long_g", "accel_long_mean_g"],
            "accel_lat_g": ["accel_lat_g", "accel_lat_mean_g"],
        }

        normalized = pd.DataFrame()
        for target, options in candidates.items():
            source = next((name for name in options if name in frame.columns), None)
            if source is None:
                raise ValueError(f"Missing accelerometer column for {target}: tried {options}")
            normalized[target] = pd.to_numeric(frame[source], errors="coerce")

        return normalized.dropna()

    def split_streams_to_features(
        self,
        gps_points: list[dict[str, Any]],
        accel_points: list[dict[str, Any]],
    ) -> dict[str, float]:
        gps_frame = pd.DataFrame(gps_points)
        accel_frame = pd.DataFrame(accel_points)

        gps = self._normalize_gps_frame(gps_frame)
        accel = self._normalize_accel_frame(accel_frame)

        if len(gps) < 2:
            raise ValueError("Need at least 2 valid GPS points to compute features")
        if len(accel) < 2:
            raise ValueError("Need at least 2 valid accelerometer points to compute features")

        features = {
            "speed_mean": float(gps["speed_kmh"].mean()),
            "speed_std": float(gps["speed_kmh"].std(ddof=1)),
            "accel_long_std": float(accel["accel_long_g"].std(ddof=1)),
            "accel_lat_std": float(accel["accel_lat_g"].std(ddof=1)),
            "distance_km": self._compute_distance_km(gps),
        }
        return features

    def stream_to_features(self, points: list[dict[str, Any]]) -> dict[str, float]:
        frame = pd.DataFrame(points)
        self._require_columns(frame, ["speed_kmh"])

        accel_long_col = "accel_long_mean_g" if "accel_long_mean_g" in frame.columns else "accel_long_g"
        accel_lat_col = "accel_lat_mean_g" if "accel_lat_mean_g" in frame.columns else "accel_lat_g"
        self._require_columns(frame, [accel_long_col, accel_lat_col])

        clean = pd.DataFrame(
            {
                "timestamp_s": pd.to_numeric(frame["timestamp_s"], errors="coerce"),
                "speed_kmh": pd.to_numeric(frame["speed_kmh"], errors="coerce"),
                "accel_long_g": pd.to_numeric(frame[accel_long_col], errors="coerce"),
                "accel_lat_g": pd.to_numeric(frame[accel_lat_col], errors="coerce"),
            }
        ).dropna()

        if len(clean) < 2:
            raise ValueError("Need at least 2 valid points to compute inference features")

        features = {
            "speed_mean": float(clean["speed_kmh"].mean()),
            "speed_std": float(clean["speed_kmh"].std(ddof=1)),
            "accel_long_std": float(clean["accel_long_g"].std(ddof=1)),
            "accel_lat_std": float(clean["accel_lat_g"].std(ddof=1)),
            "distance_km": self._compute_distance_km(clean),
        }
        return features

    def _predict_from_features(self, features: dict[str, float], num_points: int, source: str) -> dict[str, Any]:
        feature_frame = pd.DataFrame([features])[self.feature_cols]
        prediction = int(self.pipeline.predict(feature_frame)[0])

        probabilities = None
        if hasattr(self.pipeline, "predict_proba"):
            probs = self.pipeline.predict_proba(feature_frame)[0]
            probabilities = {
                LABEL_NAMES.get(int(label), str(label)): float(prob)
                for label, prob in zip(self.pipeline.classes_, probs)
            }

        return {
            "model_name": self.model_name,
            "prediction": prediction,
            "prediction_name": LABEL_NAMES.get(prediction, str(prediction)),
            "features": features,
            "probabilities": probabilities,
            "num_points": num_points,
            "source": source,
        }

    def predict_from_points(self, points: list[dict[str, Any]]) -> dict[str, Any]:
        features = self.stream_to_features(points)
        return self._predict_from_features(features, len(points), "combined_stream")

    def predict_from_split_points(
        self,
        gps_points: list[dict[str, Any]],
        accel_points: list[dict[str, Any]],
    ) -> dict[str, Any]:
        features = self.split_streams_to_features(gps_points, accel_points)
        print("DEBUG INCOMING FEATURES:", features, flush=True)
        return self._predict_from_features(features, len(gps_points) + len(accel_points), "split_streams")

    def predict_from_csv(self, csv_path: str) -> dict[str, Any]:
        frame = pd.read_csv(csv_path)
        return self.predict_from_points(frame.to_dict(orient="records"))

    def predict_from_split_csv(self, gps_csv_path: str, accel_csv_path: str) -> dict[str, Any]:
        gps_points = pd.read_csv(gps_csv_path).to_dict(orient="records")
        accel_points = pd.read_csv(accel_csv_path).to_dict(orient="records")
        return self.predict_from_split_points(gps_points, accel_points)


PREDICTOR = BehaviorPredictor(MODEL_PATH)


class BehaviorServiceHandler(BaseHTTPRequestHandler):
    server_version = "BehaviorService/1.0"

    def _send_json(self, payload: dict[str, Any], status: int = HTTPStatus.OK) -> None:
        body = json.dumps(payload, ensure_ascii=True).encode("utf-8")
        self.send_response(status)
        self.send_header("Content-Type", "application/json; charset=utf-8")
        self.send_header("Content-Length", str(len(body)))
        self.end_headers()
        self.wfile.write(body)

    def _read_json_body(self) -> dict[str, Any]:
        content_length = int(self.headers.get("Content-Length", "0"))
        if content_length <= 0:
            return {}
        raw = self.rfile.read(content_length)
        return json.loads(raw.decode("utf-8"))

    def do_GET(self) -> None:
        parsed = urlparse(self.path)

        if parsed.path == "/health":
            self._send_json(
                {
                    "status": "ok",
                    "model_name": PREDICTOR.model_name,
                    "feature_cols": PREDICTOR.feature_cols,
                }
            )
            return

        if parsed.path == "/predict-file":
            query = parse_qs(parsed.query)
            csv_path = query.get("path", [None])[0]
            if not csv_path:
                self._send_json({"error": "Missing query param: path"}, HTTPStatus.BAD_REQUEST)
                return

            try:
                result = PREDICTOR.predict_from_csv(csv_path)
                self._send_json(result)
            except Exception as exc:
                self._send_json({"error": str(exc)}, HTTPStatus.BAD_REQUEST)
            return

        if parsed.path == "/predict-split-files":
            query = parse_qs(parsed.query)
            gps_path = query.get("gps_path", [None])[0]
            accel_path = query.get("accel_path", [None])[0]
            if not gps_path or not accel_path:
                self._send_json(
                    {"error": "Missing query params: gps_path and accel_path"},
                    HTTPStatus.BAD_REQUEST,
                )
                return

            try:
                result = PREDICTOR.predict_from_split_csv(gps_path, accel_path)
                self._send_json(result)
            except Exception as exc:
                self._send_json({"error": str(exc)}, HTTPStatus.BAD_REQUEST)
            return

        self._send_json({"error": "Not found"}, HTTPStatus.NOT_FOUND)

    def do_POST(self) -> None:
        parsed = urlparse(self.path)

        if parsed.path == "/predict":
            try:
                payload = self._read_json_body()
                points = payload.get("points", [])
                if not isinstance(points, list) or not points:
                    raise ValueError("Body must contain a non-empty 'points' list")

                result = PREDICTOR.predict_from_points(points)
                self._send_json(result)
            except Exception as exc:
                self._send_json({"error": str(exc)}, HTTPStatus.BAD_REQUEST)
            return

        if parsed.path == "/predict-split":
            try:
                payload = self._read_json_body()
                gps_points = payload.get("gps_points", [])
                accel_points = payload.get("accel_points", [])
                if not isinstance(gps_points, list) or not gps_points:
                    raise ValueError("Body must contain a non-empty 'gps_points' list")
                if not isinstance(accel_points, list) or not accel_points:
                    raise ValueError("Body must contain a non-empty 'accel_points' list")

                result = PREDICTOR.predict_from_split_points(gps_points, accel_points)
                self._send_json(result)
            except Exception as exc:
                self._send_json({"error": str(exc)}, HTTPStatus.BAD_REQUEST)
            return

        self._send_json({"error": "Not found"}, HTTPStatus.NOT_FOUND)

    def log_message(self, format: str, *args: Any) -> None:
        return


def run_server(host: str = HOST, port: int = PORT) -> None:
    server = ThreadingHTTPServer((host, port), BehaviorServiceHandler)
    print(f"Behavior service listening on http://{host}:{port}")
    server.serve_forever()


if __name__ == "__main__":
    run_server()
