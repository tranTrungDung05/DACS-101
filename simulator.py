import requests
import time
import json
import random
import threading
import csv
import os
from datetime import datetime

# CONFIGURATION
API_URL = "http://localhost:5025/api/Gps/Update"
ANALYZE_URL = "http://localhost:5025/api/Gps/AnalyzeBehavior"
INTERVAL = 3    # Khoảng cách giữa các lần gửi (giây)

# Danh sách cấu hình xe giả lập
VEHICLES_CONFIG = [
    {
        "id": 1,
        "name": "Xe 51H-999.88 (NORMAL)",
        "gps_file": "uah_trip_gps_stream.csv",
        "accel_file": "uah_trip_accel_stream.csv",
        "start": "106.6983,10.7715",
        "end": "106.7011,10.7291"
    },
    {
        "id": 2,
        "name": "Xe 21A-111.22 (AGGRESSIVE)",
        "gps_file": "uah_trip_aggressive_gps_200_300s.csv",
        "accel_file": "uah_trip_aggressive_accel_200_300s.csv",
        "start": "106.73417,10.86778",
        "end": "106.7188,10.7960"
    }
]

def load_uah_accel_data(csv_path):
    """Đọc dữ liệu gia tốc kế từ file CSV của UAH dataset"""
    accel_data = []
    if not os.path.exists(csv_path):
        print(f"⚠️ Không tìm thấy file gia tốc kế: {csv_path}")
        return accel_data
    
    with open(csv_path, 'r') as f:
        reader = csv.DictReader(f)
        for row in reader:
            sample = {
                "timestamp_s": float(row["timestamp_s"]),
                "accel_long_g": float(row["accel_long_g"]),
                "accel_lat_g": float(row["accel_lat_g"])
            }
            if "accel_lat_smooth_g" in row:
                sample["accel_lat_smooth_g"] = float(row["accel_lat_smooth_g"])
            accel_data.append(sample)
    print(f"📊 Đã nạp {len(accel_data)} mẫu gia tốc kế từ {csv_path}")
    return accel_data

def load_uah_gps_data(csv_path):
    """Đọc dữ liệu GPS từ file CSV của UAH dataset"""
    gps_data = []
    if not os.path.exists(csv_path):
        print(f"⚠️ Không tìm thấy file GPS: {csv_path}")
        return gps_data
    
    with open(csv_path, 'r') as f:
        reader = csv.DictReader(f)
        for row in reader:
            point = {
                "timestamp_s": float(row["timestamp_s"]),
                "lat": float(row["lat"]),
                "lon": float(row["lon"]),
                "speed_kmh": float(row["speed_kmh"])
            }
            if "gps_x" in row:
                point["gps_x"] = float(row["gps_x"])
            gps_data.append(point)
    print(f"📍 Đã nạp {len(gps_data)} điểm GPS từ {csv_path}")
    return gps_data

def get_route(config):
    print(f"📡 Đang truy vấn lộ trình cho {config['name']}...")
    url = f"http://router.project-osrm.org/route/v1/driving/{config['start']};{config['end']}?overview=full&geometries=geojson"
    try:
        response = requests.get(url)
        if response.status_code == 200:
            data = response.json()
            coordinates = data['routes'][0]['geometry']['coordinates']
            print(f"✅ {config['name']}: Đã tìm thấy lộ trình với {len(coordinates)} điểm.")
            return coordinates
        else:
            print(f"❌ {config['name']}: Lỗi OSRM {response.status_code}")
            return None
    except Exception as e:
        print(f"❌ {config['name']}: Lỗi kết nối OSRM: {str(e)}")
        return None

