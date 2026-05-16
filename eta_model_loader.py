import json
from dataclasses import dataclass
from pathlib import Path

from xgboost import XGBRegressor


@dataclass
class ETAModelBundle:
    feature_columns: list[str]
    long_trip_seconds: int
    very_long_trip_seconds: int
    global_model: XGBRegressor
    long_model: XGBRegressor
    very_long_model: XGBRegressor
    manifest: dict


def _load_xgb_model(model_path: Path) -> XGBRegressor:
    model = XGBRegressor()
    model.load_model(model_path)
    return model


def load_eta_model_bundle(artifact_dir: str | Path = "artifacts/eta_models") -> ETAModelBundle:
    artifact_dir = Path(artifact_dir)
    manifest = json.loads((artifact_dir / "manifest.json").read_text())

    global_model = _load_xgb_model(artifact_dir / "global_log_top6.json")
    long_model = _load_xgb_model(artifact_dir / "specialized_long_log_top6.json")
    very_long_model = _load_xgb_model(artifact_dir / "specialized_very_long_log_top6.json")

    return ETAModelBundle(
        feature_columns=manifest["feature_columns"],
        long_trip_seconds=manifest["thresholds"]["long_trip_seconds"],
        very_long_trip_seconds=manifest["thresholds"]["very_long_trip_seconds"],
        global_model=global_model,
        long_model=long_model,
        very_long_model=very_long_model,
        manifest=manifest,
    )
