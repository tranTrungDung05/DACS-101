# GPS Vehicle Management System (DACS-101)

Hệ thống quản lý và giám sát hành trình phương tiện thời gian thực sử dụng ASP.NET Core 8.0, SQL Server, kết hợp mô hình học máy XGBoost v2 dự đoán thời gian đến dự kiến (ETA).

---

## 📋 Yêu cầu hệ thống (Prerequisites)

Trước khi bắt đầu, hãy đảm bảo máy tính của bạn đã cài đặt các công cụ sau:

1. **Docker Desktop**: Để chạy SQL Server container.
2. **.NET 8.0 SDK**: Để biên dịch và chạy ứng dụng web.
3. **Python 3.12+**: Để khởi chạy dịch vụ dự đoán ETA và bộ giả lập GPS tracker.
4. **DBeaver hoặc SSMS**: (Tùy chọn) Để quản lý và giám sát cơ sở dữ liệu trực quan.

---

## 🚀 Trình tự khởi chạy dự án (Getting Started)

Vui lòng thực hiện khởi chạy hệ thống theo thứ tự các bước sau đây:

### 1. Khởi động Cơ sở dữ liệu (Database)
Mở terminal tại thư mục gốc của dự án và chạy lệnh sau để khởi động SQL Server dưới nền:
```bash
docker compose up -d
```

### 2. Cấu hình Database Schema (Migrations)
Sử dụng Entity Framework Core để tạo các bảng và dữ liệu khởi tạo trong database:
```bash
dotnet ef database update
```
*(Lưu ý: Nếu chưa có công cụ `dotnet ef`, hãy cài đặt bằng lệnh: `dotnet tool install --global dotnet-ef`)*

### 3. Cài đặt & Khởi chạy Dịch vụ dự đoán ETA (Python FastAPI)
Dịch vụ dự đoán ETA sử dụng mô hình học máy **XGBoost v2 (`xgb_model_v2.json`)** chạy trên nền FastAPI (cổng `8001`).

```bash
# Cài đặt các thư viện cần thiết từ requirements.txt
~/.venv/bin/pip install -r requirements.txt

# Khởi chạy dịch vụ ETA API (FastAPI)
~/.venv/bin/uvicorn api:app --port 8001 --host 127.0.0.1
```
> [!NOTE]
> Mô hình dự đoán ETA v2 hỗ trợ song song 2 API endpoint:
> - `/predict` (Legacy): Tương thích với các yêu cầu từ máy chủ C# và `gps_eta_simulator.py`.
> - `/predict_eta` (New): Nhận chuỗi tọa độ `[longitude, latitude]` trực tiếp từ mô hình học máy mới.

### 4. Khởi chạy Ứng dụng Web (ASP.NET Core)
Mở một terminal mới và chạy máy chủ ứng dụng web chính:
```bash
dotnet run
```
Sau khi chạy thành công, truy cập giao diện giám sát bản đồ thời gian thực tại: **`http://localhost:5025`**

### 5. Chạy bộ giả lập GPS Tracker (Simulators)
Để cập nhật xe di chuyển trên bản đồ và nhận dự đoán ETA thời gian thực, bạn có 2 bộ giả lập tùy chọn:

#### Lựa chọn A: Giả lập GPS từ tệp mẫu có Dự đoán ETA thực tế
Sử dụng dữ liệu hành trình Porto thực tế từ tệp CSV để cập nhật vị trí xe lên Web App đồng thời liên hệ trực tiếp FastAPI để in kết quả dự đoán ETA ra màn hình:
```bash
~/.venv/bin/python gps_eta_simulator.py
```

#### Lựa chọn B: Giả lập hành vi Lái xe & Gia tốc kế (UAH Dataset)
Giả lập xe chạy trên bản đồ, kết hợp gửi dữ liệu gia tốc kế để kiểm tra vi phạm quá tốc độ và phân tích hành vi lái xe (NORMAL / AGGRESSIVE):
```bash
~/.venv/bin/python simulator.py
```

---

## 🔐 Thông tin đăng nhập mặc định

Truy cập trang đăng nhập hệ thống và điền thông tin sau:
- **Tài khoản**: `admin`
- **Mật khẩu**: `123`

---
*Dự án được phát triển nhằm mục đích quản lý hành trình, giám sát tín hiệu GPS và tối ưu hóa dự báo thời gian đến dự kiến thời gian thực.*
