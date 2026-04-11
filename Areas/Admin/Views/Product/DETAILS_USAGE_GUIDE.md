# 📋 Hướng Dẫn Chi Tiết Form Chỉnh Sửa Sản Phẩm

## 🎯 Cách Truy Cập Form Details

### Từ Danh Sách Sản Phẩm (Index.cshtml)
```
1. Vào Admin → Products
2. Bảng danh sách sản phẩm hiển thị
3. Tìm sản phẩm muốn sửa
4. Bấm icon Edit (ngoài cùng bên phải)
5. → Mở form Details.cshtml
```

### URL Structure
```
Create Mode (Tạo sản phẩm mới):
   /admin/product/create

Edit Mode (Chỉnh sửa sản phẩm):
   /admin/product/edit/123
   (123 = MaSanPham của sản phẩm)
```

---

## 🎨 Giao Diện Form Details.cshtml

### 1️⃣ **Header Section** (Phần Đầu)
```
┌─────────────────────────────────────────────┐
│  Chỉnh sửa sản phẩm          [← Quay lại]  │
│  Cập nhật: ASUS ROG Strix G16              │
└─────────────────────────────────────────────┘
```

**Hiển thị:**
- Tiêu đề: "Chỉnh sửa sản phẩm" (nếu edit) hoặc "Tạo sản phẩm mới" (nếu create)
- Tên sản phẩm hiện tại (nếu edit)
- Nút "Quay lại" để về danh sách

---

### 2️⃣ **SECTION 1: Thông Tin Cơ Bản**
```
┌─────────────────────────────────────────────┐
│  ℹ️  THÔNG TIN CƠ BẢN                      │
├─────────────────────────────────────────────┤
│                                             │
│  Tên sản phẩm *                   Danh mục *│
│  [ASUS ROG Strix G16]   [-- Chọn danh mục--]
│                                             │
│  Thương hiệu *                    Giá bán *│
│  [-- Chọn thương hiệu--]          [₫ 50000000]
│                                             │
│  Giá niêm yết                   Trạng thái *│
│  [₫ 55000000]                    [✓ Hoạt động]
│  Để trống nếu = giá bán                     │
│                                             │
└─────────────────────────────────────────────┘
```

**Các trường:**
| Trường | Bắt buộc | Ghi chú |
|--------|----------|--------|
| Tên sản phẩm | ✓ | VD: ASUS ROG Strix G16 |
| Danh mục | ✓ | Dropdown từ DB (Laptop, Components...) |
| Thương hiệu | ✓ | Dropdown từ DB (ASUS, Intel...) |
| Giá bán | ✓ | Số tiền (₫), tối thiểu > 0 |
| Giá niêm yết | ✗ | Giá gốc (tùy chọn) |
| Trạng thái | ✓ | ✓ Hoạt động / ✗ Ngừng bán |

**Ví dụ điền:**
```
Tên sản phẩm:     ASUS ROG Strix G16
Danh mục:         Laptop
Thương hiệu:      ASUS
Giá bán:          50000000
Giá niêm yết:     55000000
Trạng thái:       ✓ Hoạt động
```

---

### 3️⃣ **SECTION 2: Mô Tả & Thông Tin Chi Tiết**
```
┌─────────────────────────────────────────────┐
│  📄 MÔ TẢ & THÔNG TIN CHI TIẾT            │
├─────────────────────────────────────────────┤
│                                             │
│  Mô tả sản phẩm *                           │
│  ┌─────────────────────────────────────┐   │
│  │ Đây là chiếc laptop gaming cao cấp │   │
│  │ với hiệu năng mạnh mẽ, màn hình    │   │
│  │ 4K 120Hz, thiết kế gọn nhẹ...      │   │
│  │                                     │   │
│  │ Hỗ trợ HTML: <h4>, <ul>, <li>,     │   │
│  │ <strong>, <em>                      │   │
│  └─────────────────────────────────────┘   │
│                                             │
│  Thông số kỹ thuật                          │
│  ┌─────────────────────────────────────┐   │
│  │ Intel Core i9 | RTX 4090            │   │
│  │ RAM 32GB | SSD 1TB | 16" 4K 120Hz  │   │
│  │                                     │   │
│  │ Ngăn cách bằng dấu | (pipe)        │   │
│  └─────────────────────────────────────┘   │
│                                             │
│  Thông tin bảo hành        Khuyến mãi      │
│  [24 tháng chính hãng]    [Giảm 15%|Tặng balo]
│                                             │
└─────────────────────────────────────────────┘
```

