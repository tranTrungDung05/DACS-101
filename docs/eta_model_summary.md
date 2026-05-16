# ETA Model Summary

## Current Best Results

### Global notebook
- `XGBoost Log-Target Top-6`
- Validation `MAE = 331.88s` (`5.53 minutes`)
- Validation `RMSE = 1344.44s` (`22.41 minutes`)

Top-6 features:
- `observed_points`
- `distance_to_destination`
- `distance_from_start`
- `progress_ratio`
- `recent_speed_avg_kmh`
- `avg_speed_since_start_kmh`

### Long-trip notebook
- `global_log_top6` on long-trip validation:
  - `MAE = 5124.71s` (`85.41 minutes`)
  - `RMSE = 6464.23s` (`107.74 minutes`)
- `specialized_long_log_top6` on long-trip validation:
  - `MAE = 2841.04s` (`47.35 minutes`)
  - `RMSE = 4312.87s` (`71.88 minutes`)

By regime:
- `60-120m`
  - global `MAE = 3257.13s`
  - specialized long `MAE = 1614.18s`
- `>120m`
  - global `MAE = 9095.99s`
  - specialized long `MAE = 5449.88s`
  - specialized very long `MAE = 3423.84s`

## Current Project Decision

Current artifact bundle keeps three regressors:
- `global_log_top6`
- `specialized_long_log_top6`
- `specialized_very_long_log_top6`

These are saved with:
- feature list
- ETA thresholds
- XGBoost parameters

Current thresholds:
- `long_trip_seconds = 3600`
- `very_long_trip_seconds = 7200`

## Files

- Export script: [scripts/export_eta_model_bundle.py](/home/duy/ml_project/scripts/export_eta_model_bundle.py)
- Loader: [eta_model_loader.py](/home/duy/ml_project/eta_model_loader.py)
- Artifact manifest path after export:
  - `artifacts/eta_models/manifest.json`

## Usage

1. Export the bundle:

```bash
python scripts/export_eta_model_bundle.py
```

2. Load the bundle in Python:

```python
from eta_model_loader import load_eta_model_bundle

bundle = load_eta_model_bundle()
print(bundle.feature_columns)
print(bundle.long_trip_seconds, bundle.very_long_trip_seconds)
```