def analyze_trip(hanh_trinh_id, vehicle_name):
    """Gọi C# API để phân tích hành vi - C# sẽ đọc dữ liệu từ DB và gọi behavior_service"""
    print(f"\n🔍 Đang yêu cầu phân tích hành vi cho hành trình #{hanh_trinh_id} ({vehicle_name})...")
    
    try:
        res = requests.post(f"{ANALYZE_URL}/{hanh_trinh_id}", timeout=15)
        if res.status_code == 200:
            result = res.json()
            prediction = result.get("phanLoai", "UNKNOWN")
            so_gps = result.get("soLuongGPS", 0)
            so_accel = result.get("soLuongAccel", 0)
            
            emoji = "✅" if prediction == "NORMAL" else "⚠️" if prediction == "AGGRESSIVE" else "😴"
            print(f"\n{'='*55}")
            print(f"{emoji} KẾT QUẢ PHÂN TÍCH HÀNH VI - {vehicle_name}")
            print(f"   Hành trình: #{hanh_trinh_id}")
            print(f"   Phân loại : {prediction}")
            print(f"   Dữ liệu  : {so_gps} GPS + {so_accel} Accel (từ Database)")
            print(f"{'='*55}\n")
            return result
        else:
            print(f"⚠️ Lỗi phân tích (HTTP {res.status_code}): {res.text}")
    except Exception as e:
        print(f"⚠️ Không thể kết nối API phân tích: {str(e)}")
    return None

def simulate_vehicle_with_uah(config):
    """Giả lập xe sử dụng dữ liệu thực từ UAH dataset (Gửi kèm lô gia tốc kế)"""
    vehicle_id = config['id']
    gps_file = config.get('gps_file', 'uah_trip_gps_stream.csv')
    accel_file = config.get('accel_file', 'uah_trip_accel_stream.csv')

    gps_data = load_uah_gps_data(gps_file)
    accel_data = load_uah_accel_data(accel_file)

    if not gps_data:
        print(f"❌ {config['name']}: Không nạp được dữ liệu GPS.")
        return

    print(f"🚀 Bắt đầu giả lập {config['name']} (ID: {vehicle_id}) với dữ liệu UAH ({len(gps_data)} điểm GPS)...")
    
    last_accel_idx = 0
    
    for i, gps_point in enumerate(gps_data):
        current_gps_ts = gps_point["timestamp_s"]
        
        # Gom tất cả các mẫu gia tốc từ điểm GPS trước đó cho đến điểm hiện tại
        batch = []
        while last_accel_idx < len(accel_data) and accel_data[last_accel_idx]["timestamp_s"] <= current_gps_ts:
            sample = accel_data[last_accel_idx]
            batch_item = {
                "AccelLongG": sample["accel_long_g"],
                "AccelLatG": sample["accel_lat_g"],
                "Timestamp": datetime.now().isoformat() # Sử dụng giờ hiện tại để đồng bộ DB
            }
            if "accel_lat_smooth_g" in sample:
                batch_item["AccelLatSmoothG"] = sample["accel_lat_smooth_g"]
            batch.append(batch_item)
            last_accel_idx += 1
        
        payload = {
            "VehicleID": vehicle_id,
            "Latitude": gps_point["lat"],
            "Longitude": gps_point["lon"],
            "Speed": gps_point["speed_kmh"],
            "AccelBatch": batch
        }
        if "gps_x" in gps_point:
            payload["GpsX"] = gps_point["gps_x"]
        
        try:
            res = requests.post(API_URL, json=payload, timeout=5)
            status = "OK" if res.status_code == 200 else f"ERR {res.status_code}"
            batch_info = f" | Batch: {len(batch)} mẫu Accel" if batch else " | No Accel"
            print(f"[{i+1}/{len(gps_data)}] {config['name']}: {status} | Speed: {gps_point['speed_kmh']:.1f} km/h{batch_info}")
        except Exception as e:
            print(f"❌ {config['name']}: Lỗi gửi dữ liệu: {str(e)}")
        
        time.sleep(INTERVAL)
    
    # Khi kết thúc chuyến
    print(f"\n🏁 {config['name']} đã hoàn thành lộ trình!")

