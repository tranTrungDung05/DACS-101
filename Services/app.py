from fastapi import FastAPI, HTTPException
from pydantic import BaseModel, Field
from typing import List, Tuple, Optional, Union
import math
import numpy as np
import pandas as pd
from xgboost import XGBRegressor
from pathlib import Path
import sys
import torch
import json
import datetime
import os

app = FastAPI(title="Multi-Region ETA Routing API")

# Global models cache
model = None  # Porto XGBoost
deeptte_model = None  # Chengdu DeepTTE
data_feature = None
device = None

# Locate paths
CURRENT_DIR = Path(__file__).resolve().parent
ROOT_DIR = CURRENT_DIR.parent
model_path = ROOT_DIR / "xgb_model_v2.json"

# Inject eta_serving to sys.path so we can import from model.py
sys.path.append(str(ROOT_DIR / "eta_serving"))
try:
    from model import DeepTTETransformer
except ImportError as e:
    print(f"Warning: could not import DeepTTETransformer: {e}")

@app.on_event("startup")
def load_models():
    global model, deeptte_model, data_feature, device
    
    # 1. Load Porto XGBoost
    try:
        model = XGBRegressor()
        model.load_model(str(model_path))
        print(f"v2 Porto XGBoost model loaded successfully from {model_path}.")
    except Exception as e:
        print(f"Error loading Porto model from {model_path}: {e}")

    # 2. Load Chengdu DeepTTE
    try:
        eta_serving_dir = ROOT_DIR / "eta_serving"
        data_feature_path = eta_serving_dir / "data_feature_Chengdu.json"
        model_weights_path = eta_serving_dir / "model_weights.m"
        
        if not data_feature_path.exists():
            raise FileNotFoundError(f"Cannot find Chengdu config file at: {data_feature_path}")
        with open(data_feature_path, "r") as f:
            data_feature = json.load(f)
            
        config_args = {
            "uid_emb_size": 16,
            "weekid_emb_size": 3,
            "timdid_emb_size": 8,
            "hidden_size": 128,
            "num_filter": 32,
            "kernel_size": 3,
            "data_feature": data_feature
        }
        
        device = torch.device("cuda" if torch.cuda.is_available() else "cpu")
        deeptte_model = DeepTTETransformer(
            attr_size=28, 
            kernel_size=config_args["kernel_size"], 
            num_filter=config_args["num_filter"],
            hidden_size=config_args["hidden_size"],
            num_final_fcs=4,
            data_feature=config_args["data_feature"],
            device=device
        ).to(device)
        
        if not model_weights_path.exists():
            raise FileNotFoundError(f"Cannot find weight model checkpoint at: {model_weights_path}")
        checkpoint = torch.load(model_weights_path, map_location=device)
        deeptte_model.load_state_dict(checkpoint[0])
        deeptte_model.eval()
        print(f"Successfully loaded DeepTTETransformer weights on {device}!")
    except Exception as e:
        print(f"Error loading DeepTTETransformer model: {e}")

def haversine_distance_km(lon1, lat1, lon2, lat2):
    lon1, lat1, lon2, lat2 = map(math.radians, [lon1, lat1, lon2, lat2])
    dlon = lon2 - lon1
    dlat = lat2 - lat1
    a = math.sin(dlat / 2) ** 2 + math.cos(lat1) * math.cos(lat2) * math.sin(dlon / 2) ** 2
    c = 2 * math.asin(math.sqrt(a))
    earth_radius_km = 6371.0
    return earth_radius_km * c

def is_in_chengdu(lon: float, lat: float) -> bool:
    # Chengdu bounding box
    return (100.0 <= lon <= 110.0) and (28.0 <= lat <= 33.0)

