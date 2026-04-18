# Refactor Notes — Module 3 (Order & Snapshot) + Module 4 (Identity & Role)

## 1. Thay đổi chính

| Entity | Thay đổi |
| --- | --- |
| `Order` | Thêm `UserAddressId` (FK nullable), tách `TotalAmount` → (`SubTotal`, `DiscountAmount`, `ShippingFee`, `TotalAmount`), snapshot địa chỉ thành các trường riêng (`ShippingAddressLine`, `ShippingWard`, `ShippingDistrict`, `ShippingCity`, `ShippingAddressFull`), thêm `ConfirmedAt`/`ShippedAt`/`DeliveredAt`/`CancelledAt`/`CancelReason`/`Note`, đổi `Status`/`PaymentStatus` sang enum. |
| `OrderItem` | Đã có từ đợt refactor trước: `ProductVariantId`, `ProductNameSnapshot`, `SkuSnapshot`, `SpecSnapshot`, `DiscountAmount`, `LineTotal`, `SerialNumber`. |
| `UserAddress` **(mới)** | Sổ địa chỉ cho mỗi user: `Label`, `RecipientName/Phone`, `AddressLine`, `Ward/District/City/PostalCode`, `IsDefault`. |
| `AppUser` | Thêm `RoleId` (FK sang Role tuỳ chỉnh), `AvatarUrl`, `DateOfBirth`, `Gender`, `IsActive`, `LastLoginAt`, navigation `Addresses`. |
| `Role` | Thêm `RoleCode` (unique, UPPERCASE), `IsSystem`, `UpdatedAt`, navigation `Users`. |
| `Permission` | Thêm `PermissionCode` (unique). `(Module, Action)` unique composite. |
| `RolePermission` | Thêm `AssignedBy`. Unique composite `(RoleId, PermissionId)`. |

## 2. Quyết định kiến trúc — "Thống nhất Phân quyền"

Giữ `IdentityDbContext<AppUser>` để tận dụng login/password/email confirm/lockout của Identity nhưng **vô hiệu hoá hệ thống Role của Identity** bằng:

```csharp
builder.Ignore<IdentityRole>();
builder.Ignore<IdentityUserRole<string>>();
builder.Ignore<IdentityRoleClaim<string>>();
```

Kết quả:

- Schema KHÔNG còn `AspNetRoles`, `AspNetUserRoles`, `AspNetRoleClaims` (migration sẽ drop các bảng này).
- Giữ lại `AspNetUsers`, `AspNetUserClaims`, `AspNetUserLogins`, `AspNetUserTokens` vì vẫn dùng cho login/2FA/external login.
- `AppUser.RoleId` là nguồn thật duy nhất để xác định role.

### Tác động code

Phải **bỏ mọi chỗ gọi** các API sau của Identity:

- `UserManager.AddToRoleAsync` / `RemoveFromRoleAsync` / `GetRolesAsync`
- `RoleManager<IdentityRole>` (và bỏ `AddRoles<IdentityRole>()` trong `Program.cs`)
- `[Authorize(Roles = "Admin")]` ← KHÔNG còn hoạt động

Thay thế:

- Gán role: `user.RoleId = roleIdFromCustomTable;`
- Kiểm tra quyền: dùng **Authorization Policy** dựa trên claim "perm" hoặc custom requirement:

```csharp
// Program.cs
builder.Services.AddAuthorization(o =>
{
    o.AddPolicy("Product.Create", p => p.RequireClaim("perm", "Product.Create"));
    o.AddPolicy("AdminOnly",       p => p.RequireClaim("role", "ADMIN"));
});

// Khi đăng nhập: build claims từ Role + Permissions custom
var claims = new List<Claim> { new("role", user.Role?.RoleCode ?? "") };
claims.AddRange(user.Role.RolePermissions
    .Where(rp => rp.Permission.IsActive)
    .Select(rp => new Claim("perm", rp.Permission.PermissionCode)));
```

Thay thế attribute trong controller:
```csharp
// TRƯỚC:
[Authorize(Roles = "Admin")]

// SAU:
[Authorize(Policy = "AdminOnly")]
// hoặc theo quyền chi tiết:
[Authorize(Policy = "Product.Create")]
```

### Nếu vẫn muốn giữ Identity Roles

Bỏ 3 dòng `builder.Ignore<...>()` trong `OnModelCreating`. Nhưng như đã cảnh báo ở bản phân tích: duy trì song song 2 hệ thống sẽ gây nhầm lẫn.

## 3. Data migration plan