**Các trường:**
| Trường | Bắt buộc | Ghi chú |
|--------|----------|--------|
| Mô tả sản phẩm | ✓ | Văn bản chi tiết, hỗ trợ HTML |
| Thông số kỹ thuật | ✗ | Danh sách, ngăn cách bằng \| |
| Thông tin bảo hành | ✗ | Ví dụ: 24 tháng, bảo hành chính hãng |
| Khuyến mãi/Quà tặng | ✗ | Ví dụ: Giảm 15%, Tặng balo |

**Ví dụ điền:**
```
Mô tả sản phẩm:
  Laptop gaming cao cấp ASUS ROG Strix G16 với:
  - Bộ xử lý Intel Core i9 Gen 13
  - Card đồ họa RTX 4090 Super
  - Màn hình 4K IPS 120Hz
  - Pin 90Wh - 12 giờ sử dụng

Thông số kỹ thuật:
  Intel Core i9-13900HX | RTX 4090 Super | RAM 32GB DDR5 | SSD 1TB NVMe | 16" 4K 120Hz | Wi-Fi 7 | Port Thunderbolt 4

Thông tin bảo hành:
  24 tháng bảo hành chính hãng ASUS

Khuyến mãi / Quà tặng:
  Giảm giá 10% | Tặng balo ASUS | Vệ sinh máy miễn phí năm đầu
```

---

### 4️⃣ **Action Buttons** (Phần Cuối)
```
┌─────────────────────────────────────────────┐
│  [Hủy]  [Xóa sản phẩm]  [Cập nhật sản phẩm]│
└─────────────────────────────────────────────┘
```

**Các nút:**
- **[Hủy]** - Quay lại danh sách (không lưu)
- **[Xóa sản phẩm]** - Xóa sản phẩm khỏi DB (chỉ hiển thị khi Edit)
- **[Cập nhật/Tạo sản phẩm]** - Lưu thay đổi

---

## 🔴 Validation (Kiểm Tra Lỗi)

Khi bấm **[Cập nhật]**, form sẽ tự động kiểm tra:

### Lỗi 1: Trường bắt buộc để trống
```
❌ Vui lòng nhập tên sản phẩm
```
→ Điền vào field "Tên sản phẩm"

### Lỗi 2: Chưa chọn danh mục
```
❌ Vui lòng chọn danh mục
```
→ Bấm vào dropdown "Danh mục" → chọn một mục

### Lỗi 3: Chưa chọn thương hiệu
```
❌ Vui lòng chọn thương hiệu
```
→ Bấm vào dropdown "Thương hiệu" → chọn một hãng

### Lỗi 4: Giá bán không hợp lệ
```
❌ Vui lòng nhập giá bán lớn hơn 0
```
→ Nhập giá > 0 (ví dụ: 50000000)

### Lỗi 5: Mô tả để trống
```
❌ Vui lòng nhập mô tả sản phẩm
```
→ Viết mô tả ít nhất 10 ký tự

---

## ✅ Sau Khi Lưu Thành Công

```
┌──────────────────────────────────────────────┐
│ ✓ Thành công! Cập nhật sản phẩm thành công  │
└──────────────────────────────────────────────┘
    ↓
Redirect về danh sách sản phẩm (Index.cshtml)
Sản phẩm hiển thị với thông tin mới
```

---

## 📱 Ví Dụ Điền Form Hoàn Chỉnh

### **CREATE MODE** (Tạo sản phẩm mới)

