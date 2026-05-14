# GPS-Only ETA Simulator Design

## Goal

Them mot simulator rieng chi gui GPS vao `api/Gps/Update` de kich hoat ETA realtime, khong gui gia toc ke, khong goi phan tich hanh vi, va khong sua `simulator.py` hien tai.

## Scope

- Tao file simulator moi rieng.
- Ho tro nhieu xe chay song song.
- Moi xe doc mot file CSV GPS rieng.
- CSV dung format toi gian: `lat,lon,speed_kmh`.
- Gui 1 diem moi sau moi 15 giay.
- Payload chi gom GPS fields can thiet cho `GpsController`.

## Architecture

He thong se co 2 phan:

1. `gps_eta_simulator.py` dieu phoi danh sach xe, doc CSV va gui request HTTP.
2. Thu muc du lieu GPS mau trong repo de chua cac file trip CSV phuc vu simulator.

Moi xe chay tren mot thread rieng de co the mo phong nhieu xe cung luc, nhung moi thread chi co mot trach nhiem: doc cac diem trong file va gui lan luot vao API.

## Data Flow

1. Nguoi dung chay `gps_eta_simulator.py`.
2. Script nap danh sach cau hinh xe.
3. Moi xe doc file CSV GPS rieng.
4. Moi 15 giay, script gui 1 request `POST` toi `api/Gps/Update`.
5. `GpsController` luu GPS, tu goi ETA service, va push SignalR ETA neu du 3 diem.

## Components

- `gps_eta_simulator.py`
  - Chua `API_URL`, `INTERVAL_SECONDS`, va danh sach xe.
  - Co ham doc CSV GPS.
  - Co ham gui tung diem GPS.
  - Co ham chay da luong cho nhieu xe.

- `sample_trips/*.csv`
  - Chua file GPS da chuan hoa ve format `lat,lon,speed_kmh`.
  - Moi file tuong ung voi mot route co the gan cho mot xe.

## Payload Contract

Moi request gui len API co dang:

```json
{
  "VehicleID": 1,
  "Latitude": 10.8012,
  "Longitude": 106.7104,
  "Speed": 35.0
}
```

Khong gui `AccelBatch`, `AccelLongG`, `AccelLatG`, `GpsX`, hoac truong phu nao khac.

## Error Handling

- Neu file CSV khong ton tai: log loi va bo qua xe do.
- Neu dong CSV loi format: bo qua dong loi va tiep tuc.
- Neu API tra loi HTTP khac 200: log status code va body ngan gon.
- Neu request loi ket noi: log canh bao va tiep tuc diem tiep theo.

## Testing

- Test doc dung file CSV format `lat,lon,speed_kmh`.
- Test payload gui len API khong chua accel fields.
- Test co the chay nhieu xe song song.
- Test simulator gui du 3 diem de ETA co co hoi xuat hien.

## Out of Scope

- Tich hop gia toc ke.
- Goi API phan tich hanh vi.
- Sinh route tu OSRM.
- Tu dong tao CSV tu notebook.
