# 📐 Kiến Trúc & Cấu Trúc HTML

## 📂 Project Structure

```
HDKTech/
├── Areas/
│   └── Admin/
│       ├── Controllers/
│       │   └── OrderController.cs          ← Xử lý logic
│       └── Views/
│           ├── Order/
│           │   ├── Index.cshtml            ← Danh sách đơn hàng
│           │   ├── Details.cshtml          ← Chi tiết đơn hàng
│           │   ├── ADMIN_ORDER_GUIDE.md    ← Hướng dẫn
│           │   └── BEST_PRACTICES.md       ← Tips & tricks
│           └── Shared/
│               └── _AdminLayout.cshtml     ← Layout chung
├── Models/
│   ├── DonHang.cs                          ← Model đơn hàng
│   ├── ChiTietDonHang.cs                   ← Model chi tiết
│   ├── NguoiDung.cs                        ← Model khách hàng
│   ├── SanPham.cs                          ← Model sản phẩm
│   └── HinhAnh.cs                          ← Model hình ảnh
├── Data/
│   └── HDKTechContext.cs                   ← Database context
└── ...
```

## 🎨 Component Structure

### OrderController
```
┌─────────────────────────────────────┐
│      OrderController                │
├─────────────────────────────────────┤
│ + Index()          → GET /order     │
│ + Details()        → GET /order/{id}│
│ + UpdateStatus()   → POST /order    │
│ + CancelOrder()    → POST /order    │
│ + Export()         → GET /order     │
└─────────────────────────────────────┘
```

### Index View Layout
```
┌─────────────────────────────────────────────┐
│            Header Section                   │
│  "Quản lý Đơn Hàng" + Export Button        │
├─────────────────────────────────────────────┤
│         Quick Stats (5 Cards)               │
│  [Today Orders] [Today Revenue]             │
│  [Pending] [Processing] [Delivered]         │
├─────────────────────────────────────────────┤
│       Filter & Search Section               │
│  [Search] [Status Filter] [Sort] [Button]   │
├─────────────────────────────────────────────┤
│          Orders Table                       │
│  ┌─────────────────────────────────────────┐│
│  │ MãĐH │ Khách │ SĐT │ Ngày │ Tiền │ TT  ││
│  │─────────────────────────────────────────││
│  │ HK-1 │ John  │ 123 │ 1/1  │ 500k │ ✓   ││
│  │ HK-2 │ Jane  │ 456 │ 2/1  │ 300k │ ⏳  ││
│  │ ...  │ ...   │ ... │ ...  │ ...  │ ... ││
│  └─────────────────────────────────────────┘│
├─────────────────────────────────────────────┤
│          Pagination                        │
│  [< Prev] [1] [2] [3] [Next >]              │
└─────────────────────────────────────────────┘
```

### Details View Layout
```
┌─────────────────────────────────────────────────┐
│  Header: "Đơn Hàng HK-9281" [Status Badge]     │
└─────────────────────────────────────────────────┘
┌──────────────────────────┐  ┌─────────────────┐
│     Main Content         │  │     Sidebar     │
├──────────────────────────┤  ├─────────────────┤
│                          │  │  Customer Info  │
│  Order Info Card         │  │  ┌──────────────│
│  ├─ Mã: HK-9281          │  │  │  Avatar      │
│  ├─ Ngày: 1/1/2024       │  │  │  Name        │
│  └─ Tổng: 500,000đ       │  │  │  Email       │
│                          │  │  │  Phone       │
│  Shipping Info Card      │  │  └──────────────│
│  ├─ Người nhận: John     │  │                 │
│  ├─ ĐT: 0123456789       │  │  Status Update  │
│  ├─ Địa chỉ: 123 Main    │  │  Buttons        │
│  └─ Phí: 50,000đ         │  │  ┌──────────────│
│                          │  │  │ [Chờ xác nhận]│
│  Products Table          │  │  │ [Đang xử lý] │
│  ┌──────────────────────┐│  │  │ [Đang giao]  │
│  │Sản phẩm│Giá│SL│Total││  │  │ [Đã giao]    │
│  │────────────────────────││  │  │ [Hủy đơn]    │
│  │Laptop  │10M│1│ 10M   ││  │  └──────────────│
│  │Mouse   │1M │2│ 2M    ││  │                 │
│  └──────────────────────┘│  │  Timeline       │
│                          │  │  (Activity Log) │
│  Order Summary           │  │                 │
│  ├─ Tổng sản phẩm: 3     │  │                 │
│  ├─ Tiền hàng: 450k      │  │                 │
│  ├─ Phí vận chuyển: 50k  │  │                 │
│  └─ Tổng cộng: 500k      │  │                 │
└──────────────────────────┘  └─────────────────┘
```

## 🔌 API Endpoints

```
ORDER MANAGEMENT
├── GET /admin/order
│   ├── Query params: pageNumber, pageSize, searchTerm, statusFilter, sortBy
│   └── Returns: List of orders with pagination
│
├── GET /admin/order/details/{id}
│   └── Returns: Single order with full details
│
├── POST /admin/order/update-status
│   ├── Body: { orderId, newStatus }
│   └── Returns: { success, message }
│
├── POST /admin/order/cancel
│   ├── Body: { orderId }
│   └── Returns: { success, message }
│
└── GET /admin/order/export
    ├── Query params: searchTerm, statusFilter
    └── Returns: CSV file download
```

## 📊 Data Flow