```sql
-- Bước 1: migrate role cũ từ AspNetUserRoles sang AppUser.RoleId
-- Giả sử bạn đã seed Roles custom (Admin=1, Staff=2, Customer=3)
UPDATE u
SET u.RoleId = CASE r.Name
                  WHEN 'Admin'    THEN 1
                  WHEN 'Staff'    THEN 2
                  WHEN 'Customer' THEN 3
               END
FROM Users u
JOIN AspNetUserRoles ur ON ur.UserId = u.Id
JOIN AspNetRoles r      ON r.Id = ur.RoleId;

-- Bước 2: seed RoleCode cho bảng Roles cũ
UPDATE Roles SET RoleCode = UPPER(REPLACE(RoleName, ' ', '_'))
WHERE RoleCode IS NULL OR RoleCode = '';

-- Bước 3: seed PermissionCode
UPDATE Permissions SET PermissionCode = CONCAT(Module, '.', Action)
WHERE PermissionCode IS NULL OR PermissionCode = '';

-- Bước 4: migrate địa chỉ từ các đơn cũ (nếu muốn tạo sẵn UserAddress)
INSERT INTO UserAddresses
  (UserId, Label, RecipientName, RecipientPhone,
   AddressLine, Ward, District, City, IsDefault, CreatedAt)
SELECT DISTINCT
   o.UserId, N'Mặc định',
   o.RecipientName, o.RecipientPhone,
   o.ShippingAddress, N'', N'', N'',
   1, GETDATE()
FROM Orders o
WHERE NOT EXISTS (SELECT 1 FROM UserAddresses a WHERE a.UserId = o.UserId);

-- Bước 5: split Order.ShippingAddress -> fields mới (nếu có format chuẩn)
-- Tuỳ dữ liệu thật mà parse. Với đơn cũ có thể fill nguyên vào ShippingAddressLine
--  và để Ward/District/City rỗng tạm.
UPDATE Orders SET ShippingAddressLine = ShippingAddress,
                  ShippingAddressFull = ShippingAddress
WHERE ShippingAddressLine IS NULL OR ShippingAddressLine = '';
```

## 4. Ví dụ checkout sinh snapshot chuẩn

```csharp
public async Task<Order> CreateOrderAsync(string userId, int addressId, List<CartItem> items)
{
    var addr = await _db.UserAddresses
        .FirstOrDefaultAsync(a => a.Id == addressId && a.UserId == userId)
        ?? throw new InvalidOperationException("Địa chỉ không hợp lệ");

    var order = new Order
    {
        OrderCode       = GenerateOrderCode(),
        UserId          = userId,
        UserAddressId   = addr.Id,

        // SNAPSHOT — copy giá trị hiện tại
        RecipientName       = addr.RecipientName,
        RecipientPhone      = addr.RecipientPhone,
        ShippingAddressLine = addr.AddressLine,
        ShippingWard        = addr.Ward,
        ShippingDistrict    = addr.District,
        ShippingCity        = addr.City,
        ShippingAddressFull = addr.FullAddress,

        Status        = OrderStatus.Pending,
        PaymentStatus = PaymentStatus.Unpaid,
    };

    foreach (var item in items)
    {
        var variant = await _db.ProductVariants
            .Include(v => v.Product)
            .FirstAsync(v => v.Id == item.VariantId);

        order.Items.Add(new OrderItem
        {
            ProductId           = variant.ProductId,
            ProductVariantId    = variant.Id,
            ProductNameSnapshot = variant.Product!.Name,
            SkuSnapshot         = variant.Sku,
            SpecSnapshot        = $"{variant.Cpu}/{variant.Ram}/{variant.Storage}",
            Quantity            = item.Quantity,
            UnitPrice           = variant.Price,
            DiscountAmount      = 0,
            LineTotal           = variant.Price * item.Quantity,
        });
    }

    order.SubTotal       = order.Items.Sum(i => i.UnitPrice * i.Quantity);
    order.DiscountAmount = order.Items.Sum(i => i.DiscountAmount);
    order.TotalAmount    = order.SubTotal - order.DiscountAmount + order.ShippingFee;

    _db.Orders.Add(order);
    await _db.SaveChangesAsync();
    return order;
}
```

## 5. Lệnh EF Migration

```bash
# Tạo migration
dotnet ef migrations add RefactorOrderSnapshotAndIdentityRole

# Kiểm tra SQL sẽ chạy trước khi apply
dotnet ef migrations script -o migration.sql

# Apply
dotnet ef database update
```

Package Manager Console:
```powershell
Add-Migration RefactorOrderSnapshotAndIdentityRole
Script-Migration -Output migration.sql
Update-Database
```

### Thứ tự thực thi đề xuất

1. Backup DB (`BACKUP DATABASE HDKTech TO DISK = ...`).
2. `Add-Migration RefactorOrderSnapshotAndIdentityRole`.
3. Mở file migration vừa tạo, **chèn các SQL data-migration ở mục 3** vào đúng vị trí `Up()` (sau khi tạo cột mới, trước khi drop cột cũ).
4. `Update-Database`.
5. Xoá code cũ dùng `UserManager.AddToRoleAsync` / `[Authorize(Roles = ...)]` — build sẽ báo lỗi giúp bạn tìm.
6. Cập nhật `Program.cs`:
   - Bỏ `.AddRoles<IdentityRole>()` nếu có.
   - Thêm khối `AddAuthorization` với các Policy tương ứng.
7. Trong sign-in handler, build claims từ `user.Role` + `RolePermissions` thay vì `SignInManager` mặc định.

## 6. Breaking changes cần sửa (grep sẵn)

- `Controllers/AccountController.cs` — login/register có thể đang gọi `UserManager.AddToRoleAsync`.
- `Program.cs` — có thể có `.AddRoles<IdentityRole>()`.
- Mọi `[Authorize(Roles = "...")]` → đổi sang `[Authorize(Policy = "...")]`.
- `Order.TotalAmount` cũ là tổng cuối; giờ phải set đầy đủ `SubTotal`/`DiscountAmount`/`ShippingFee`/`TotalAmount`.
- `Order.RecipientName/Phone/ShippingAddress` cũ vẫn còn → đổi sang `ShippingAddressLine` + các field mới.
- `Order.Status` / `Order.PaymentStatus` giờ là enum thay vì string/int — cần cast/migrate.
