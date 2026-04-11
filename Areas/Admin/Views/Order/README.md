# 📦 Hệ Thống Quản Lý Đơn Hàng - HDKTech Admin Panel

> Một hệ thống quản lý đơn hàng toàn diện, hiện đại và dễ sử dụng cho trang Admin của HDKTech

## ✨ Tính Năng Chính

### 📋 Quản Lý Đơn Hàng
- ✅ **Danh sách đơn hàng** với phân trang
- ✅ **Tìm kiếm** theo mã đơn, tên khách, số điện thoại
- ✅ **Lọc** theo trạng thái (5 trạng thái khác nhau)
- ✅ **Sắp xếp** theo ngày, giá tiền, tên khách hàng
- ✅ **Xem chi tiết** đơn hàng đầy đủ

### 🔧 Cập Nhật Trạng Thái
- 🔄 Cập nhật trạng thái đơn hàng (0-4)
- ❌ Hủy đơn hàng (với kiểm tra logic)
- 📊 Thống kê theo trạng thái
- 📅 Thống kê theo ngày

### 📁 Xuất Dữ Liệu
- 📥 Xuất dữ liệu ra CSV
- 🔗 Hỗ trợ lọc trước khi xuất
- 💾 Tự động đặt tên file với timestamp

### 📊 Dashboard Thống Kê
- 📈 Doanh thu hôm nay
- 📦 Số đơn hàng hôm nay
- ⏳ Số đơn chờ xác nhận
- 🔄 Số đơn đang xử lý
- ✓ Số đơn đã giao

## 🚀 Bắt Đầu Nhanh

### 1. Truy Cập Trang Quản Lý Đơn Hàng
```
URL: https://your-domain/admin/order
```

### 2. Xem Danh Sách Đơn Hàng
- Tất cả đơn hàng được hiển thị với thông tin cơ bản
- Sử dụng các bộ lọc để tìm đơn hàng cần thiết

### 3. Cập Nhật Trạng Thái
- Click vào mã đơn hàng để xem chi tiết
- Chọn trạng thái mới từ sidebar
- Hệ thống tự động cập nhật

### 4. Xuất Báo Cáo
- Nhấp nút "Xuất CSV"
- File sẽ được tải xuống

## 📚 Tài Liệu

| File | Mô Tả |
|------|-------|
| [ADMIN_ORDER_GUIDE.md](Areas/Admin/Views/Order/ADMIN_ORDER_GUIDE.md) | Hướng dẫn chi tiết sử dụng |
| [BEST_PRACTICES.md](Areas/Admin/Views/Order/BEST_PRACTICES.md) | Tips, mẹo và best practices |
| [ARCHITECTURE_OVERVIEW.md](Areas/Admin/ARCHITECTURE_OVERVIEW.md) | Kiến trúc hệ thống |
| [INTEGRATION_GUIDE.md](Areas/Admin/INTEGRATION_GUIDE.md) | Hướng dẫn tích hợp |

## 🎨 Giao Diện Người Dùng

### Danh Sách Đơn Hàng
```
┌─────────────────────────────────────┐
│  🏠 Quản lý Đơn Hàng     📥 Export  │
├─────────────────────────────────────┤
│ [Today: 5 orders] [Revenue: 2.5M]   │
├─────────────────────────────────────┤
│ Search: [_______] Status: [All    ] │
│ Sort: [Newest  ] [Search Button]    │
├─────────────────────────────────────┤
│ MãĐH    │ Khách      │ Tiền   │ TT   │
├─────────────────────────────────────┤
│ HK-001  │ John Doe   │ 500K   │ ✓    │
│ HK-002  │ Jane Smith │ 300K   │ ⏳   │
│ ...     │ ...        │ ...    │ ...  │
├─────────────────────────────────────┤
│ [< 1 2 3 >] Showing 1-20 of 156    │
└─────────────────────────────────────┘
```

### Chi Tiết Đơn Hàng
```
┌────────────────────┐ ┌──────────────┐
│  Đơn HK-001 ✓      │ │  Khách Info  │
│  ────────────────  │ │  ─────────── │
│  Thông tin đơn     │ │  John Doe    │
│  - Ngày: 1/1/2024  │ │  john@ex.com │
│  - Tổng: 500,000đ  │ │  0123456789  │
│                    │ │              │
│  Thông tin giao    │ │  Status ⏳    │
│  - Người: John Doe │ │  [Chờ      ] │
│  - Địa chỉ: 123... │ │  [Xử lý    ] │
│  - ĐT: 0123456789  │ │  [Giao     ] │
│                    │ │  [Đã giao  ] │
│  Sản phẩm          │ │  [Hủy đơn  ] │
│  ┌───────────────┐ │ │              │
│  │Laptop │2 │10M│ │ │  Timeline    │
│  │Mouse  │1 │1M │ │ │  (Coming)    │
│  └───────────────┘ │ │              │
└────────────────────┘ └──────────────┘
```

## 🔐 Quyền Truy Cập

### Yêu Cầu
- Vai trò: **Admin** hoặc **Manager**
- Đã đăng nhập vào hệ thống

### Bảo Vệ
- ✅ Authorize attribute trên tất cả endpoints
- ✅ Role-based access control
- ✅ SQL injection prevention (Entity Framework)
- ✅ CSRF protection

## 📂 Cấu Trúc File

