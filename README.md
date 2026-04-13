# HDKTech - Laptop E-Commerce Website

Dự án website bán máy tính xách tay được xây dựng bằng framework **ASP.NET Core MVC**. Dự án tập trung vào việc áp dụng các Design Pattern chuyên nghiệp và quy trình phân tích thiết kế hệ thống chuẩn.

## 🚀 Công nghệ sử dụng
* **Backend:** ASP.NET Core 8.0 (hoặc phiên bản bạn đang dùng)
* **Database:** SQL Server
* **Frontend:** HTML, CSS, JavaScript, Tailwind CSS & Bootstrap (Admin Panel)
* **ORM:** Entity Framework Core
* **Design Pattern:** Repository Pattern, Unit of Work

## 🛠️ Tính năng chính
### Đối với Khách hàng (Customer)
* Xem danh sách sản phẩm (Laptop) theo danh mục và thương hiệu.
* Tìm kiếm sản phẩm thông minh.
* Quản lý giỏ hàng và đặt hàng.
* Theo dõi trạng thái đơn hàng.

### Đối với Quản trị viên (Admin)
* Quản lý Sản phẩm (CRUD): Tên, giá, cấu hình, hình ảnh.
* Quản lý Danh mục & Thương hiệu.
* Quản lý Đơn hàng và Phân quyền người dùng.
* Thống kê doanh thu (Dashboard).

## 📂 Cấu trúc dự án
* `Areas/Admin`: Khu vực quản trị hệ thống sử dụng Bootstrap.
* `Models`: Chứa các thực thể (Entities) của hệ thống.
* `Repositories`: Triển khai Repository Pattern để quản lý logic truy vấn dữ liệu.
* `Controllers`: Xử lý luồng dữ liệu và điều hướng.
* `Data`: Quản lý Database Context và Migrations.

## ⚙️ Cài đặt
1. Clone dự án:
   ```bash
   git clone [https://github.com/Dkay-dev/HDKTech.git](https://github.com/Dkay-dev/HDKTech.git)
