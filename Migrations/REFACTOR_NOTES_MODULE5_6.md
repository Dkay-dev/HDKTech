# Refactor Notes — Module 5 (Promotion/Discount) + Module 6 (Warranty)

## 1. Tổng quan thay đổi

### Module 5 — Promotion

| Entity | Thay đổi |
| --- | --- |
| `Promotion` | Đổi `PromotionType`/`Status` từ string → enum. Thêm `MinOrderAmount`, `MaxDiscountAmount`, `MaxUsagePerUser`, `AppliesToAll`. Bỏ `ApplicableCategory` (string) → thay bằng quan hệ qua `PromotionProduct`. Thêm navigation `PromotionProducts`, `OrderPromotions`. Đổi `[Table("Promotion")]` → `[Table("Promotions")]`. |
| `PromotionProduct` **(mới)** | Bảng nối n-n linh hoạt: 1 dòng = 1 cặp (Promotion, ScopeTarget). `ScopeType` ∈ {Product, Variant, Category, Brand}. Có `IsExclusion` cho logic "áp toàn category trừ 2 sản phẩm". |
| `OrderPromotion` **(mới)** | Lưu vết mã áp dụng trên đơn. Snapshot: `CampaignNameSnapshot`, `PromoCodeSnapshot`, `PromotionTypeSnapshot`, `ValueSnapshot`, + `DiscountAmount` thực tế. |

### Module 6 — Warranty

| Entity | Thay đổi |
| --- | --- |
| `WarrantyPolicy` **(mới)** | Chính sách bảo hành dùng chung. `Name`, `Code`, `DurationMonths`, `Coverage`, `Terms`, `Exclusions`. |
| `WarrantyClaim` **(mới)** | Một lần yêu cầu bảo hành cho 1 máy cụ thể (trace qua `OrderItemId` + `SerialNumber` snapshot). Enum `Status` gồm 7 trạng thái, có `RepairCost`, `IsChargeable`, full timeline `DiagnosedAt/RepairStartedAt/CompletedAt/DeliveredAt`. |
| `Product` | Thêm FK `WarrantyPolicyId` (nullable). |
| `OrderItem` | Thêm navigation `WarrantyClaims`. `SerialNumber` đã có từ Module 3. |
| `Order` | Thêm navigation `Promotions` (ICollection&lt;OrderPromotion&gt;). |

## 2. Quan hệ & Delete Behavior

```
Promotion 1 ──n PromotionProduct  (Cascade — xoá Promotion thì xoá scope)
PromotionProduct n──1 Product/Variant/Category/Brand  (Restrict)
Promotion 1 ──n OrderPromotion    (SetNull — giữ lịch sử đơn)
Order     1 ──n OrderPromotion    (Cascade)

WarrantyPolicy 1 ──n Product      (SetNull)
OrderItem 1 ──n WarrantyClaim     (Restrict — bảo toàn lịch sử)
```

### Check constraint trên PromotionProduct

`CK_PromotionProduct_ExactlyOneScope`: đảm bảo chính xác **1** trong 4 FK (`ProductId`, `ProductVariantId`, `CategoryId`, `BrandId`) là NOT NULL. Tránh dữ liệu mâu thuẫn kiểu "vừa trỏ Product vừa trỏ Category".

### Indexes quan trọng

- `Promotion.PromoCode`: UNIQUE (filtered, chỉ khi != null).
- `Promotion (Status, StartDate, EndDate)`: index ghép phục vụ query "promotion nào đang chạy".
- `PromotionProduct` có index riêng cho từng loại target.
- `WarrantyClaim.ClaimCode`: UNIQUE.
- `WarrantyClaim.SerialNumber`: index (tra cứu bảo hành theo serial).
- `WarrantyClaim (Status, ClaimDate)`: index cho bảng kê claim chờ xử lý.

## 3. Data migration script

