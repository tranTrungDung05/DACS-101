# Hướng dẫn triển khai DeepTTETransformer (FastAPI) cho Chengdu

Thư mục này chứa toàn bộ các file cần thiết để triển khai mô hình dự đoán ETA (DeepTTETransformer) trên máy khác mà không cần cài đặt framework LibCity cồng kềnh.

## 📁 Thành phần các file
* `model.py`: Chứa toàn bộ kiến trúc mô hình `DeepTTETransformer` (đã viết tách rời độc lập, chỉ phụ thuộc PyTorch).
* `app.py`: File khởi chạy FastAPI.
* `model_weights.m`: File trọng số mô hình đã huấn luyện xong (MAPE 18.63% trên Chengdu).
* `data_feature_Chengdu.json`: Lưu trữ các thông số chuẩn hóa dữ liệu (mean & std) của Chengdu.
* `sample_trip.json`: Dữ liệu JSON mô tả chuyến đi đầu tiên (`traj_id = 0`) trong tập dữ liệu Chengdu thực tế. Bạn có thể dùng file này để gửi trực tiếp bằng Postman.
* `send_test.py`: File python mẫu tự động đọc `sample_trip.json` và gửi yêu cầu tới FastAPI, nhận kết quả dự báo thời gian và hiển thị.

---

## 🛠️ Hướng dẫn cài đặt và khởi chạy

### Bước 1: Cài đặt các thư viện cần thiết
Hãy cài đặt Python (phiên bản khuyến nghị >= 3.8) và cài các thư viện sau:
```bash
pip install fastapi uvicorn torch pydantic
```

### Bước 2: Khởi động FastAPI server
Chạy lệnh uvicorn từ thư mục này:
```bash
uvicorn app:app --host 0.0.0.0 --port 8000 --reload
```
Khi chạy thành công, Terminal sẽ hiển thị log báo: `Successfully loaded DeepTTETransformer weights on cuda` (hoặc `cpu`).

### Bước 3: Gửi yêu cầu dự báo ETA (Test Request)
Có hai cách để kiểm tra dự báo ETA:

#### Cách 1: Sử dụng file Python Test có sẵn (Khuyên dùng)
Mở một cửa sổ Terminal mới tại thư mục này và chạy lệnh:
```bash
python send_test.py
```
Màn hình sẽ hiển thị thông tin chuyến đi thực tế và kết quả dự báo ETA trả về từ API.

#### Cách 2: Sử dụng Postman hoặc Curl gửi thủ công
Bạn có thể gửi yêu cầu HTTP POST tới địa chỉ `http://localhost:8000/predict_eta` bằng Postman hoặc curl:

**Ví dụ curl:**
```bash
curl -X POST "http://localhost:8000/predict_eta" \
     -H "Content-Type: application/json" \
     -d '{
       "uid": 810,
       "weekid": 6,
       "timeid": 1109,
       "current_longi": [104.115353, 104.113091, 104.110404, 104.108335],
       "current_lati": [30.64392, 30.642129, 30.64393, 30.640667],
       "current_dis": [0.0, 0.294091, 0.619951, 1.033260],
       "current_state": [1, 1, 1, 1]
     }'
```

**Kết quả trả về sẽ có dạng:**
```json
{
  "eta_seconds": 156.45,
  "eta_minutes": 2.61,
  "total_distance_km": 1.03326
}
```