# --- Legacy Contract Requests ---
class GPSPointRequest(BaseModel):
    lat: float = Field(..., description="Latitude in decimal degrees.")
    lon: float = Field(..., description="Longitude in decimal degrees.")
    timestamp: Optional[float] = Field(default=None, description="Optional Unix timestamp of this GPS point.")

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
    trip_start_timestamp: Optional[int] = Field(
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

    # Convert coordinates to (lon, lat) tuples as expected by features / haversine
    points = [(point.lon, point.lat) for point in request.gps_points]
    dest_lon = request.destination.lon
    dest_lat = request.destination.lat
    
    start_lon, start_lat = points[0]
    current_lon, current_lat = points[-1]
    
    # 1. Check if the trip starting point is in Chengdu -> route to DeepTTE
    if is_in_chengdu(start_lon, start_lat):
        if not deeptte_model or not data_feature or not device:
            raise HTTPException(status_code=500, detail="DeepTTE Chengdu model is not loaded.")
        
        try:
            # Calculate cumulative distances along the GPS sequence (in km)
            current_dis = [0.0]
            total_dist = 0.0
            for i in range(1, len(points)):
                d = haversine_distance_km(points[i-1][0], points[i-1][1], points[i][0], points[i][1])
                total_dist += d
                current_dis.append(total_dist)
            
            # Map sequence coordinates
            current_longi = [p[0] for p in points]
            current_lati = [p[1] for p in points]
            
            # Use all 1s for state indicators
            current_state = [1] * len(points)
            
            # Get temporal attributes
            now = datetime.datetime.now()
            last_timestamp = request.gps_points[-1].timestamp
            if last_timestamp is not None:
                dt = datetime.datetime.fromtimestamp(last_timestamp)
                weekid_val = dt.weekday()
                timeid_val = dt.hour * 60 + dt.minute
            elif request.trip_start_timestamp is not None:
                dt = datetime.datetime.fromtimestamp(request.trip_start_timestamp)
                weekid_val = dt.weekday()
                timeid_val = dt.hour * 60 + dt.minute
            else:
                weekid_val = now.weekday()
                timeid_val = now.hour * 60 + now.minute
            
            # Build input batch
            batch = {
                "uid": torch.tensor([810]).long().to(device), # Default driver ID
                "weekid": torch.tensor([weekid_val]).long().to(device),
                "timeid": torch.tensor([timeid_val]).long().to(device),
                "dist": torch.tensor([total_dist]).float().to(device),
                "current_longi": torch.tensor([current_longi]).float().to(device),
                "current_lati": torch.tensor([current_lati]).float().to(device),
                "current_dis": torch.tensor([current_dis]).float().to(device),
                "current_state": torch.tensor([current_state]).float().to(device),
            }
            
            # Run inference
            with torch.no_grad():
                eta_seconds = deeptte_model.predict(batch)
            eta_val = float(eta_seconds.cpu().item())
            
            distance_to_destination = haversine_distance_km(current_lon, current_lat, dest_lon, dest_lat)
            
            # Scale the predicted travel time using distance progress so it counts down to 0 at the destination
            total_distance_proxy = total_dist + distance_to_destination
            if total_distance_proxy > 0:
                progress = total_dist / total_distance_proxy
            else:
                progress = 0.0
            
            remaining_seconds = (1.0 - progress) * eta_val
            
            # If extremely close to destination (within 30m), force to 0
            if distance_to_destination <= 0.03:
                remaining_seconds = 0.0
            
            features_dict = {
                "distance_to_destination": distance_to_destination,
                "distance_from_start": total_dist,
                "elapsed_seconds": float(request.gps_points[-1].timestamp - request.gps_points[0].timestamp) if (request.gps_points[0].timestamp is not None and request.gps_points[-1].timestamp is not None) else (len(points) - 1) * 15,
                "weekid": weekid_val,
                "timeid": timeid_val
            }
            
            return {
                "eta_seconds": remaining_seconds,
                "eta_minutes": remaining_seconds / 60.0,
                "selected_model": "deeptte_chengdu",
                "route_stage_predictions": {"deeptte_chengdu": remaining_seconds},
                "features": features_dict
            }
        except Exception as e:
            raise HTTPException(status_code=500, detail=f"DeepTTE inference error: {str(e)}")

    # 2. Otherwise, route to Porto XGBoost
    if not model:
        raise HTTPException(status_code=500, detail="XGBoost Porto model is not loaded.")
        
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


# --- New Contract Requests (overloaded to support both Porto and Chengdu schemas) ---
class ChengduETARequest(BaseModel):
    uid: int                                   # Driver ID
    weekid: Optional[int] = None               # Day of the week (0 to 6)
    timeid: Optional[int] = None               # Minute of the day (0 to 1439)
    current_longi: list[float]                 # List of longitude coordinates
    current_lati: list[float]                  # List of latitude coordinates
    current_dis: list[float]                   # List of cumulative distance
    current_state: list[int]                   # List of travel states

class PortoETARequest(BaseModel):
    points: List[Tuple[float, float]]
    destination: Tuple[float, float]
    start_timestamp: Optional[int] = None

@app.post("/predict_eta")
def predict_eta(request: Union[ChengduETARequest, PortoETARequest]):
    # A. If it's a ChengduETARequest (contains current_longi or is an instance of ChengduETARequest)
    if isinstance(request, ChengduETARequest) or hasattr(request, "current_longi"):
        if not deeptte_model or not device:
            raise HTTPException(status_code=500, detail="DeepTTE Chengdu model is not loaded.")
        try:
            n_points = len(request.current_longi)
            if len(request.current_lati) != n_points or len(request.current_dis) != n_points or len(request.current_state) != n_points:
                raise ValueError("All coordinate list sizes must match (current_longi, current_lati, current_dis, current_state).")

            total_dist = request.current_dis[-1]
            now = datetime.datetime.now()
            weekid_val = request.weekid if request.weekid is not None else now.weekday()
            timeid_val = request.timeid if request.timeid is not None else (now.hour * 60 + now.minute)

            batch = {
                "uid": torch.tensor([request.uid]).long().to(device),
                "weekid": torch.tensor([weekid_val]).long().to(device),
                "timeid": torch.tensor([timeid_val]).long().to(device),
                "dist": torch.tensor([total_dist]).float().to(device),
                "current_longi": torch.tensor([request.current_longi]).float().to(device),
                "current_lati": torch.tensor([request.current_lati]).float().to(device),
                "current_dis": torch.tensor([request.current_dis]).float().to(device),
                "current_state": torch.tensor([request.current_state]).float().to(device),
            }

            with torch.no_grad():
                eta_seconds = deeptte_model.predict(batch)
            remaining_seconds = float(eta_seconds.cpu().item())

            return {
                "eta_seconds": remaining_seconds,
                "eta_minutes": remaining_seconds / 60.0,
                "total_distance_km": float(total_dist),
                "selected_model": "deeptte_chengdu"
            }
        except Exception as e:
            raise HTTPException(status_code=500, detail=str(e))

    # B. Otherwise, it is a PortoETARequest
    if len(request.points) < 2:
        raise HTTPException(status_code=400, detail="Need at least 2 GPS points to predict ETA.")
        
    start_lon, start_lat = request.points[0]
    current_lon, current_lat = request.points[-1]
    dest_lon, dest_lat = request.destination

    # If it is in Chengdu coordinates range, we can also automatically run it through DeepTTE!
    if is_in_chengdu(start_lon, start_lat):
        if not deeptte_model or not device:
            raise HTTPException(status_code=500, detail="DeepTTE Chengdu model is not loaded.")
        try:
            current_dis = [0.0]
            total_dist = 0.0
            for i in range(1, len(request.points)):
                d = haversine_distance_km(request.points[i-1][0], request.points[i-1][1], request.points[i][0], request.points[i][1])
                total_dist += d
                current_dis.append(total_dist)

            current_longi = [p[0] for p in request.points]
            current_lati = [p[1] for p in request.points]
            current_state = [1] * len(request.points)

            now = datetime.datetime.now()
            if request.start_timestamp:
                dt = datetime.datetime.fromtimestamp(request.start_timestamp)
                weekid_val = dt.weekday()
                timeid_val = dt.hour * 60 + dt.minute
            else:
                weekid_val = now.weekday()
                timeid_val = now.hour * 60 + now.minute

            batch = {
                "uid": torch.tensor([810]).long().to(device),
                "weekid": torch.tensor([weekid_val]).long().to(device),
                "timeid": torch.tensor([timeid_val]).long().to(device),
                "dist": torch.tensor([total_dist]).float().to(device),
                "current_longi": torch.tensor([current_longi]).float().to(device),
                "current_lati": torch.tensor([current_lati]).float().to(device),
                "current_dis": torch.tensor([current_dis]).float().to(device),
                "current_state": torch.tensor([current_state]).float().to(device),
            }

            with torch.no_grad():
                eta_seconds = deeptte_model.predict(batch)
            remaining_seconds = float(eta_seconds.cpu().item())

            return {
                "remaining_eta_seconds": remaining_seconds,
                "remaining_eta_minutes": remaining_seconds / 60.0,
                "selected_model": "deeptte_chengdu"
            }
        except Exception as e:
            raise HTTPException(status_code=500, detail=str(e))

    # Otherwise, fallback to XGBoost
    if not model:
        raise HTTPException(status_code=500, detail="XGBoost Porto model is not loaded.")

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
        start_dt = pd.Timestamp.now()

    start_hour = start_dt.hour
    day_of_week = start_dt.dayofweek
    is_weekend = 1 if day_of_week in [5, 6] else 0

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
