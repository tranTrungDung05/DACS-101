import requests
import time
import json
import random
import threading

# CONFIGURATION
# Cập nhật API_URL khớp với port của app (Ví dụ: 5025)
API_URL = "http://localhost:5025/api/Gps/Update"
INTERVAL = 3    # Khoảng cách giữa các lần gửi (giây)

# Danh sách cấu hình xe giả lập (Dùng ID thực tế từ DB: 13, 14)
VEHICLES_CONFIG = [
    {
        "id": 1,
        "name": "Xe 51H-999.88 (Quận 1 -> Quận 7)",
        "start": "106.6983,10.7715", # Long,Lat
        "end": "106.7011,10.7291"
    },
    # {
    #     "id": 2,
    #     "name": "Xe 21A-111.22 (Nhà Duy -> HUTECH)",
    #     "start": "106.73417,10.86778", # Tam Bình
    #     "end": "106.7188,10.7960"    # Đại học HUTECH
    # }
]

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

def simulate_vehicle(config):
    route = get_route(config)
    if not route: return

    vehicle_id = config['id']
    print(f"🚀 Bắt đầu giả lập {config['name']} (ID: {vehicle_id})...")
    
    for i, point in enumerate(route):
        lng, lat = point
        speed = random.uniform(30, 60)
        
        # Thỉnh thoảng cho xe 51H chạy quá tốc độ (Id: 13) để test vi phạm
        if vehicle_id == 1 and random.random() < 0.15: # 15% xác suất
            speed = random.uniform(85, 110)
        
        payload = {
            "VehicleID": vehicle_id,
            "Latitude": lat,
            "Longitude": lng,
            "Speed": speed
        }

        try:
            res = requests.post(API_URL, json=payload, timeout=5)
            status = "OK" if res.status_code == 200 else f"ERR {res.status_code}"
            print(f"[{i+1}/{len(route)}] {config['name']}: {status} | {lat:.5f}, {lng:.5f} | {speed:.1f} km/h")
        except Exception as e:
            print(f"❌ {config['name']}: Lỗi gửi dữ liệu: {str(e)}")

        time.sleep(INTERVAL)

    print(f"🏁 {config['name']} đã hoàn thành lộ trình!")

def run_simulation():
    threads = []
    for config in VEHICLES_CONFIG:
        t = threading.Thread(target=simulate_vehicle, args=(config,))
        t.start()
        threads.append(t)
        # Delay nhẹ giữa các xe để tránh spam API
        time.sleep(1)

    for t in threads:
        t.join()

if __name__ == "__main__":
    print(f"==========================================")
    print(f"   GIẢ LẬP LỘ TRÌNH THÔNG MINH (OSRM)     ")
    print(f"==========================================")
    print(f"API đích: {API_URL}")
    print(f"Số lượng xe: {len(VEHICLES_CONFIG)}")
    print(f"------------------------------------------\n")
    run_simulation()
