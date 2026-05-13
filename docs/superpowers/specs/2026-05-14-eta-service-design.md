# ETA Service Design

## Goal

Tich hop ETA realtime vao du an hien tai theo luong nhan GPS moi trong `GpsController`, su dung dung cum model Python ETA da co san, va push ket qua len frontend qua SignalR.

## Scope

- Tinh ETA moi khi API nhan duoc GPS moi.
- Chi tinh ETA khi hanh trinh da co it nhat 3 diem GPS.
- Su dung mot diem dich cau hinh san de demo.
- Push ETA qua SignalR theo dang don gian cho frontend.
- Khong thay doi model ETA hoac huan luyen lai model.
- Khong dua ETA vao `TripMonitorService`.

## Architecture

He thong se duoc tach thanh 3 lop:

1. `GpsController` giu vai tro kich hoat ETA sau khi luu GPS thanh cong.
2. `ETAService` trong C# boc viec goi HTTP toi Python service.
3. `eta_service.py` trong `Services` nhan payload, dung `eta_inference.py` va `eta_model_loader.py` de du doan ETA tu artifact model.

Python service duoc tach rieng khoi `behavior_service.py` de tranh ghep 2 domain khac nhau vao cung mot process. C# khong can biet logic model, chi can goi endpoint ETA va nhan ket qua.

## Data Flow

1. Client gui GPS moi vao API hien co.
2. `GpsController` luu diem GPS nhu luong hien tai.
3. `GpsController` truy van danh sach GPS cua hanh trinh hien tai theo thu tu thoi gian.
4. Neu so diem < 3, bo qua ETA va khong push SignalR.
5. Neu so diem >= 3, `GpsController` goi `ETAService`.
6. `ETAService` gui HTTP request toi `eta_service.py` kem:
   - danh sach GPS quan sat
   - toa do dich den cau hinh san
7. Python service tinh feature, chon model phu hop, tra ve ETA theo giay va phut.
8. `GpsController` format thong diep don gian nhu `thoi gian con lai den dich: 12.5p`.
9. `GpsController` push SignalR cho frontend.

## Components

### Python

- `Services/eta_service.py`
  - Expose endpoint HTTP de nhan payload ETA.
  - Tai model bundle khi khoi dong.
  - Validate du lieu dau vao.
  - Tra loi JSON ETA.

- `Services/eta_inference.py`
  - Giu nguyen logic build feature va suy luan ETA.

- `Services/eta_model_loader.py`
  - Giu nguyen logic load manifest va 3 model XGBoost.

- `Services/artifacts/eta_models/*`
  - Chua `manifest.json` va cac file model JSON.

### C#

- `Services/ETAService.cs`
  - Tao interface va implementation de goi Python ETA service.
  - Chiu trach nhiem tao payload, goi HTTP, parse ket qua, va tra ve DTO nho gon cho controller.

- `Controllers/GpsController.cs`
  - Them buoc goi ETA sau khi luu GPS.
  - Chi goi khi co du >= 3 diem GPS.
  - Push su kien SignalR ETA.

- `Program.cs`
  - Dang ky `IETAService`.

- `appsettings.json`
  - Them `EtaServiceUrl`, `EtaDestinationLat`, `EtaDestinationLon`.

## SignalR Contract

Frontend se nhan mot event moi danh cho ETA. Noi dung muc tieu toi thieu:

- ma hoac bien so xe
- `message`: chuoi da format san, vi du `thoi gian con lai den dich: 12.5p`

Ten event cu the se duoc dat theo pattern hien co trong hub/controller de frontend de bat.

## Error Handling

- Duoi 3 diem GPS: khong goi ETA, khong push.
- Python service timeout hoac loi HTTP: log canh bao va bo qua ETA, khong lam hong luong nhan GPS.
- Payload thieu du lieu/toa do khong hop le: Python tra `400`.
- Model/artifact khong load duoc: Python service fail fast khi khoi dong de loi duoc phat hien som.

## Testing

### Python

- Test endpoint voi payload hop le va khong hop le.
- Test truong hop duoi 3 diem GPS bi tu choi dung nhu thiet ke.

### C#

- Test `ETAService` parse duoc response ETA.
- Test `GpsController` khong goi ETA khi chua du 3 diem.
- Test `GpsController` push SignalR khi ETA service tra ket qua hop le.
- Test luong loi ETA service khong lam request GPS chinh bi that bai.

## Operational Notes

- ETA service duoc chay rieng, tuong tu `behavior_service.py`.
- Moi truong Python can co `numpy`, `pandas`, `xgboost`.
- Frontend chi hien thi thong diep ETA don gian, khong dung debug metadata trong pha demo nay.

## Out of Scope

- Tu dong khoi dong Python service tu C#.
- Chuyen ETA sang C# native model inference.
- Ho tro nhieu diem dich dong tu frontend.
- Toi uu hoa batch ETA hoac cache ket qua.
