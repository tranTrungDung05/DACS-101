# Hướng dẫn triển khai DeepTTETransformer (FastAPI) cho Chengdu

Thư mục này chứa toàn bộ các file cần thiết để triển khai mô hình dự đoán ETA (DeepTTETransformer) trên máy khác mà không cần cài đặt framework LibCity cồng kềnh.

## 📁 Thành phần các file
* `model.py`: Chứa toàn bộ kiến trúc mô hình `DeepTTETransformer` (đã viết tách rời độc lập, chỉ phụ thuộc PyTorch).
* `app.py`: File khởi chạy FastAPI.
* `model_weights.m`: File trọng số mô hình đã huấn luyện xong (MAPE 18.63% trên Chengdu).
* `data_feature_Chengdu.json`: Lưu trữ các thông số chuẩn hóa dữ liệu (mean & std) của Chengdu.

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
Bạn có thể gửi yêu cầu HTTP POST tới địa chỉ `http://localhost:8000/predict_eta` bằng Postman hoặc curl:

**Ví dụ curl:**
```bash
curl -X POST "http://localhost:8000/predict_eta" \
     -H "Content-Type: application/json" \
     -d '{
       "uid": 12,
       "weekid": 2,
       "timeid": 480,
       "current_longi": [104.065, 104.066, 104.067],
       "current_lati": [30.657, 30.658, 30.659],
       "current_dis": [0.0, 0.15, 0.32],
       "current_state": [0, 0, 0]
     }'
```

**Kết quả trả về sẽ có dạng:**
```json
{
  "eta_seconds": 156.45,
  "eta_minutes": 2.61,
  "total_distance_km": 0.32
}
```
