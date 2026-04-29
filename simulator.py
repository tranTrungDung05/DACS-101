import requests
import time
import json
import random
import threading
import csv
import os

# CONFIGURATION
API_URL = "http://localhost:5025/api/Gps/Update"
ANALYZE_URL = "http://localhost:5025/api/Gps/AnalyzeBehavior"
INTERVAL = 3    # Khoảng cách giữa các lần gửi (giây)

# Đường dẫn tới file dữ liệu UAH
GPS_CSV = "uah_trip_gps_stream.csv"
ACCEL_CSV = "uah_trip_accel_stream.csv"

# Danh sách cấu hình xe giả lập
VEHICLES_CONFIG = [
    {
        "id": 1,
        "name": "Xe 51H-999.88 (Quận 1 -> Quận 7)",
        "start": "106.6983,10.7715", # Long,Lat
        "end": "106.7011,10.7291"
    },
    {
        "id": 2,
        "name": "Xe 21A-111.22 (Nhà Duy -> HUTECH)",
        "start": "106.73417,10.86778", # Tam Bình
        "end": "106.7188,10.7960"    # Đại học HUTECH
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
            accel_data.append({
                "timestamp_s": float(row["timestamp_s"]),
                "accel_long_g": float(row["accel_long_g"]),
                "accel_lat_g": float(row["accel_lat_g"])
            })
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
            gps_data.append({
                "timestamp_s": float(row["timestamp_s"]),
                "lat": float(row["lat"]),
                "lon": float(row["lon"]),
                "speed_kmh": float(row["speed_kmh"])
            })
    print(f"📍 Đã nạp {len(gps_data)} điểm GPS từ {csv_path}")
    return gps_data

def find_nearest_accel(accel_data, target_timestamp):
    """Tìm mẫu gia tốc gần nhất với timestamp GPS hiện tại"""
    if not accel_data:
        return None
    
    best = accel_data[0]
    best_diff = abs(best["timestamp_s"] - target_timestamp)
    
    for sample in accel_data:
        diff = abs(sample["timestamp_s"] - target_timestamp)
        if diff < best_diff:
            best = sample
            best_diff = diff
        elif diff > best_diff:
            break
    
    return best

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

def get_active_journey_id(vehicle_id):
    """Lấy ID hành trình đang hoạt động của xe từ C# API (thông qua DB)"""
    try:
        # Gửi 1 request nhỏ để lấy response, trong đó C# backend sẽ trả về journey ID
        # Ta tận dụng thông tin từ response của lần Update cuối cùng
        return None  # Sẽ được parse từ response của Update
    except:
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

def simulate_vehicle_with_uah(config, gps_data, accel_data):
    """Giả lập xe sử dụng dữ liệu thực từ UAH dataset"""
    vehicle_id = config['id']
    last_journey_id = None
    
    print(f"🚀 Bắt đầu giả lập {config['name']} (ID: {vehicle_id}) với dữ liệu UAH ({len(gps_data)} điểm GPS)...")
    
    for i, gps_point in enumerate(gps_data):
        accel_sample = find_nearest_accel(accel_data, gps_point["timestamp_s"])
        
        payload = {
            "VehicleID": vehicle_id,
            "Latitude": gps_point["lat"],
            "Longitude": gps_point["lon"],
            "Speed": gps_point["speed_kmh"],
            "AccelLongG": accel_sample["accel_long_g"] if accel_sample else None,
            "AccelLatG": accel_sample["accel_lat_g"] if accel_sample else None
        }
        
        try:
            res = requests.post(API_URL, json=payload, timeout=5)
            status = "OK" if res.status_code == 200 else f"ERR {res.status_code}"
            accel_info = f" | Accel: ({accel_sample['accel_long_g']:.3f}g, {accel_sample['accel_lat_g']:.3f}g)" if accel_sample else ""
            print(f"[{i+1}/{len(gps_data)}] {config['name']}: {status} | Speed: {gps_point['speed_kmh']:.1f} km/h{accel_info}")
        except Exception as e:
            print(f"❌ {config['name']}: Lỗi gửi dữ liệu: {str(e)}")
        
        time.sleep(INTERVAL)
    
    # Khi kết thúc chuyến, hỏi user nhập ID hành trình để phân tích
    print(f"\n🏁 {config['name']} đã hoàn thành lộ trình!")
    try:
        hanh_trinh_id = input("📋 Nhập ID hành trình cần phân tích (xem trong DB): ").strip()
        if hanh_trinh_id.isdigit():
            analyze_trip(int(hanh_trinh_id), config['name'])
    except:
        pass

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
            "AccelLongG": accel_sample["accel_long_g"] if accel_sample else None,
            "AccelLatG": accel_sample["accel_lat_g"] if accel_sample else None
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
    accel_data = load_uah_accel_data(ACCEL_CSV)
    gps_data = load_uah_gps_data(GPS_CSV)
    
    print(f"\nChọn chế độ:")
    print(f"  1. OSRM Route (lộ trình thực, gia tốc mô phỏng từ UAH)")
    print(f"  2. UAH Dataset (sử dụng hoàn toàn dữ liệu thực từ UAH)")
    print(f"  3. Phân tích hành trình từ Database (không giả lập, chỉ phân tích)")
    
    mode = input("\nNhập lựa chọn (1, 2 hoặc 3): ").strip()
    
    if mode == "2" and gps_data:
        for config in VEHICLES_CONFIG[:1]:
            simulate_vehicle_with_uah(config, gps_data, accel_data)
    elif mode == "3":
        analyze_all_trips()
    else:
        threads = []
        for config in VEHICLES_CONFIG:
            t = threading.Thread(target=simulate_vehicle, args=(config, accel_data))
            t.start()
            threads.append(t)
            time.sleep(1)
        
        for t in threads:
            t.join()
        
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
