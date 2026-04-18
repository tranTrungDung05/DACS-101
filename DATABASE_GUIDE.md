# Hướng dẫn Kết nối Cơ sở Dữ liệu (Database Guide)

Dự án này sử dụng **Microsoft SQL Server 2022** chạy trên **Docker** để quản lý cơ sở dữ liệu.

## 1. Cấu hình Docker
Cơ sở dữ liệu được cấu hình trong tệp `docker-compose.yml`.

- **Container Name**: `dacs-sql-server`
- **Port Mapping**: `1435:1433` (Cổng 1435 trên máy host trỏ vào cổng 1433 trong container)
- **Database Engine**: SQL Server 2022 Express / Developer

### Lệnh khởi chạy:
```bash
docker compose up -d
```

## 2. Thông tin kết nối (Connection String)
Ứng dụng kết nối tới database thông qua chuỗi kết nối trong `appsettings.json`:

```json
"DefaultConnection": "Server=localhost,1435;Database=DACS_QuanLyXe;User Id=sa;Password=YourPassword123!;TrustServerCertificate=True;MultipleActiveResultSets=true"
```

## 3. Quản lý bằng DBeaver
Để xem và quản lý dữ liệu trực quan bằng DBeaver, hãy tạo một kết nối mới:

- **Type**: SQL Server
- **Host**: `localhost`
- **Port**: `1435`
- **Username**: `sa`
- **Password**: `YourPassword123!`
- **Database**: `DACS_QuanLyXe`
- **Driver Property**: `trustServerCertificate=true`

## 4. Các lệnh Entity Framework Core thường dùng

### Cập nhật Database sau khi thay đổi Model:
```bash
dotnet ef database update
```

### Tạo một bản Migration mới:
```bash
dotnet ef migrations add <TenMigration>
```

---
*Lưu ý: Nếu bạn gặp lỗi kết nối, hãy đảm bảo rằng Docker đang chạy và container `dacs-sql-server` đang ở trạng thái "Up".*
