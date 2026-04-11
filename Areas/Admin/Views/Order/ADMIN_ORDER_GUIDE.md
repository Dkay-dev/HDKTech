# Quản lý Đơn Hàng - Tài liệu Tổng Quan

## 📋 Mô tả
Đã tạo một hệ thống quản lý đơn hàng hoàn chỉnh cho phần Admin của ứng dụng HDKTech. Hệ thống cung cấp khả năng xem, tìm kiếm, lọc và cập nhật trạng thái đơn hàng.

## 📁 Files Được Tạo/Cập Nhật

### 1. **OrderController.cs** (`Areas/Admin/Controllers/OrderController.cs`)
   - **Index()**: Hiển thị danh sách tất cả đơn hàng với các tính năng:
     - Tìm kiếm theo mã đơn, tên khách hàng, số điện thoại
     - Lọc theo trạng thái (Chờ xác nhận, Đang xử lý, Đang giao, Đã giao, Đã hủy)
     - Sắp xếp theo: Ngày mới nhất, Giá cao/thấp nhất, Tên khách hàng
     - Phân trang với kích thước 20 đơn hàng mỗi trang
     - Hiển thị thống kê hôm nay

   - **Details(id)**: Xem chi tiết một đơn hàng:
     - Thông tin khách hàng
     - Chi tiết sản phẩm trong đơn hàng
     - Thông tin giao hàng
     - Công cụ cập nhật trạng thái

   - **UpdateStatus(orderId, newStatus)**: API endpoint cập nhật trạng thái đơn hàng
     - 0: Chờ xác nhận
     - 1: Đang xử lý
     - 2: Đang giao
     - 3: Đã giao
     - 4: Đã hủy

   - **CancelOrder(orderId)**: Hủy đơn hàng (không thể hủy nếu đã giao)

   - **Export()**: Xuất dữ liệu đơn hàng thành file CSV

### 2. **Order/Index.cshtml** (`Areas/Admin/Views/Order/Index.cshtml`)
   - Danh sách tất cả đơn hàng với layout hiện đại
   - **Quick Stats Cards**:
     - Đơn hàng hôm nay
     - Doanh thu hôm nay
     - Số đơn chờ xác nhận
     - Số đơn đang xử lý
     - Số đơn đã giao
   
   - **Filter & Search Section**:
     - Tìm kiếm
     - Lọc theo trạng thái
     - Sắp xếp kết quả
   
   - **Orders Table**:
     - Mã đơn hàng (link đến chi tiết)
     - Khách hàng
     - Số điện thoại
     - Ngày đặt
     - Tổng tiền
     - Trạng thái (color-coded)
     - Hành động (xem chi tiết, hủy)
   
   - **Pagination**: Điều hướng giữa các trang

### 3. **Order/Details.cshtml** (`Areas/Admin/Views/Order/Details.cshtml`)
   - Trang chi tiết đơn hàng với layout sidebar
   - **Main Content**:
     - Thông tin đơn hàng cơ bản
     - Thông tin giao hàng
     - Bảng chi tiết sản phẩm với hình ảnh
     - Tóm tắt đơn hàng (tiền hàng, phí vận chuyển, tổng cộng)
   
   - **Sidebar**:
     - Thông tin khách hàng
     - Công cụ cập nhật trạng thái (nút cho mỗi trạng thái)
     - Nút hủy đơn hàng
     - Timeline lịch sử (placeholder cho future implementation)

## 🎨 Tính Năng UI/UX
- **Responsive Design**: Hoạt động tốt trên mọi kích thước màn hình
- **Color-coded Status**: Mỗi trạng thái đơn hàng có màu riêng để dễ nhận biết
- **Material Icons**: Sử dụng Material Symbols cho các biểu tượng
- **Tailwind CSS**: Styling hiện đại với Tailwind
- **Interactive Elements**: Các nút hành động có hiệu ứng hover
- **Real-time Updates**: Các hành động cập nhật tức thì mà không cần tải lại trang

## 📊 Thống Kê & Số Liệu
- Tổng số đơn hàng theo trạng thái
- Doanh thu hôm nay
- Số đơn hàng mới hôm nay
- Dữ liệu lọc tự động cập nhật dựa trên các bộ lọc

## 🔐 Bảo Mật
- Tất cả các endpoint được bảo vệ bằng `[Authorize]` attribute
- Chỉ cho phép Admin và Manager truy cập
- CSRF protection thông qua POST requests

## 📝 Hướng Dẫn Sử Dụng

### Xem Danh Sách Đơn Hàng
1. Đi tới: `/admin/order`
2. Sử dụng các bộ lọc để tìm kiếm đơn hàng mong muốn
3. Nhấp vào mã đơn hàng để xem chi tiết

### Cập Nhật Trạng Thái
1. Vào trang chi tiết đơn hàng
2. Nhấp vào nút trạng thái mong muốn ở sidebar
3. Xác nhận cập nhật

### Hủy Đơn Hàng
1. Nhấp vào biểu tượng X trên danh sách hoặc nút "Hủy đơn hàng" trên chi tiết
2. Xác nhận hủy (không thể hủy nếu đã giao)

### Xuất Dữ Liệu
1. Nhấp vào nút "Xuất CSV" ở danh sách
2. File CSV sẽ được tải xuống

## 🔧 API Endpoints
- **GET** `/admin/order` - Danh sách đơn hàng
- **GET** `/admin/order/details/{id}` - Chi tiết đơn hàng
- **POST** `/admin/order/update-status` - Cập nhật trạng thái
- **POST** `/admin/order/cancel` - Hủy đơn hàng
- **GET** `/admin/order/export` - Xuất CSV

## 🚀 Tương Lai
- Thêm lịch sử cập nhật (activity log)
- In hóa đơn/phiếu vận chuyển
- Gửi email thông báo khách hàng khi thay đổi trạng thái
- Ghi chú đơn hàng
- Tích hợp với hệ thống vận chuyển
