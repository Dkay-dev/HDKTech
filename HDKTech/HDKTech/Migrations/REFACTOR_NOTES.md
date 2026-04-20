# Refactor Notes — Module 1 (Product & Variant) + Module 2 (Inventory)

Tài liệu mô tả tác động của đợt refactor lõi Catalog/Inventory để các dev khác trong team biết cần update gì trước khi chạy `dotnet ef migrations add` và `dotnet ef database update`.

## 1. Thay đổi schema chính

| Entity | Thay đổi |
| --- | --- |
| `Product` | BỎ các cột: `Price`, `ListPrice`, `IsFlashSale`, `FlashSalePrice`, `FlashSaleEndTime`, `DiscountNote`, `WarrantyInfo`. THÊM: `Slug`, `UpdatedAt`. |
| `ProductVariant` **(mới)** | Chứa `Sku`, `Cpu`, `Ram`, `Storage`, `Gpu`, `Screen`, `Color`, `Os`, `Price`, `ListPrice`, `CostPrice`, `IsActive`, `IsDefault`. |
| `Inventory` | Đổi PK từ `ProductId` → `Id` độc lập. Thêm `ProductVariantId`, `WarehouseId`, `ReservedQuantity`, `LowStockThreshold`, `RowVersion`. |
| `StockMovement` **(mới)** | Nhật ký append-only: `InventoryId`, `Quantity (+/-)`, `Reason`, `ReferenceType/Id`, `CreatedAt`, `CreatedBy`. |
| `OrderItem` | Thêm `ProductVariantId`, `ProductNameSnapshot`, `SkuSnapshot`, `SpecSnapshot`, `DiscountAmount`, `LineTotal`, `SerialNumber`. |

## 2. Quan hệ (Fluent API chuẩn)

```
Product 1 ──n ProductVariant 1 ──n Inventory 1 ──n StockMovement
Product 1 ──n ProductImage
Product 1 ──n ProductTag
Product 1 ──n Review
OrderItem ──n 1 Product   (Restrict)
OrderItem ──n 1 Variant   (Restrict)
Order     1 ──1 Invoice
```

- `Product → Variant`: Cascade (xoá Product sẽ xoá Variant theo).
- `OrderItem → Product / Variant`: **Restrict** (không được xoá sản phẩm nếu còn đơn).
- `Inventory → Variant`: Cascade; `Inventory → Product`: Restrict (denormalized).
- `Variant.Sku`: unique toàn hệ thống.
- `Inventory(ProductVariantId, WarehouseId)`: unique composite index.
- `Product.Slug`: filtered unique index (chỉ áp dụng khi != null).

## 3. Data migration plan (chạy trước `database update`)

Do các cột `Price / ListPrice / FlashSale*` bị drop khỏi `Products`, bạn **phải** di chuyển dữ liệu sang `ProductVariants` trước:

```sql
-- Bước 1: tạo ít nhất 1 variant mặc định cho mỗi product cũ
INSERT INTO ProductVariants
  (ProductId, Sku, VariantName, Price, ListPrice, IsActive, IsDefault, CreatedAt)
SELECT
  p.Id,
  CONCAT('LEGACY-', p.Id),
  N'Mặc định',
  p.Price,
  p.ListPrice,
  1, 1, GETDATE()
FROM Products p;

-- Bước 2: di chuyển tồn kho cũ (1 product = 1 variant mặc định)
INSERT INTO Inventories
  (ProductId, ProductVariantId, Quantity, ReservedQuantity, LowStockThreshold, UpdatedAt)
SELECT
  i.ProductId,
  v.Id,
  i.Quantity,
  0, 5, i.UpdatedAt
FROM Inventories_OLD i
JOIN ProductVariants v ON v.ProductId = i.ProductId AND v.IsDefault = 1;

-- Bước 3: update OrderItems cũ về variant mặc định của product
UPDATE oi
SET oi.ProductVariantId = v.Id,
    oi.ProductNameSnapshot = p.Name,
    oi.SkuSnapshot = v.Sku,
    oi.LineTotal = oi.UnitPrice * oi.Quantity
FROM OrderItems oi
JOIN ProductVariants v ON v.ProductId = oi.ProductId AND v.IsDefault = 1
JOIN Products p ON p.Id = oi.ProductId;
```

> Khuyến nghị: **đổi tên bảng Inventories cũ → Inventories_OLD trước khi `Remove-Migration`** để tránh EF drop dữ liệu.

## 4. Mã nguồn cần update theo (breaking changes)

Các file sau tham chiếu các property đã bị bỏ (`Price`, `ListPrice`, `FlashSale*`, `WarrantyInfo`, `DiscountNote`) và cần update:

- `Repositories/ProductRepository.cs`
- `Repositories/AdminProductRepository.cs`
- `Areas/Admin/Repositories/AdminProductRepository.cs`
- `Services/ProductService.cs`
- `Services/ReportService.cs`
- `Controllers/ProductController.cs`
- `Areas/Admin/Controllers/ProductController.cs`
- `Controllers/CartController.cs`
- `Controllers/HomeController.cs`
- `Data/ProductSeed.cs`, `Data/OrderSeed.cs`, `Data/PromotionSeed.cs`

Mẫu update:
```csharp
// TRƯỚC
var price = product.Price;
var isFlash = product.IsFlashSaleActive;

// SAU
var variant = product.DefaultVariant ?? product.Variants.FirstOrDefault();
var price = variant?.Price ?? 0;
// IsFlashSale chuyển hẳn sang entity Promotion (Module 3 sẽ refactor)
```

## 5. Lệnh EF

```bash
dotnet ef migrations add RefactorProductVariantInventory
dotnet ef database update
```

Nếu dùng Package Manager Console:
```powershell
Add-Migration RefactorProductVariantInventory
Update-Database
```
