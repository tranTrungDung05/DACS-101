from dataclasses import asdict

from fastapi import FastAPI
from pydantic import BaseModel, Field

from Services.eta_inference import predict_eta_from_points


class GPSPointRequest(BaseModel):
    lat: float = Field(..., description="Latitude in decimal degrees.")
    lon: float = Field(..., description="Longitude in decimal degrees.")


class DestinationPoint(BaseModel):
    lat: float = Field(..., description="Destination latitude in decimal degrees.")
    lon: float = Field(..., description="Destination longitude in decimal degrees.")


class PredictRequest(BaseModel):
    gps_points: list[GPSPointRequest] = Field(
        ...,
        description="Observed GPS points in {lat, lon} format from the C# ETA client.",
    )
    destination: DestinationPoint = Field(
        ...,
        description="Destination point in {lat, lon} format.",
    )
    trip_start_timestamp: int | None = Field(
        default=None,
        description="Optional Unix timestamp for trip start time.",
    )


class HealthResponse(BaseModel):
    status: str


app = FastAPI(title="Porto ETA API", version="1.0.0")


@app.get("/health", response_model=HealthResponse)
def health() -> HealthResponse:
    return HealthResponse(status="ok")


@app.post("/predict")
def predict(request: PredictRequest):
    points = [(point.lon, point.lat) for point in request.gps_points]
    destination = (request.destination.lon, request.destination.lat)

    result = predict_eta_from_points(
        points=points,
        destination=destination,
        trip_start_timestamp=request.trip_start_timestamp,
    )
    return asdict(result)