```
🔵 Tên sản phẩm
   → Dell XPS 15 (9530)

🔵 Danh mục
   → Laptop

🔵 Thương hiệu
   → Dell

🔵 Giá bán
   → 45000000

⚪ Giá niêm yết
   → 48000000

🔵 Trạng thái
   → ✓ Hoạt động

🔵 Mô tả sản phẩm
   → Dell XPS 15 là laptop cao cấp dành cho lập trình viên 
     và designer. Được trang bị:
     - Bộ xử lý Intel Core i7/i9
     - RAM 16GB/32GB DDR5
     - SSD 512GB/1TB NVMe
     - Màn hình 4K OLED 120Hz
     - Webcam FHD 1080p
     - Cổng Thunderbolt 4

⚪ Thông số kỹ thuật
   → Intel Core i7-13700H | RTX 4050 | RAM 16GB | SSD 512GB | 15.6" 4K OLED

⚪ Thông tin bảo hành
   → 12 tháng bảo hành Dell Care Plus

⚪ Khuyến mãi
   → Giảm 5% | Tặng chuột wireless | Bảo hiểm màn hình 1 năm
```

↓ Bấm [Tạo sản phẩm] ↓

```
✅ Thành công! Sản phẩm đã được tạo
→ Quay về danh sách, sản phẩm Dell XPS 15 hiển thị
```

---

### **EDIT MODE** (Chỉnh sửa sản phẩm)

```
URL: /admin/product/edit/123

(Form tự động điền sẵn dữ liệu cũ)

🔵 Tên sản phẩm
   → ASUS ROG Strix G16 (đã có sẵn)

🔵 Danh mục
   → Laptop (đã chọn sẵn)

... (các trường khác)

Muốn sửa giá?
   Giá bán: 50000000 → 48000000

Muốn sửa mô tả?
   (Xóa text cũ, viết text mới)

Bấm [Cập nhật sản phẩm]
```

↓ Bấm [Cập nhật sản phẩm] ↓

```
✅ Thành công! Cập nhật sản phẩm thành công
→ Quay về danh sách, sản phẩm hiển thị giá mới
```

---

## 🗑️ Xóa Sản Phẩm

```
(Chỉ khi ở EDIT MODE)

Bấm nút [Xóa sản phẩm] (màu đỏ)
        ↓
Confirm dialog: "Xóa sản phẩm này?"
        ↓
Bấm OK → Xóa khỏi DB
        ↓
✅ Quay về danh sách (sản phẩm biến mất)
```

---

## 💡 Tips & Tricks

### ✨ Để Form Trông Đẹp
- Tên sản phẩm: **Rõ ràng, chứa model/version**
  ✓ "ASUS ROG Strix G16 (2024)"
  ✗ "Laptop tốt"

- Mô tả: **Dàn dòng cho dễ đọc**
  ```
  - Bộ xử lý mạnh mẽ
  - Màn hình sắc nét
  - Pin lâu lâu
  ```

- Thông số: **Dùng pipe | để tách**
  ✓ "Intel i9 | RTX 4090 | RAM 32GB | SSD 1TB"
  ✗ "Intel i9 RTX 4090 RAM 32GB SSD 1TB"

### 🎯 Lưu Dữ Liệu Đúng
- Giá: Nhập số nguyên (không có dấu phẩy)
  ✓ `50000000`
  ✗ `50.000.000`

- Trạng thái: Chọn rõ Hoạt động hay Ngừng bán
- Danh mục: Luôn chọn chính xác (ảnh hưởng đến hiển thị shop)

---

## 📞 Nếu Có Lỗi?

**Lỗi: "Server error 500"**
- Kiểm tra tất cả trường bắt buộc đã điền
- Kiểm tra format giá (số nguyên dương)

**Lỗi: "Danh mục/Thương hiệu không hiển thị"**
- Thêm danh mục/thương hiệu vào database trước
- Quay lại form, refresh trang

**Lỗi: "Không thể xóa sản phẩm"**
- Sản phẩm có thể đang được sử dụng (Order, Cart)
- Thay vì xóa, chọn "Ngừng bán"

---

**Version:** 1.0
**Ngày cập nhật:** 2024
**Trạng thái:** ✅ Ready to Use
