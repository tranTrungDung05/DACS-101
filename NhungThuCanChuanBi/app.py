from fastapi import FastAPI, HTTPException
from pydantic import BaseModel
import torch
import json
import os
from model import DeepTTETransformer

app = FastAPI(title="DeepTTETransformer Chengdu ETA Service")

# Locate directories
CURRENT_DIR = os.path.dirname(os.path.abspath(__file__))
DATA_FEATURE_PATH = os.path.join(CURRENT_DIR, "data_feature_Chengdu.json")
MODEL_WEIGHTS_PATH = os.path.join(CURRENT_DIR, "model_weights.m")

# 1. Load data_feature configuration dynamically
if not os.path.exists(DATA_FEATURE_PATH):
    raise FileNotFoundError(f"Cannot find Chengdu config file at: {DATA_FEATURE_PATH}")
with open(DATA_FEATURE_PATH, "r") as f:
    data_feature = json.load(f)

# 2. Setup configuration matching our training setup
config_args = {
    "uid_emb_size": 16,
    "weekid_emb_size": 3,
    "timdid_emb_size": 8,
    "hidden_size": 128,
    "num_filter": 32,
    "kernel_size": 3,
    "data_feature": data_feature
}

# 3. Initialize model
device = torch.device("cuda" if torch.cuda.is_available() else "cpu")
model = DeepTTETransformer(
    attr_size=28, 
    kernel_size=config_args["kernel_size"], 
    num_filter=config_args["num_filter"],
    hidden_size=config_args["hidden_size"],
    data_feature=config_args["data_feature"],
    device=device
).to(device)

# 4. Load weights checkpoint
if not os.path.exists(MODEL_WEIGHTS_PATH):
    raise FileNotFoundError(f"Cannot find weight model checkpoint at: {MODEL_WEIGHTS_PATH}")
checkpoint = torch.load(MODEL_WEIGHTS_PATH, map_location=device)
model.load_state_dict(checkpoint[0])
model.eval()
print(f"Successfully loaded DeepTTETransformer weights on {device}!")

# 5. Define request schema
class ETARequest(BaseModel):
    uid: int              # Driver ID
    weekid: int           # Day of the week (0 to 6)
    timeid: int           # Minute of the day (0 to 1439)
    current_longi: list[float]  # List of route longitude coordinate sequence
    current_lati: list[float]   # List of route latitude coordinate sequence
    current_dis: list[float]    # List of cumulative distance sequence (Haversine)
    current_state: list[int]    # List of segment travel state indicators (usually 0s or 1s)

@app.post("/predict_eta")
def predict_eta(data: ETARequest):
    try:
        # Validate that list dimensions match
        n_points = len(data.current_longi)
        if len(data.current_lati) != n_points or len(data.current_dis) != n_points or len(data.current_state) != n_points:
            raise ValueError("All coordinate list sizes must match (current_longi, current_lati, current_dis, current_state).")

        # Sum of segments is the total distance of the trip
        total_dist = data.current_dis[-1]

        # Structure batch (batch_size = 1)
        batch = {
            "uid": torch.tensor([data.uid]).long().to(device),
            "weekid": torch.tensor([data.weekid]).long().to(device),
            "timeid": torch.tensor([data.timeid]).long().to(device),
            "dist": torch.tensor([total_dist]).float().to(device),
            "current_longi": torch.tensor([data.current_longi]).float().to(device),
            "current_lati": torch.tensor([data.current_lati]).float().to(device),
            "current_dis": torch.tensor([data.current_dis]).float().to(device),
            "current_state": torch.tensor([data.current_state]).float().to(device),
        }
        
        # Run inference
        with torch.no_grad():
            eta_seconds = model.predict(batch)
            
        return {
            "eta_seconds": float(eta_seconds.cpu().item()),
            "eta_minutes": float(eta_seconds.cpu().item() / 60.0),
            "total_distance_km": float(total_dist)
        }
        
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))
