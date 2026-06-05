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
Dịch vụ dự đoán ETA tích hợp mô hình học máy **DeepTTETransformer** (dành cho khu vực Chengdu) và **XGBoost v2** (dành cho khu vực Porto) chạy trên nền FastAPI (cổng `8001`).

Kích hoạt môi trường Conda chứa PyTorch và khởi chạy API:
```bash
# Kích hoạt môi trường conda
conda activate libcity_env

# Khởi chạy dịch vụ ETA API (FastAPI) từ thư mục gốc
uvicorn Services.app:app --port 8001 --host 127.0.0.1
```
> [!NOTE]
> Hệ thống hỗ trợ định tuyến thông minh (Multi-Region Routing):
> - Tự động phát hiện tọa độ thuộc khu vực **Chengdu** để áp dụng mô hình mạng nơ-ron **DeepTTETransformer** (với thuật toán đếm ngược ETA mượt mà).
> - Sử dụng mô hình **XGBoost v2** làm phương án mặc định hoặc khi phát hiện xe di chuyển tại khu vực **Porto**.

### 4. Khởi chạy Ứng dụng Web (ASP.NET Core)
Mở một terminal mới và chạy máy chủ ứng dụng web chính:
```bash
dotnet run
```
Sau khi chạy thành công, truy cập giao diện giám sát bản đồ thời gian thực tại: **`http://localhost:5025`**

### 5. Chạy bộ giả lập GPS Tracker (Simulators)
Để cập nhật xe di chuyển trên bản đồ và nhận dự đoán ETA thời gian thực, bạn chạy giả lập:

#### Giả lập hành trình Chengdu với mô hình DeepTTETransformer
Chạy kịch bản nạp chuỗi tọa độ thực tế của chuyến đi Chengdu, truyền dữ liệu thời gian thực lên ứng dụng C# Web App và hiển thị đếm ngược thời gian dự kiến (ETA) trực tiếp ra màn hình:
```bash
# Kích hoạt môi trường conda
conda activate libcity_env

# Chạy giả lập Chengdu
python simulate_chengdu.py
```

#### Giả lập hành vi Lái xe & Gia tốc kế (UAH Dataset)
Giả lập xe chạy trên bản đồ, kết hợp gửi dữ liệu gia tốc kế để kiểm tra vi phạm quá tốc độ và phân tích hành vi lái xe (NORMAL / AGGRESSIVE):
```bash
python simulator.py
```

---

## 🔐 Thông tin đăng nhập mặc định

Truy cập trang đăng nhập hệ thống và điền thông tin sau:
- **Tài khoản**: `admin`
- **Mật khẩu**: `123`

---
*Dự án được phát triển nhằm mục đích quản lý hành trình, giám sát tín hiệu GPS và tối ưu hóa dự báo thời gian đến dự kiến thời gian thực.*
