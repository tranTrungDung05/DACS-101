import pandas as pd
import numpy as np

def map_behavior_labels(df, mapping):
    """Cell gán nhãn: Gộp các loại NORMAL thành 0"""
    df['label'] = df['behavior'].map(mapping)
    return df

def filter_invalid_trips(df, min_dist, min_dur):
    """Cell lọc: Loại bỏ các trip rác hoặc quá ngắn"""
    initial_count = len(df)
    df = df[(df['distance_km'] >= min_dist) & (df['duration_min'] >= min_dur)]
    print(f"   [Filter] Giữ lại {len(df)}/{initial_count} trips.")
    return df

def get_feature_columns(df, meta_cols):
    """Cell chọn lọc: Chỉ lấy các cột số để làm feature X"""
    # Lấy các cột kiểu số và không nằm trong danh sách metadata
    cols = [c for c in df.columns if df[c].dtype in [np.float64, np.int64]]
    return [c for c in cols if c not in meta_cols and c != 'label']

def handle_missing_values(df, feature_cols):
    """Cell làm sạch: Điền giá trị trung vị vào các ô trống"""
    for col in feature_cols:
        if df[col].isnull().any():
            median_val = df[col].median()
            df[col] = df[col].fillna(median_val)
    return df