import json
from dataclasses import dataclass
from pathlib import Path

from xgboost import XGBRegressor


@dataclass
class ETAModelBundle:
    feature_columns: list[str]
    model_name: str
    model: XGBRegressor
    manifest: dict


def _load_xgb_model(model_path: Path) -> XGBRegressor:
    model = XGBRegressor()
    model.load_model(model_path)
    return model


def load_eta_model_bundle(artifact_dir: str | Path = "artifacts/eta_models") -> ETAModelBundle:
    artifact_dir = Path(artifact_dir)
    manifest = json.loads((artifact_dir / "manifest.json").read_text(encoding="utf-8"))

    model_config = manifest["model"]
    model_path = Path(model_config["path"])
    if not model_path.is_absolute():
        model_path = artifact_dir / model_path.name

    return ETAModelBundle(
        feature_columns=manifest["feature_columns"],
        model_name=model_config["name"],
        model=_load_xgb_model(model_path),
        manifest=manifest,
    )
