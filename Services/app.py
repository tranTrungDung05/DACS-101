from fastapi import FastAPI, HTTPException
from pydantic import BaseModel, Field
from typing import List, Tuple, Optional
import math
import numpy as np
import pandas as pd
from xgboost import XGBRegressor
from pathlib import Path

app = FastAPI(title="Porto ETA API v2")

# Global model cache
model = None

# Locate the model file robustly
model_path = Path("xgb_model_v2.json")
if not model_path.exists():
    model_path = Path(__file__).resolve().parent.parent / "xgb_model_v2.json"

@app.on_event("startup")
def load_model():
    global model
    try:
        model = XGBRegressor()
        model.load_model(str(model_path))
        print(f"v2 ETA model loaded successfully from {model_path}.")
    except Exception as e:
        print(f"Error loading v2 model from {model_path}: {e}")

def haversine_distance_km(lon1, lat1, lon2, lat2):
    lon1, lat1, lon2, lat2 = map(math.radians, [lon1, lat1, lon2, lat2])
    dlon = lon2 - lon1
    dlat = lat2 - lat1
    a = math.sin(dlat / 2) ** 2 + math.cos(lat1) * math.cos(lat2) * math.sin(dlon / 2) ** 2
    c = 2 * math.asin(math.sqrt(a))
    earth_radius_km = 6371.0
    return earth_radius_km * c

# --- Legacy Contract Requests ---
class GPSPointRequest(BaseModel):
    lat: float = Field(..., description="Latitude in decimal degrees.")
    lon: float = Field(..., description="Longitude in decimal degrees.")
    timestamp: float | None = Field(default=None, description="Optional Unix timestamp of this GPS point.")

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

@app.get("/health", response_model=HealthResponse)
def health() -> HealthResponse:
    return HealthResponse(status="ok")

@app.post("/predict")
def predict(request: PredictRequest):
    if len(request.gps_points) < 2:
        raise HTTPException(status_code=400, detail="Need at least 2 GPS points to predict ETA.")
    if not model:
        raise HTTPException(status_code=500, detail="Model is not loaded.")

    # Convert coordinates to (lon, lat) tuples as expected by features / haversine
    points = [(point.lon, point.lat) for point in request.gps_points]
    dest_lon = request.destination.lon
    dest_lat = request.destination.lat
    
    start_lon, start_lat = points[0]
    current_lon, current_lat = points[-1]
    
    if len(request.gps_points) >= 2 and request.gps_points[0].timestamp is not None and request.gps_points[-1].timestamp is not None:
        elapsed_seconds = float(request.gps_points[-1].timestamp - request.gps_points[0].timestamp)
    else:
        elapsed_seconds = (len(points) - 1) * 15
    distance_to_destination = haversine_distance_km(current_lon, current_lat, dest_lon, dest_lat)
    distance_from_start = haversine_distance_km(start_lon, start_lat, current_lon, current_lat)
    total_distance_proxy = distance_from_start + distance_to_destination

    progress_ratio = (
        distance_from_start / total_distance_proxy
        if total_distance_proxy > 0 else 0.0
    )

    if request.trip_start_timestamp:
        start_dt = pd.to_datetime(request.trip_start_timestamp, unit="s")
    else:
        # Default to current time if not provided
        start_dt = pd.Timestamp.now()

    start_hour = start_dt.hour
    day_of_week = start_dt.dayofweek
    is_weekend = 1 if day_of_week in [5, 6] else 0

    # Build feature vector matching the training set
    feature_vector = pd.DataFrame([{
        "distance_to_destination": distance_to_destination,
        "distance_from_start": distance_from_start,
        "elapsed_seconds": elapsed_seconds,
        "progress_ratio": progress_ratio,
        "start_hour": start_hour,
        "day_of_week": day_of_week,
        "is_weekend": is_weekend
    }])

    try:
        # Model predicts log1p(remaining_eta_seconds)
        prediction_log = model.predict(feature_vector)[0]
        remaining_seconds = float(np.expm1(prediction_log))
        if remaining_seconds < 0:
            remaining_seconds = 0.0
        
        return {
            "eta_seconds": remaining_seconds,
            "eta_minutes": remaining_seconds / 60.0,
            "selected_model": "xgb_model_v2",
            "route_stage_predictions": {"xgb_model_v2": remaining_seconds},
            "features": feature_vector.to_dict(orient="records")[0]
        }
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))

# --- New Contract Requests ---
class ETARequest(BaseModel):
    points: List[Tuple[float, float]]
    destination: Tuple[float, float]
    start_timestamp: Optional[int] = None # Unix timestamp (seconds)

@app.post("/predict_eta")
def predict_eta(request: ETARequest):
    if len(request.points) < 2:
        raise HTTPException(status_code=400, detail="Need at least 2 GPS points to predict ETA.")
    if not model:
        raise HTTPException(status_code=500, detail="Model is not loaded.")

    start_lon, start_lat = request.points[0]
    current_lon, current_lat = request.points[-1]
    dest_lon, dest_lat = request.destination
    
    elapsed_seconds = (len(request.points) - 1) * 15
    distance_to_destination = haversine_distance_km(current_lon, current_lat, dest_lon, dest_lat)
    distance_from_start = haversine_distance_km(start_lon, start_lat, current_lon, current_lat)
    total_distance_proxy = distance_from_start + distance_to_destination

    progress_ratio = (
        distance_from_start / total_distance_proxy
        if total_distance_proxy > 0 else 0.0
    )

    if request.start_timestamp:
        start_dt = pd.to_datetime(request.start_timestamp, unit="s")
    else:
        # Default to current time if not provided
        start_dt = pd.Timestamp.now()

    start_hour = start_dt.hour
    day_of_week = start_dt.dayofweek
    is_weekend = 1 if day_of_week in [5, 6] else 0

    # Build feature vector matching the training set
    feature_vector = pd.DataFrame([{
        "distance_to_destination": distance_to_destination,
        "distance_from_start": distance_from_start,
        "elapsed_seconds": elapsed_seconds,
        "progress_ratio": progress_ratio,
        "start_hour": start_hour,
        "day_of_week": day_of_week,
        "is_weekend": is_weekend
    }])

    try:
        # Model predicts log1p(remaining_eta_seconds)
        prediction_log = model.predict(feature_vector)[0]
        remaining_seconds = float(np.expm1(prediction_log))
        if remaining_seconds < 0:
            remaining_seconds = 0.0
        
        return {
            "remaining_eta_seconds": remaining_seconds,
            "remaining_eta_minutes": remaining_seconds / 60.0,
            "features_used": feature_vector.to_dict(orient="records")[0]
        }
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))
