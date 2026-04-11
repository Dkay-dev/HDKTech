# Hướng Dẫn Tích Hợp Quản lý Đơn Hàng vào Admin Navigation

## Cập Nhật Admin Layout

Nếu bạn đang sử dụng một sidebar hoặc navigation menu trong `_AdminLayout.cshtml`, 
hãy thêm link sau đến mục "Orders" hoặc "Đơn Hàng":

```html
<a href="@Url.Action("Index", "Order", new { area = "Admin" })" class="flex items-center gap-3 px-4 py-3 text-zinc-600 dark:text-zinc-400 hover:bg-slate-200 dark:hover:bg-zinc-800 transition-all">
    <span class="material-symbols-outlined">shopping_cart</span>
    <span>Đơn Hàng</span>
</a>
```

## Cấu Trúc Thư Mục

```
Areas/
└── Admin/
    ├── Controllers/
    │   └── OrderController.cs
    └── Views/
        └── Order/
            ├── Index.cshtml (Danh sách)
            ├── Details.cshtml (Chi tiết)
            └── ADMIN_ORDER_GUIDE.md (Tài liệu)
```

## Kiểm Tra Quyền Truy Cập

Đảm bảo người dùng có vai trò "Admin" hoặc "Manager" được gán trong database Identity.

Cách kiểm tra:
```csharp
var user = await userManager.GetUserAsync(User);
var roles = await userManager.GetRolesAsync(user);
// Phải chứa "Admin" hoặc "Manager"
```

## Tùy Chỉnh

### Thay Đổi Kích Thước Phân Trang
Tại dòng trong `OrderController.Index()`:
```csharp
int pageSize = 20  // Thay 20 thành số mong muốn
```

### Thay Đổi Màu Sắc Trạng Thái
Tại file `Index.cshtml` và `Details.cshtml`, tìm các class color mapping:
```csharp
var statusClass = order.TrangThaiDonHang switch
{
    0 => "bg-error-container/30 text-on-error-container",  // Chờ xác nhận
    1 => "bg-surface-container text-on-surface-variant",   // Đang xử lý
    // ...
};
```

### Thêm Cột Mới Vào Bảng
1. Tại `OrderController.cs`, thêm dữ liệu cần thiết vào query
2. Tại `Index.cshtml`, thêm `<th>` và `<td>` mới vào bảng

## Các Tính Năng Có Thể Mở Rộng

### 1. Activity Log
Thêm bảng `OrderActivityLog` để theo dõi mỗi thay đổi:
```csharp
public class OrderActivityLog
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public string Action { get; set; }
    public string OldValue { get; set; }
    public string NewValue { get; set; }
    public DateTime ChangedAt { get; set; }
    public string ChangedBy { get; set; }
}
```

### 2. Gửi Email Thông Báo
Thêm vào `UpdateStatus()`:
```csharp
// Gửi email thông báo khách hàng
await emailService.SendOrderStatusChangeEmail(order);
```

### 3. In Phiếu
Thêm action `PrintOrder()`:
```csharp
[HttpGet]
[Route("print/{id}")]
public async Task<IActionResult> PrintOrder(int id)
{
    // Trả về PDF hoặc trang in
}
```

### 4. Ghi Chú Đơn Hàng
Thêm trường `Notes` vào model `DonHang`:
```csharp
public string Notes { get; set; }
```

## Kiểm Tra Chức Năng

1. **Danh sách đơn hàng**: `/admin/order`
   - ✅ Hiển thị tất cả đơn hàng
   - ✅ Tìm kiếm hoạt động
   - ✅ Lọc theo trạng thái
   - ✅ Sắp xếp hoạt động
   - ✅ Phân trang hoạt động

2. **Chi tiết đơn hàng**: `/admin/order/details/1`
   - ✅ Hiển thị đủ thông tin
   - ✅ Hiển thị sản phẩm với hình ảnh
   - ✅ Cập nhật trạng thái hoạt động
   - ✅ Hủy đơn hàng hoạt động

3. **Xuất dữ liệu**: 
   - ✅ Nút xuất CSV hoạt động
   - ✅ File CSV được tải xuống

## Lưu Ý Quan Trọng

- ⚠️ Đảm bảo `HDKTechContext` được inject đúng cách trong Startup
- ⚠️ Các migration phải được áp dụng để tạo bảng nếu chưa tồn tại
- ⚠️ Chỉ user có vai trò Admin/Manager mới truy cập được
- ⚠️ Kiểm tra quyền truy cập database trước khi deploy lên production
