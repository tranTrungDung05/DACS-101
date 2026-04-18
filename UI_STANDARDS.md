# UI Standards & Branding

Tài liệu này quy định các thông số thiết kế chung để đảm bảo tính nhất quán trên toàn bộ hệ thống Tracking GPS.

## 1. Bảng màu (Color Palette)

| Loại | Mã màu | Sử dụng |
| :--- | :--- | :--- |
| **Primary** | `#0d6efd` | Nút bấm chính, Brand logo, Active states |
| **Secondary (Dark)** | `#343a40` | Header navbar, Sidebar background |
| **Background (Light)** | `#f4f6f9` | Nền chung của các trang Dashboard/Tracking |
| **Success** | `#198754` (`#28a745`) | Trạng thái xe hoạt động, Nút "Hoàn tất" |
| **Warning** | `#ffc107` | Trạng thái xe dừng, Cảnh báo nhẹ |
| **Danger** | `#dc3545` | Vi phạm, Cảnh báo khẩn cấp, Trạng thái mất kết nối |
| **Border/Divider** | `#dee2e6` / `#eee` | Đường kẻ phân cách, Border nhẹ |

## 2. Layout & Kích thước

- **Navbar Height**: `56px`
- **Main Content**: `height: calc(100vh - 56px)`
- **Border Radius**: 
    - Card chung: `10px`
    - Auth Card: `15px`
    - Buttons: `8px`
- **Shadow**: `box-shadow: 0 2px 10px rgba(0,0,0,0.05)`

## 3. Typography

- **Phông chữ**: Hệ thống Sans-serif mặc định (`Arial`, `Segoe UI`, `Roboto`)
- **Heading**:
    - `h3` (KPI Dashboard): `1.8rem`, Bold
    - `h5` (Card Title): `1.1rem`, Bold
    - `h6` (Sidebar Title): `Default Bootstrap`, Bold

## 4. Thành phần giao diện (UI Components)

- **KPI Cards (Dashboard)**:
    - Có `border-left` dày `5px` theo màu trạng thái.
    - Sử dụng `info-box` class.
- **Vehicle Item (Sidebar)**:
    - `padding: 12px 15px`.
    - `hover`: background `#f0f7ff`.
    - `active`: background `#e2efff` + `border-left: 4px solid #007bff`.
- **Status Dot**:
    - Kích thước: `10px x 10px`, `border-radius: 50%`.

## 5. Thư viện Icon & Thông báo

- **Icon**: FontAwesome 6 (Sử dụng các class `fas`, `far`).
- **Thông báo**:
    - Toastr (Cho thông báo nhanh).
    - SweetAlert2 (Cho xác nhận hành động quan trọng).
    - CSS Animations (Blink effects cho vi phạm).
