import json
import urllib.request
import urllib.error
import os

def main():
    # Locate sample json file
    current_dir = os.path.dirname(os.path.abspath(__file__))
    json_path = os.path.join(current_dir, "sample_trip.json")
    
    if not os.path.exists(json_path):
        print(f"Error: sample_trip.json not found at {json_path}")
        return

    # Load sample trip data
    with open(json_path, "r") as f:
        payload = json.load(f)
        
    print("--- CHUẨN BỊ GỬI YÊU CẦU DỰ BÁO ETA ---")
    print(f"Tài xế (uid): {payload['uid']}")
    print(f"Thứ trong tuần (weekid): {payload['weekid']} (Chủ Nhật)")
    print(f"Thời điểm xuất phát: {payload['timeid']} phút (18h29)")
    print(f"Tổng số điểm tọa độ: {len(payload['current_longi'])} điểm GPS")
    print(f"Tổng chiều dài lộ trình: {payload['current_dis'][-1]} km")
    print("---------------------------------------")

    url = "http://localhost:8000/predict_eta"
    req_data = json.dumps(payload).encode("utf-8")
    
    req = urllib.request.Request(
        url, 
        data=req_data, 
        headers={"Content-Type": "application/json"},
        method="POST"
    )

    try:
        print("Đang kết nối tới FastAPI server tại http://localhost:8000 ...")
        with urllib.request.urlopen(req) as response:
            res_body = response.read().decode("utf-8")
            res_json = json.loads(res_body)
            
            print("\n🚀 KẾT QUẢ DỰ BÁO ETA THÀNH CÔNG:")
            print(f"⏱️ Tổng thời gian dự kiến (Giây): {res_json['eta_seconds']:.2f} giây")
            print(f"⏱️ Tổng thời gian dự kiến (Phút): {res_json['eta_minutes']:.2f} phút")
            print(f"📏 Tổng quãng đường: {res_json['total_distance_km']:.2f} km")
            
    except urllib.error.URLError as e:
        print(f"\n❌ Lỗi kết nối: Không thể kết nối tới server. Bạn đã chạy server FastAPI chưa?")
        print("Mẹo: Hãy chạy lệnh: uvicorn app:app --reload")
        print(f"Chi tiết lỗi: {e}")
    except Exception as e:
        print(f"\n❌ Có lỗi xảy ra: {e}")

if __name__ == "__main__":
    main()