def simulate_vehicle(config, accel_data):
    """Giả lập xe sử dụng OSRM route + dữ liệu gia tốc từ UAH"""
    route = get_route(config)
    if not route: return

    vehicle_id = config['id']
    
    # THIẾT LẬP HẠN MỨC VI PHẠM (0 - 3 lần)
    target_violations = random.randint(0, 3)
    current_violations = 0
    
    print(f"🚀 Bắt đầu giả lập {config['name']} (ID: {vehicle_id}) | Mục tiêu vi phạm: {target_violations} lần...")
    
    for i, point in enumerate(route):
        lng, lat = point
        
        speed = random.uniform(30, 55)
        
        if current_violations < target_violations:
            if random.random() < 0.05:
                speed = random.uniform(90, 110)
                current_violations += 1
                print(f"⚠️ {config['name']} đang cố ý chạy quá tốc độ (Lần {current_violations}/{target_violations})")
        
        accel_sample = random.choice(accel_data) if accel_data else None
        
        payload = {
            "VehicleID": vehicle_id,
            "Latitude": lat,
            "Longitude": lng,
            "Speed": speed,
            "AccelBatch": [{
                "AccelLongG": accel_sample["accel_long_g"] if accel_sample else 0,
                "AccelLatG": accel_sample["accel_lat_g"] if accel_sample else 0,
                "Timestamp": datetime.now().isoformat()
            }] if accel_sample else []
        }

        try:
            res = requests.post(API_URL, json=payload, timeout=5)
            status = "OK" if res.status_code == 200 else f"ERR {res.status_code}"
            print(f"[{i+1}/{len(route)}] {config['name']}: {status} | Speed: {speed:.1f} km/h")
        except Exception as e:
            print(f"❌ {config['name']}: Lỗi gửi dữ liệu: {str(e)}")

        time.sleep(INTERVAL)

    print(f"\n🏁 {config['name']} đã hoàn thành lộ trình! Tổng vi phạm: {current_violations} lần.")

def analyze_all_trips():
    """Chế độ 3: Phân tích hành trình đã có trong DB"""
    print("\n📋 Nhập danh sách ID hành trình cần phân tích (cách nhau bằng dấu phẩy):")
    ids_input = input("   Ví dụ: 1,2,3 hoặc nhập ID đơn lẻ: ").strip()
    
    ids = [int(x.strip()) for x in ids_input.split(",") if x.strip().isdigit()]
    
    if not ids:
        print("⚠️ Không có ID hợp lệ.")
        return
    
    print(f"\n🔄 Đang phân tích {len(ids)} hành trình...\n")
    for hanh_trinh_id in ids:
        analyze_trip(hanh_trinh_id, f"Hành trình #{hanh_trinh_id}")

def run_simulation():
    print(f"\nChọn chế độ:")
    print(f"  1. OSRM Route (lộ trình thực, gia tốc mô phỏng từ UAH)")
    print(f"  2. UAH Dataset (sử dụng hoàn toàn dữ liệu thực từ UAH)")
    print(f"  3. Phân tích hành trình từ Database (không giả lập, chỉ phân tích)")
    
    mode = input("\nNhập lựa chọn (1, 2 hoặc 3): ").strip()
    
    if mode == "2":
        threads = []
        for config in VEHICLES_CONFIG:
            t = threading.Thread(target=simulate_vehicle_with_uah, args=(config,))
            t.start()
            threads.append(t)
            time.sleep(1)
        
        for t in threads:
            t.join()
    elif mode == "3":
        analyze_all_trips()
    else:
        # Chế độ 1: OSRM Route
        # Dùng file accel mặc định cho OSRM
        accel_data = load_uah_accel_data("uah_trip_accel_stream.csv")
        threads = []
        for config in VEHICLES_CONFIG:
            t = threading.Thread(target=simulate_vehicle, args=(config, accel_data))
            t.start()
            threads.append(t)
            time.sleep(1)
        
        for t in threads:
            t.join()
        
    if mode in ["1", "2"]:
        # Sau khi tất cả xe hoàn thành, hỏi phân tích
        print("\n" + "="*55)
        print("🏁 TẤT CẢ CÁC XE ĐÃ HOÀN THÀNH LỘ TRÌNH!")
        print("="*55)
        
        analyze = input("\nBạn có muốn phân tích hành vi không? (y/n): ").strip().lower()
        if analyze == 'y':
            analyze_all_trips()

if __name__ == "__main__":
    print(f"==========================================")
    print(f"   GIẢ LẬP LỘ TRÌNH + GIA TỐC KẾ (UAH)   ")
    print(f"==========================================")
    print(f"API đích: {API_URL}")
    print(f"Phân tích: {ANALYZE_URL}")
    print(f"------------------------------------------\n")
    run_simulation()