```sql
-- ==========================================================
-- (a) Migrate flash-sale từ Products cũ → Promotions + PromotionProducts
-- ==========================================================
-- Với mỗi product có IsFlashSale = 1, tạo 1 Promotion loại FlashSale tương ứng

INSERT INTO Promotions
  (CampaignName, PromotionType, Value, StartDate, EndDate,
   IsActive, Status, AppliesToAll, CreatedAt)
SELECT
   CONCAT(N'Flash Sale: ', p.Name),
   4,                                    -- PromotionType.FlashSale
   p.FlashSalePrice,                     -- giá cố định
   GETDATE(),
   ISNULL(p.FlashSaleEndTime, DATEADD(day, 7, GETDATE())),
   1, 2,                                 -- IsActive=1, Status=Running
   0, GETDATE()
FROM Products_OLD p
WHERE p.IsFlashSale = 1 AND p.FlashSalePrice IS NOT NULL;

-- Gắn scope cho từng Promotion flash sale vừa tạo
INSERT INTO PromotionProducts
  (PromotionId, ScopeType, ProductId, IsExclusion)
SELECT
   pr.Id,
   1,            -- ScopeType.Product
   p.Id,
   0
FROM Promotions pr
JOIN Products_OLD p ON pr.CampaignName = CONCAT(N'Flash Sale: ', p.Name)
WHERE p.IsFlashSale = 1;

-- ==========================================================
-- (b) Seed WarrantyPolicy mặc định từ Product.WarrantyInfo cũ
-- ==========================================================
INSERT INTO WarrantyPolicies
  (Name, Code, DurationMonths, Coverage, IsActive, CreatedAt)
VALUES
  (N'Bảo hành chính hãng 24 tháng', 'HDK-STD-24', 24, N'Tại hãng', 1, GETDATE()),
  (N'Bảo hành chính hãng 12 tháng', 'HDK-STD-12', 12, N'Tại hãng', 1, GETDATE()),
  (N'Không bảo hành',               'HDK-NONE',    0, N'N/A',     1, GETDATE());

-- Gán mặc định: product có WarrantyInfo LIKE '%24%' → policy 24T, v.v.
UPDATE p
SET p.WarrantyPolicyId = w.Id
FROM Products p
JOIN Products_OLD po ON po.Id = p.Id
JOIN WarrantyPolicies w ON
     (po.WarrantyInfo LIKE N'%24%' AND w.Code = 'HDK-STD-24')
  OR (po.WarrantyInfo LIKE N'%12%' AND w.Code = 'HDK-STD-12')
  OR (po.WarrantyInfo IS NULL      AND w.Code = 'HDK-STD-24');  -- default
```

## 4. Ví dụ sử dụng

### 4a. Tạo promotion "giảm 10% cho mọi ThinkPad, trừ X1 Carbon"

```csharp
var promo = new Promotion
{
    CampaignName    = "ThinkPad Sale 10%",
    PromotionType   = PromotionType.Percentage,
    Value           = 10,
    MaxDiscountAmount = 2_000_000,
    StartDate       = DateTime.Now,
    EndDate         = DateTime.Now.AddDays(7),
    Status          = PromotionStatus.Running,
    PromoCode       = "THINKPAD10"
};

promo.PromotionProducts.Add(new PromotionProduct
{
    ScopeType = PromotionScopeType.Category,
    CategoryId = thinkpadCategoryId
});
promo.PromotionProducts.Add(new PromotionProduct
{
    ScopeType   = PromotionScopeType.Product,
    ProductId   = x1CarbonId,
    IsExclusion = true              // loại trừ X1 Carbon
});

_db.Promotions.Add(promo);
await _db.SaveChangesAsync();
```

### 4b. Snapshot promotion khi checkout

```csharp
public void ApplyPromotion(Order order, Promotion promo, decimal discount)
{
    order.Promotions.Add(new OrderPromotion
    {
        PromotionId            = promo.Id,
        CampaignNameSnapshot   = promo.CampaignName,
        PromoCodeSnapshot      = promo.PromoCode,
        PromotionTypeSnapshot  = promo.PromotionType,
        ValueSnapshot          = promo.Value,
        DiscountAmount         = discount,
        AppliedAt              = DateTime.Now
    });
    order.DiscountAmount += discount;
    order.TotalAmount     = order.SubTotal - order.DiscountAmount + order.ShippingFee;

    promo.UsageCount++;
}
```

### 4c. Tạo yêu cầu bảo hành

