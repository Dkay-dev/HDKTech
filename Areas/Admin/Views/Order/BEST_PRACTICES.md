# 💡 Mẹo & Thực Tiễn Tốt Nhất

## 🎯 Hiểu Rõ Các Trạng Thái Đơn Hàng

| Mã | Tên | Mô Tả | Có Thể Hủy? |
|----|-----|-------|-----------|
| 0 | Chờ xác nhận | Đơn hàng vừa được tạo, chưa xác nhận | ✅ Có |
| 1 | Đang xử lý | Đang chuẩn bị đơn hàng | ✅ Có |
| 2 | Đang giao | Đã giao cho đơn vị vận chuyển | ✅ Có |
| 3 | Đã giao | Khách hàng đã nhận | ❌ Không |
| 4 | Đã hủy | Đơn hàng đã bị hủy | ❌ Không |

## 🚀 Quy Trình Xử Lý Đơn Hàng Chuẩn

```
1. Khách đặt hàng → Trạng thái: Chờ xác nhận (0)
                    ↓
2. Admin xác nhận → Trạng thái: Đang xử lý (1)
                    ↓
3. Chuẩn bị xong → Trạng thái: Đang giao (2)
                    ↓
4. Khách nhận → Trạng thái: Đã giao (3)
```

## 💻 Tips Khi Sử Dụng Hệ Thống

### Tìm Kiếm Hiệu Quả
- **Tìm theo mã đơn hàng**: Nhập "HK-" hoặc mã đầy đủ
- **Tìm theo khách hàng**: Nhập tên hoặc số điện thoại
- **Kết hợp bộ lọc**: Lọc trạng thái + tìm kiếm = kết quả chính xác

### Sắp Xếp Thông Minh
- **"Mới nhất"**: Để xem đơn hàng vừa được đặt
- **"Giá cao nhất"**: Để ưu tiên xử lý đơn hàng giá trị lớn
- **"Giá thấp nhất"**: Để tìm đơn hàng nhỏ để xử lý nhanh
- **"Tên khách"**: Để tìm đơn hàng của một khách cụ thể

### Xuất Dữ Liệu
- Xuất CSV để dùng với Excel hoặc các công cụ khác
- Có thể lọc trước khi xuất để chỉ lấy dữ liệu cần thiết
- File được lưu với timestamp, không lo mất dữ liệu cũ

## ⚡ Tối Ưu Hiệu Suất

### Database Queries
```csharp
// ✅ TỐT: Sử dụng AsNoTracking() cho dữ liệu chỉ đọc
var orders = await _context.DonHangs
    .AsNoTracking()
    .Include(o => o.NguoiDung)
    .ToListAsync();

// ❌ TẤT: Không sử dụng Include/Join quá lạm dụng
```

### Phân Trang
```csharp
// ✅ TỐT: Luôn sử dụng phân trang
var orders = await query
    .Skip((pageNumber - 1) * pageSize)
    .Take(pageSize)
    .ToListAsync();

// ❌ TẤT: Tải toàn bộ 10,000+ dơn hàng
```

## 🔒 Bảo Mật

### Kiểm Tra Quyền
```csharp
// Luôn kiểm tra quyền trước khi cập nhật
[Authorize(Roles = "Admin,Manager")]
public async Task<IActionResult> UpdateStatus(int orderId, int newStatus)
{
    // Chỉ admin/manager mới có quyền
}
```

### Validate Dữ Liệu
```csharp
// ✅ TỐT: Validate trạng thái hợp lệ
if (newStatus < 0 || newStatus > 4)
    return Json(new { success = false });

// ❌ TẤT: Không validate dữ liệu
```

### SQL Injection Prevention
```csharp
// ✅ TỐT: Entity Framework tự động chống SQL injection
query = query.Where(o => o.MaDonHangChuoi.Contains(searchTerm));

// ❌ TẤT: String concatenation (KHÔNG BAO GIỜ LÀM ĐIỀU NÀY)
var sql = $"SELECT * FROM DonHang WHERE MaDonHang = '{orderId}'";
```

