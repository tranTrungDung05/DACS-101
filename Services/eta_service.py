import json
from http import HTTPStatus
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from typing import Any

from eta_inference import predict_eta_from_points
from eta_model_loader import load_eta_model_bundle


HOST = "127.0.0.1"
PORT = 8001
BUNDLE = load_eta_model_bundle()


class ETAServiceHandler(BaseHTTPRequestHandler):
    server_version = "ETAService/1.0"

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
        if self.path == "/health":
            self._send_json({"status": "ok", "feature_columns": BUNDLE.feature_columns})
            return

        self._send_json({"error": "Not found"}, HTTPStatus.NOT_FOUND)

    def do_POST(self) -> None:
        if self.path != "/predict":
            self._send_json({"error": "Not found"}, HTTPStatus.NOT_FOUND)
            return

        try:
            payload = self._read_json_body()
            gps_points = payload.get("gps_points", [])
            destination = payload.get("destination")

            if len(gps_points) < 3:
                self._send_json({"error": "Need at least 3 GPS points."}, HTTPStatus.BAD_REQUEST)
                return

            if not destination:
                self._send_json({"error": "Missing destination."}, HTTPStatus.BAD_REQUEST)
                return

            point_tuples = [(float(point["lon"]), float(point["lat"])) for point in gps_points]
            destination_tuple = (float(destination["lon"]), float(destination["lat"]))
            prediction = predict_eta_from_points(point_tuples, destination_tuple, bundle=BUNDLE)

            self._send_json(
                {
                    "eta_seconds": prediction.eta_seconds,
                    "eta_minutes": prediction.eta_minutes,
                    "selected_model": prediction.selected_model,
                }
            )
        except Exception as exc:
            self._send_json({"error": str(exc)}, HTTPStatus.BAD_REQUEST)


if __name__ == "__main__":
    server = ThreadingHTTPServer((HOST, PORT), ETAServiceHandler)
    print(f"ETA service listening on http://{HOST}:{PORT}", flush=True)
    server.serve_forever()
