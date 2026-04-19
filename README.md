# GPS Vehicle Management System (DACS-101)

Hệ thống quản lý và giám sát hành trình phương tiện thời gian thực sử dụng ASP.NET Core 8.0 và SQL Server.

## 📋 Yêu cầu hệ thống (Prerequisites)

Trước khi bắt đầu, hãy đảm bảo máy tính của bạn đã cài đặt các công cụ sau:

1.  **Docker Desktop**: Để chạy SQL Server container.
2.  **.NET 8.0 SDK**: Để biên dịch và chạy ứng dụng.
3.  **Python 3.x**: (Tùy chọn) Để chạy bộ giả lập GPS tracker.
4.  **DBeaver hoặc SSMS**: (Tùy chọn) Để quản lý cơ sở dữ liệu trực quan.

## 🚀 Trình tự khởi chạy dự án (Getting Started)

Sau khi clone dự án về máy, hãy thực hiện theo thứ tự các bước sau:

### 1. Khởi động Cơ sở dữ liệu (Database)
Mở terminal tại thư mục gốc của dự án và chạy lệnh sau để khởi động SQL Server:
```bash
docker compose up -d
```

### 2. Cấu hình Database Schema (Migrations)
Sử dụng Entity Framework Core để tạo các bảng cần thiết trong database:
```bash
dotnet ef database update
```
*(Lưu ý: Nếu chưa có công cụ `dotnet ef`, hãy cài đặt bằng lệnh: `dotnet tool install --global dotnet-ef`)*

### 3. Chạy ứng dụng Web
Khởi động máy chủ web ASP.NET Core:
```bash
dotnet run
```
Sau khi chạy thành công, truy cập trang web tại: `http://localhost:5025`

### 4. Chạy bộ giả lập GPS (Simulator)
Để thấy xe di chuyển trên bản đồ, hãy chạy script giả lập bằng Python:
```bash
# Cài đặt thư viện cần thiết
pip install requests

# Chạy simulator
python simulator.py
```

## 🔐 Thông tin đăng nhập mặc định
- **Tài khoản**: `admin`
- **Mật khẩu**: `123`

---
*Dự án được phát triển nhằm mục đích quản lý hành trình và thống kê tín hiệu GPS thời gian thực.*