## 📱 Responsive Design Notes

- **Desktop (1920px+)**: Hiển thị 3-4 cột thống kê
- **Tablet (768-1024px)**: Hiển thị 2 cột, scroll ngang bảng
- **Mobile (< 768px)**: Hiển thị 1 cột, bảng stack nhưng vẫn xem được

## 🐛 Debugging Tips

### Kiểm Tra Logs
```csharp
// Lỗi sẽ được ghi vào _logger
_logger.LogError(ex, "Error loading orders");
_logger.LogInformation("Order {OrderId} updated to status {Status}", orderId, status);
```

### Kiểm Tra Network
- Mở DevTools (F12)
- Tab Network để xem request/response
- Tab Console để xem JavaScript errors

### Kiểm Tra Database
```sql
-- Kiểm tra số lượng đơn theo trạng thái
SELECT TrangThaiDonHang, COUNT(*) as Count 
FROM DonHang 
GROUP BY TrangThaiDonHang;
```

## 🎨 Tùy Chỉnh Giao Diện

### Thay Đổi Màu Sắc
Tìm các class Tailwind trong view:
```html
<!-- Chờ xác nhận: Đỏ -->
<span class="bg-error-container text-on-error-container">

<!-- Đang xử lý: Xám -->
<span class="bg-surface-container text-on-surface-variant">

<!-- Đã giao: Xanh dương -->
<span class="bg-tertiary text-on-tertiary">
```

### Thêm Icon Mới
Sử dụng [Material Symbols](https://fonts.google.com/icons):
```html
<span class="material-symbols-outlined">shopping_bag</span>
```

## 📊 Báo Cáo & Phân Tích

### Các Metric Quan Trọng
- **Tổng doanh thu**: Tổng tiền từ tất cả đơn
- **Tỷ lệ hoàn thành**: (Đã giao / Tất cả) × 100%
- **Thời gian xử lý TB**: Ngày giao - Ngày đặt
- **Tỷ lệ hủy**: (Đã hủy / Tất cả) × 100%

### Xuất Báo Cáo CSV
```csharp
// Xuất để phân tích trong Excel
var csv = GenerateOrderReport(orders);
return File(bytes, "text/csv", $"report_{DateTime.Now:yyyyMMdd}.csv");
```

## 🔧 Maintenance

### Backup Dữ Liệu
```bash
# Backup SQL Server
BACKUP DATABASE [HDKTech] TO DISK = 'C:\backup\HDKTech.bak'
```

### Cleanup Dữ Liệu Cũ
```csharp
// Xóa các đơn hàng đã hủy cũ hơn 1 năm (nếu cần)
var oldCancelledOrders = _context.DonHangs
    .Where(o => o.TrangThaiDonHang == 4 && o.NgayDatHang < DateTime.Now.AddYears(-1))
    .ToList();
```

## 🚀 Tiếp Theo

### Tính Năng Có Thể Thêm
- [ ] Gửi email thông báo
- [ ] SMS notifications
- [ ] Lịch sử thay đổi (Activity Log)
- [ ] Ghi chú đơn hàng
- [ ] In phiếu vận chuyển
- [ ] Integraton vận chuyển
- [ ] Hóa đơn điện tử
- [ ] Báo cáo chi tiết
- [ ] Refund management
- [ ] Bulk update orders

## 📞 Hỗ Trợ

Nếu gặp lỗi:
1. Kiểm tra logs trong Event Viewer (Windows) hoặc syslog (Linux)
2. Kiểm tra Network tab trong DevTools
3. Đảm bảo database connection string đúng
4. Kiểm tra quyền truy cập database
5. Kiểm tra migration đã được áp dụng

---

**Cập nhật lần cuối**: 2024
**Phiên bản**: 1.0