```
Areas/Admin/
├── Controllers/
│   └── OrderController.cs           (130 lines)
└── Views/
    └── Order/
        ├── Index.cshtml             (350 lines)
        ├── Details.cshtml           (400 lines)
        ├── ADMIN_ORDER_GUIDE.md
        └── BEST_PRACTICES.md
```

## 🛠 Công Nghệ Sử Dụng

| Công Nghệ | Phiên Bản | Mục Đích |
|-----------|----------|---------|
| .NET | 7 | Framework chính |
| ASP.NET Core | 7 | Web framework |
| Entity Framework Core | 7 | ORM |
| Razor | 7 | Template engine |
| Tailwind CSS | 3 | Styling |
| SQL Server | - | Database |

## 📊 Trạng Thái Đơn Hàng

| Mã | Tên | Mô Tả | Có Thể Hủy |
|----|-----|-------|-----------|
| 0 | Chờ xác nhận | Vừa đặt, chưa xác nhận | ✅ |
| 1 | Đang xử lý | Chuẩn bị hàng | ✅ |
| 2 | Đang giao | Giao cho vận chuyển | ✅ |
| 3 | Đã giao | Khách đã nhận | ❌ |
| 4 | Đã hủy | Bị hủy | ❌ |

## ⚡ Hiệu Suất

- ✅ AsNoTracking() cho read-only queries
- ✅ Phân trang mặc định 20 bản ghi/trang
- ✅ Include() optimization cho FK
- ✅ CSV export streaming
- ✅ Async/await cho I/O operations

## 🔍 API Reference

### GET /admin/order
Lấy danh sách đơn hàng

**Query Parameters:**
```
pageNumber=1          // Trang (mặc định 1)
pageSize=20           // Số bản ghi/trang
searchTerm=           // Tìm kiếm
statusFilter=-1       // Lọc trạng thái (-1 = tất cả)
sortBy=date           // Sắp xếp: date, amount_high, amount_low, customer
```

**Response:**
```json
{
  "orders": [...],
  "totalCount": 156,
  "pageNumber": 1,
  "pageSize": 20,
  "totalPages": 8
}
```

### GET /admin/order/details/{id}
Lấy chi tiết đơn hàng

**Response:**
```json
{
  "maDonHang": 1,
  "maDonHangChuoi": "HK-001",
  "tenNguoiNhan": "John Doe",
  "tongTien": 500000,
  "chiTietDonHangs": [...]
}
```

### POST /admin/order/update-status
Cập nhật trạng thái

**Body:**
```json
{
  "orderId": 1,
  "newStatus": 1
}
```

**Response:**
```json
{
  "success": true,
  "message": "Cập nhật thành công"
}
```

### POST /admin/order/cancel
Hủy đơn hàng

**Body:**
```json
{
  "orderId": 1
}
```

## 🐛 Troubleshooting

### Không thấy đơn hàng nào
- ✅ Kiểm tra role người dùng (phải là Admin/Manager)
- ✅ Kiểm tra database có dữ liệu không
- ✅ Xóa filter và tìm kiếm để reset

### Cập nhật trạng thái không hoạt động
- ✅ Kiểm tra console (F12) có lỗi không
- ✅ Kiểm tra network tab, request gửi đi không
- ✅ Kiểm tra server logs
- ✅ Đảm bảo user có quyền Admin/Manager

### CSV export không hoạt động
- ✅ Kiểm tra browser download settings
- ✅ Kiểm tra server disk space
- ✅ Kiểm tra encoding (UTF-8)

## 📈 Tương Lai

### Tính năng sắp tới
- [ ] Activity log / Lịch sử thay đổi
- [ ] Email notifications
- [ ] SMS alerts
- [ ] Order notes
- [ ] Print invoice
- [ ] Shipping integration
- [ ] Refund management
- [ ] Bulk operations
- [ ] Advanced reporting
- [ ] Dashboard analytics

## 👨‍💻 Development

### Thiết lập môi trường
```bash
# Clone repository
git clone https://github.com/tuilabodayz/HDKTech.git

# Restore packages
dotnet restore

# Update database
dotnet ef database update

# Run application
dotnet run
```

### Build
```bash
dotnet build
```

### Test
```bash
# Chạy tất cả tests
dotnet test

# Chạy specific test
dotnet test --filter "MethodName"
```

## 📝 Changelog

### v1.0.0 - 2024-01-XX
- ✅ Danh sách đơn hàng với phân trang
- ✅ Tìm kiếm và lọc
- ✅ Xem chi tiết đơn hàng
- ✅ Cập nhật trạng thái
- ✅ Hủy đơn hàng
- ✅ Xuất CSV
- ✅ Thống kê
- ✅ Responsive design
- ✅ Documentation

## 📄 License

Dự án này thuộc về HDKTech. Tất cả quyền được bảo lưu.

## 📞 Support

Có câu hỏi? Bạn có thể:
1. Xem tài liệu trong folder `Areas/Admin/`
2. Kiểm tra file `BEST_PRACTICES.md` để xem tips
3. Tham khảo `ARCHITECTURE_OVERVIEW.md` để hiểu kiến trúc

## ✍️ Tác Giả

Được phát triển cho **HDKTech** - Hệ thống e-commerce bán máy tính.

---

**Cập nhật lần cuối**: 2024
**Phiên bản**: 1.0.0
**Status**: ✅ Production Ready
