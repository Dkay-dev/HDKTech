# 🎯 HDKTech - Next Steps & Roadmap

## Prioritized Fixes Remaining

### 🔴 CRITICAL (Fix trước khi demo)

#### 1. BannerController.ToggleActive - AntiForgery Token Issue
**File:** `Areas/Admin/Controllers/BannerController.cs`
**Problem:** ValidateAntiForgeryToken + FromBody JSON không tương thích
```javascript
// JavaScript call fails with 400 Bad Request
fetch('/Admin/Banner/ToggleActive', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ maBanner: 1, isActive: true })
})
```

**Solution 1 - Bỏ Token Validation (Quick):**
```csharp
[HttpPost("ToggleActive")]
// [ValidateAntiForgeryToken]  ← Bỏ cái này vì AJAX
public async Task<IActionResult> ToggleActive([FromBody] dynamic request)
{
    // Token không cần cho internal API
}
```

**Solution 2 - Dùng FormData (Better):**
```javascript
const form = new FormData();
form.append('maBanner', 1);
form.append('isActive', true);

fetch('/Admin/Banner/ToggleActive', {
    method: 'POST',
    body: form
})
```

---

#### 2. CategoryController - Hardcoded Category IDs
**File:** `Controllers/CategoryController.cs`
**Current Issue:**
```csharp
if (parentCategory?.MaDanhMuc == 15)  // Thương hiệu
if (parentCategory?.MaDanhMuc == 21)  // Giá bán
```
❌ Nếu seed data thay đổi order → sai hết

**Fix:** Tạo Category Config
```csharp
// Models/CategoryConfig.cs
public static class CategoryConfig
{
    public const int LAPTOP = 1;
    public const int DESKTOP = 2;
    public const int BRAND = 15;
    public const int PRICE_RANGE = 21;
}

// CategoryController
if (parentCategory?.MaDanhMuc == CategoryConfig.BRAND)
```

Or better - **Tag Categories với type:**
```csharp
// Add column: Category.CategoryType enum { Normal, Filter_Brand, Filter_Price, ... }
```

---

#### 3. Email Notifications - Not Implemented
**Impact:** Khách đặt hàng không nhận được confirmation

**Files to Update:**
- Inject `IEmailSender` (đã có dependency)
- Implement `SmtpEmailSender` hoặc dùng SendGrid
- Update `CheckoutController.Index()` - Send confirmation after CreateOrder
- Update `OrderController (Admin)` - Send status change notifications

**Quick Fix - Fake Email (Dev):**
```csharp
public class FakeEmailSender : IEmailSender
{
    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        System.Diagnostics.Debug.WriteLine($"[FAKE EMAIL] To: {email}, Subject: {subject}");
    }
}
```

**Production Fix - SendGrid:**
```csharp
public class SendGridEmailSender : IEmailSender
{
    private readonly SendGridClient _sendGridClient;
    
    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        var msg = new SendGridMessage()
        {
            From = new EmailAddress("noreply@hdktech.com", "HDKTech"),
            Subject = subject,
            HtmlContent = htmlMessage
        };
        msg.AddTo(new EmailAddress(email));
        
        await _sendGridClient.SendEmailAsync(msg);
    }
}
```

---

### 🟡 HIGH (Fix trước khi production)

#### 4. Manager Role - Too Broad Permissions
**Current:**
```csharp
[Authorize(Roles = "Admin,Manager")]
// Manager can see/edit EVERYTHING
```

**Better - Permission-Based:**
```csharp
[Authorize(Policy = "CanManageProducts")]
public class ProductController { }

[Authorize(Policy = "CanManageOrders")]
public class OrderController { }

[Authorize(Roles = "Admin")]  // Only admin
public class RoleController { }
```

**In Program.cs:**
```csharp
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("CanManageProducts", policy =>
        policy.Requirements.Add(new PermissionRequirement("ManageProducts")))
    .AddPolicy("CanManageOrders", policy =>
        policy.Requirements.Add(new PermissionRequirement("ManageOrders")));
```

---

#### 5. Session Cart - Lost on Server Restart
**Issue:** Khách thêm 5 sản phẩm → Server restart → Mất giỏ hàng

**Solutions:**
1. **Database-Backed Cart (Recommended)**
   - Create `Cart` table: UserId, ProductId, Quantity, CreatedDate
   - Migrate SessionCartService → DatabaseCartService
   - Persistent across sessions

2. **Hybrid Approach**
   - Keep Session for temp cart
   - Sync to DB every 30 seconds (timer)
   - Restore from DB on login

**Quick Implementation:**
```csharp
// Models/Cart.cs
public class CartItem
{
    public int MaSanPham { get; set; }
    public int SoLuong { get; set; }
    public decimal Gia { get; set; }
    public DateTime NgayThem { get; set; }
}

// Save to DB instead of Session
```

---

### 🟢 MEDIUM (Nice to Have)

#### 6. Product Inventory Management
**Missing:** 
- Nhập hàng từ nhà cung cấp
- Xuất hàng (bán, tặng, hỏng)
- Báo cáo tồn kho theo thời gian