```
┌─────────┐
│ Browser │
└────┬────┘
     │ HTTP Request
     ↓
┌──────────────────────┐
│ OrderController      │
│ (ASP.NET Core)       │
└────┬─────────────────┘
     │
     │ LINQ Query
     ↓
┌──────────────────────┐
│ HDKTechContext       │
│ (Entity Framework)   │
└────┬─────────────────┘
     │
     │ SQL Query
     ↓
┌──────────────────────┐
│ SQL Server Database  │
└────┬─────────────────┘
     │
     │ Data (Rows)
     ↓
┌──────────────────────┐
│ DonHang Objects      │
│ (Mapped to C# Models)│
└────┬─────────────────┘
     │
     │ Pass to View
     ↓
┌──────────────────────┐
│ Razor View           │
│ (Index.cshtml)       │
└────┬─────────────────┘
     │
     │ HTML + CSS + JS
     ↓
┌──────────────────────┐
│ Browser              │
│ (Rendered HTML)      │
└──────────────────────┘
```

## 🎯 State Management

### Order Status Flow
```
Create Order
    │
    ├─→ 0 (Chờ xác nhận)
    │      └─→ Admin confirms
    │
    ├─→ 1 (Đang xử lý)
    │      └─→ Ready to ship
    │
    ├─→ 2 (Đang giao)
    │      └─→ Customer received
    │
    ├─→ 3 (Đã giao)
    │      └─→ Final state
    │
    └─→ 4 (Đã hủy)
           └─→ Final state

At any point (except 3 & 4), can cancel → Status 4
```

## 💾 Database Schema (Relevant Tables)

```sql
-- Orders Table
CREATE TABLE DonHang (
    MaDonHang INT PRIMARY KEY,
    MaDonHangChuoi NVARCHAR(50),           -- HK-001, HK-002, etc
    MaNguoiDung NVARCHAR(450),             -- FK to NguoiDung
    TongTien DECIMAL(18,2),                -- Total amount
    PhiVanChuyen DECIMAL(18,2),            -- Shipping fee
    TenNguoiNhan NVARCHAR(100),            -- Recipient name
    SoDienThoaiNhan NVARCHAR(20),          -- Phone
    DiaChiGiaoHang NVARCHAR(500),          -- Address
    TrangThaiDonHang INT,                  -- 0-4 status
    NgayDatHang DATETIME                   -- Order date
);

-- Order Details Table
CREATE TABLE ChiTietDonHang (
    MaChiTietDonHang INT PRIMARY KEY,
    MaDonHang INT,                         -- FK to DonHang
    MaSanPham INT,                         -- FK to SanPham
    SoLuong INT,                           -- Quantity
    GiaBanLucMua DECIMAL(18,2)             -- Price at purchase
);

-- User Table
CREATE TABLE NguoiDung (
    Id NVARCHAR(450) PRIMARY KEY,
    HoTen NVARCHAR(100),
    Email NVARCHAR(256),
    PhoneNumber NVARCHAR(20),
    NgayTao DATETIME
);

-- Product Table
CREATE TABLE SanPham (
    MaSanPham INT PRIMARY KEY,
    TenSanPham NVARCHAR(200),
    ...
);

-- Product Images Table
CREATE TABLE HinhAnh (
    MaHinhAnh INT PRIMARY KEY,
    MaSanPham INT,                         -- FK to SanPham
    Url NVARCHAR(300),                     -- Image URL
    IsDefault BIT,
    NgayTao DATETIME
);
```

## 🔄 Request-Response Cycle

### Viewing Order List
```
1. User navigates to /admin/order
2. OrderController.Index() is called
3. Query DonHang table with filters/sorting/pagination
4. Include related NguoiDung data
5. Calculate summary statistics
6. Pass data to ViewBag
7. Render Index.cshtml with data
8. Browser displays rendered HTML
```

### Updating Status
```
1. User clicks status button in Details.cshtml
2. JavaScript sends POST to /admin/order/update-status
3. OrderController.UpdateStatus() receives request
4. Validate orderId and newStatus
5. Find DonHang in database
6. Update TrangThaiDonHang field
7. SaveChanges() to commit
8. Return JSON { success: true, message: "..." }
9. JavaScript reloads page on success
```

## 🎭 View Context

### Index.cshtml receives from ViewBag:
```csharp
ViewBag.Orders              // List<DonHang>
ViewBag.TotalCount          // int
ViewBag.PageNumber          // int
ViewBag.PageSize            // int
ViewBag.TotalPages          // int
ViewBag.SearchTerm          // string
ViewBag.StatusFilter        // int
ViewBag.SortBy              // string
ViewBag.PendingCount        // int
ViewBag.ProcessingCount     // int
ViewBag.ShippingCount       // int
ViewBag.DeliveredCount      // int
ViewBag.CancelledCount      // int
ViewBag.TodayOrderCount     // int
ViewBag.TodayRevenue        // decimal
```

### Details.cshtml receives from ViewBag:
```csharp
ViewBag.Order               // DonHang (with includes)
```

## 🚦 HTTP Methods Used

- **GET** `/admin/order` - Retrieve list
- **GET** `/admin/order/details/{id}` - Retrieve single
- **POST** `/admin/order/update-status` - Update state
- **POST** `/admin/order/cancel` - Update to cancelled
- **GET** `/admin/order/export` - Download CSV

## 📱 Browser DevTools Tips

### Network Tab
- Monitor XHR requests for status updates
- Check response body for error messages
- Verify request payloads are correct

### Console Tab
- JavaScript errors appear here
- Network request errors logged
- Debugging with console.log()

### Elements Tab
- Inspect HTML structure
- Check CSS classes applied
- Verify data attributes

---

**Tài liệu này giúp bạn hiểu toàn bộ kiến trúc và cách hoạt động của hệ thống quản lý đơn hàng.**