```csharp
public async Task<WarrantyClaim> CreateClaimAsync(string serial, string issue)
{
    // Tra ngược OrderItem gốc từ serial
    var item = await _db.OrderItems
        .Include(oi => oi.Product).ThenInclude(p => p.WarrantyPolicy)
        .Include(oi => oi.Order)
        .FirstOrDefaultAsync(oi => oi.SerialNumber == serial)
        ?? throw new InvalidOperationException("Serial không tồn tại");

    var policy = item.Product?.WarrantyPolicy;
    var warrantyMonths = policy?.DurationMonths ?? 0;
    var expireAt = item.Order!.OrderDate.AddMonths(warrantyMonths);
    var isChargeable = DateTime.Now > expireAt;

    var claim = new WarrantyClaim
    {
        ClaimCode        = $"WC-{DateTime.Now:yyyy}-{Random.Shared.Next(1000, 9999)}",
        OrderItemId      = item.Id,
        SerialNumber     = serial,
        IssueDescription = issue,
        IsChargeable     = isChargeable,
        Status           = WarrantyClaimStatus.Received
    };
    _db.WarrantyClaims.Add(claim);
    await _db.SaveChangesAsync();
    return claim;
}
```

## 5. Lệnh EF Migration

```bash
dotnet ef migrations add AddPromotionScopeAndWarranty
dotnet ef migrations script -o migration.sql      # review trước
dotnet ef database update
```

PMC:
```powershell
Add-Migration AddPromotionScopeAndWarranty
Script-Migration -Output migration.sql
Update-Database
```

### Thứ tự thực thi khuyến nghị

1. **Backup DB** trước tiên.
2. Nếu bạn chưa apply migration Module 1-4 thì làm theo thứ tự các notes: 1-2 → 3-4 → 5-6.
3. `Add-Migration AddPromotionScopeAndWarranty`.
4. Mở file migration vừa tạo:
   - Trong `Up()`, sau khi các cột/bảng mới được tạo, chèn các lệnh SQL ở mục 3 để migrate data từ các cột Promotion/Warranty cũ.
   - Lưu ý: nếu Migration EF tự drop cột `IsFlashSale` / `FlashSalePrice` trước khi bạn kịp copy data, **di chuyển các block `DropColumn` xuống sau data-migration**.
5. `Update-Database`.
6. Remove seed data cũ liên quan `FlashSale*` trong `Data/ProductSeed.cs` và `PromotionSeed.cs` — viết lại theo schema mới.

## 6. Breaking changes cần sửa

- `Data/PromotionSeed.cs` — hiện dùng `[Table("Promotion")]` (singular) + fields cũ. Phải viết lại theo schema mới.
- `Controllers/ProductController.cs`, `Services/ProductService.cs`: code dùng `product.IsFlashSale` / `product.FlashSalePrice` → chuyển sang query Promotion:

```csharp
// TRƯỚC
var flashProducts = _db.Products.Where(p => p.IsFlashSale);

// SAU
var now = DateTime.Now;
var flashProducts = _db.Promotions
    .Where(p => p.PromotionType == PromotionType.FlashSale
             && p.IsActive && p.Status == PromotionStatus.Running
             && p.StartDate <= now && p.EndDate >= now)
    .SelectMany(p => p.PromotionProducts
        .Where(pp => pp.ScopeType == PromotionScopeType.Product
                  && !pp.IsExclusion)
        .Select(pp => new { Promotion = p, Product = pp.Product! }));
```

- `Areas/Admin/Controllers/PromotionController.cs` (nếu có): update UI binding theo enum mới.
- `[Authorize(Policy = ...)]` cho các action quản lý WarrantyClaim — thêm 2 permission mới `Warranty.View`, `Warranty.Process`.

## 7. Kiểm tra sau migration

```sql
-- Không còn bảng nào gọi là "Promotion" singular
SELECT name FROM sys.tables WHERE name LIKE '%romotion%';
-- Kỳ vọng: Promotions, PromotionProducts, OrderPromotions (3 bảng)

-- Check constraint đã tạo
SELECT name, definition FROM sys.check_constraints
WHERE name = 'CK_PromotionProduct_ExactlyOneScope';

-- Mỗi order có tổng DiscountAmount khớp SUM(OrderPromotion.DiscountAmount)
SELECT o.Id,
       o.DiscountAmount,
       SUM(op.DiscountAmount) AS SumOP
FROM Orders o
LEFT JOIN OrderPromotions op ON op.OrderId = o.Id
GROUP BY o.Id, o.DiscountAmount
HAVING o.DiscountAmount <> ISNULL(SUM(op.DiscountAmount), 0);
-- Kỳ vọng: trả về 0 dòng (không bị lệch)
```