**Implementation:**
```csharp
// Models/InventoryLog.cs
public class InventoryLog
{
    public int MaSanPham { get; set; }
    public int SoLuong { get; set; }  // Positive=nhập, Negative=xuất
    public string LoaiGiaoDich { get; set; }  // "Import", "Sale", "Damage"
    public DateTime NgayTao { get; set; }
    public string GhiChu { get; set; }
}

// Admin/InventoryController CRUD
```

---

#### 7. Revenue Dashboard & Reports
**Current:** Dashboard chỉ show "7 days"
**Add:** Monthly, Quarterly, Yearly reports

```csharp
public async Task<DashboardReportDto> GetMonthlyReportAsync(int month, int year)
{
    var orders = await _context.Orders
        .Where(o => o.NgayDatHang.Month == month && o.NgayDatHang.Year == year)
        .ToListAsync();
    
    return new DashboardReportDto
    {
        TotalRevenue = orders.Sum(o => o.TongTien),
        TotalOrders = orders.Count,
        AverageOrderValue = orders.Average(o => o.TongTien),
        TopProducts = GetTopProductsByRevenue(orders)
    };
}
```

---

#### 8. Wishlist / Favorites
**Add:**
- Heart icon trên product card
- `WishlistItem` table
- Wishlist page

```csharp
// Models/WishlistItem.cs
public class WishlistItem
{
    public int MaSanPham { get; set; }
    public string MaNguoiDung { get; set; }
    public DateTime NgayThem { get; set; }
}
```

---

#### 9. Product Search Autocomplete
**Add:**
- Endpoint `/api/products/search?q=iphone`
- JSON response với suggestions
- JavaScript UI component

```csharp
[HttpGet("/api/products/search")]
public async Task<IActionResult> Search([FromQuery] string q, [FromQuery] int limit = 10)
{
    var products = await _productRepo.GetAllWithImagesAsync();
    var results = products
        .Where(p => p.TenSanPham.Contains(q, StringComparison.OrdinalIgnoreCase))
        .Take(limit)
        .Select(p => new { p.MaSanPham, p.TenSanPham, p.Gia })
        .ToList();
    
    return Ok(results);
}
```

---

#### 10. Product Ratings & Reviews
**Current:** `DanhGia` table exists nhưng không có UI

**Add:**
- View ratings on product detail
- Form to submit rating (stars + comment)
- Moderation by admin

```csharp
// Views/Product/Details.cshtml - Add:
<div class="product-reviews">
    @foreach (var review in Model.DanhGias)
    {
        <div class="review-item">
            <div class="stars">★★★★★ @review.SoSao/5</div>
            <p>@review.NhanXet</p>
            <small>- @review.NguoiDung?.HoTen</small>
        </div>
    }
    
    @if (User.Identity.IsAuthenticated)
    {
        <form asp-action="AddReview" method="post">
            <textarea name="comment"></textarea>
            <select name="rating">
                <option>1 - Rất tệ</option>
                <option>5 - Rất tốt</option>
            </select>
            <button type="submit">Đánh giá</button>
        </form>
    }
</div>
```

---

## 📋 Testing Checklist

### Unit Tests (Priority)
```csharp
namespace HDKTech.Tests
{
    [TestClass]
    public class SecurityTests
    {
        [TestMethod]
        public async Task OrderController_UnauthorizedAccess_ReturnsForbidden()
        {
            // Arrange
            var userA = new NguoiDung { Id = "user-a" };
            var userB = new NguoiDung { Id = "user-b" };
            var order = new DonHang { MaNguoiDung = "user-a" };
            
            // Act - userB tries to view userA's order
            var controller = new OrderController(...);
            controller.User = CreateClaimsPrincipal(userB);
            
            var result = await controller.Details(order.MaDonHangChuoi);
            
            // Assert
            Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
        }
    }
}
```

### Performance Tests
```powershell
# Load test Homepage
ab -n 100 -c 10 https://localhost:5001/

# Check query count
Debug.WriteLine($"Total queries: {db.ChangeTracker.Entries().Count()}");
```

---

## 📅 Suggested Timeline

| Week | Task | Priority |
|------|------|----------|
| Week 1 | Fix BannerController, Email, Hardcoded IDs | 🔴 |
| Week 2 | Manager permissions, Session cart | 🟡 |
| Week 3 | Inventory, Dashboard reports | 🟢 |
| Week 4 | Wishlist, Search, Reviews | 🟢 |

---

## 📞 Quick Reference

**File Locations:**
- Controllers: `Controllers/`, `Areas/Admin/Controllers/`
- Models: `Models/`
- Repositories: `Repositories/`
- Views: `Views/`, `Areas/Admin/Views/`
- Config: `Program.cs`

**Common Tasks:**
- Add migration: `dotnet ef migrations add NAME -p HDKTech`
- Update DB: `dotnet ef database update -p HDKTech`
- Run tests: `dotnet test`
- Build: `dotnet build`

---

**Last Updated:** $(date)
**Status:** In Progress 🚀
