using HDKTech.Data;
using HDKTech.Models;
using HDKTech.Utils;
using Microsoft.EntityFrameworkCore;

namespace HDKTech.Services
{
    /// <summary>
    /// DbCartService — Cart lưu trong Database thay Session.
    ///
    /// Lý do chuyển sang DB:
    ///  - Session mất dữ liệu khi server restart / scale out nhiều instance
    ///  - Không thể merge cart guest → user
    ///  - Không validate được tồn kho real-time
    ///
    /// Guest support: dùng cookie "GuestCartId" (GUID) để identify.
    /// Khi user login, gọi MergeGuestCartAsync() để gộp cart.
    /// </summary>
    public class DbCartService : ICartService
    {
        private const string GuestCartCookieName = "GuestCartId";

        private readonly HDKTechContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<DbCartService> _logger;

        public DbCartService(
            HDKTechContext context,
            IHttpContextAccessor httpContextAccessor,
            ILogger<DbCartService> logger)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        // ── Public API ──────────────────────────────────────────────

        public async Task<Cart> GetCartAsync()
        {
            var (userId, guestId) = GetIdentity();
            var dbItems = await LoadDbItemsAsync(userId, guestId);

            var cart = new Cart();
            cart.Items = dbItems.Select(MapToCartItem).Where(x => x != null).ToList()!;
            return cart;
        }

        public async Task AddItemAsync(CartItem item)
        {
            var (userId, guestId) = GetIdentity();

            // 1. Validate tồn kho trước khi thêm vào cart
            var inventory = await GetInventoryAsync(item.ProductVariantId, item.ProductId);
            if (inventory == null)
                throw new InvalidOperationException("Không tìm thấy thông tin tồn kho.");

            // 2. Tính tổng quantity hiện có trong cart + quantity mới
            var existing = await GetDbItemAsync(userId, guestId, item.ProductId, item.ProductVariantId);
            var totalQuantity = (existing?.Quantity ?? 0) + item.Quantity;

            if (totalQuantity > inventory.AvailableQuantity)
            {
                var avail = inventory.AvailableQuantity;
                throw new InvalidOperationException(
                    avail <= 0
                        ? $"Sản phẩm \"{item.ProductName}\" đã hết hàng."
                        : $"Sản phẩm \"{item.ProductName}\" chỉ còn {avail} cái. " +
                          $"Bạn đã có {existing?.Quantity ?? 0} trong giỏ.");
            }

            // 3. Upsert vào DB
            if (existing != null)
            {
                existing.Quantity = totalQuantity;
                existing.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                await _context.CartItems.AddAsync(new DbCartItem
                {
                    UserId           = userId,
                    GuestId          = guestId,
                    ProductId        = item.ProductId,
                    ProductVariantId = item.ProductVariantId,
                    Quantity         = item.Quantity
                });
            }

            // Đảm bảo guest có cookie
            if (userId == null && guestId != null)
                EnsureGuestCookie(guestId);

            await _context.SaveChangesAsync();
        }

        public async Task RemoveItemAsync(int productId, int productVariantId)
        {
            var (userId, guestId) = GetIdentity();
            var item = await _context.CartItems
                .FirstOrDefaultAsync(c =>
                    (userId != null ? c.UserId == userId : c.GuestId == guestId)
                    && c.ProductId        == productId
                    && c.ProductVariantId == productVariantId);  // Without AsNoTracking() để có thể remove
            if (item != null)
            {
                _context.CartItems.Remove(item);
                await _context.SaveChangesAsync();
            }
        }

        public async Task UpdateQuantityAsync(int productId, int productVariantId, int quantity)
        {
            var (userId, guestId) = GetIdentity();
            var item = await _context.CartItems
                .FirstOrDefaultAsync(c =>
                    (userId != null ? c.UserId == userId : c.GuestId == guestId)
                    && c.ProductId        == productId
                    && c.ProductVariantId == productVariantId);  // Without AsNoTracking() để có thể update/remove
            if (item == null) return;

            if (quantity <= 0)
            {
                _context.CartItems.Remove(item);
                await _context.SaveChangesAsync();
                return;
            }

            // Validate tồn kho khi cập nhật số lượng
            var inventory = await GetInventoryAsync(productVariantId, productId);
            if (inventory == null)
                throw new InvalidOperationException("Không tìm thấy thông tin tồn kho.");

            if (quantity > inventory.AvailableQuantity)
                throw new InvalidOperationException(
                    $"Chỉ còn {inventory.AvailableQuantity} sản phẩm trong kho.");

            item.Quantity  = quantity;
            item.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        public async Task ClearCartAsync()
        {
            var (userId, guestId) = GetIdentity();
            var items = await _context.CartItems
                .Where(c => userId != null ? c.UserId == userId : c.GuestId == guestId)
                .ToListAsync();  // Without AsNoTracking() để có thể remove
            if (items.Any())
            {
                _context.CartItems.RemoveRange(items);
                await _context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Merge cart guest vào cart của user đã đăng nhập.
        /// Gọi từ Login callback (hoặc ExternalLogin).
        /// </summary>
        public async Task MergeGuestCartAsync(string userId)
        {
            var guestId = GetGuestId();
            if (string.IsNullOrEmpty(guestId)) return;

            var guestItems = await _context.CartItems
                .Where(c => c.GuestId == guestId)
                .ToListAsync();

            foreach (var guestItem in guestItems)
            {
                var existing = await GetDbItemAsync(userId, null,
                    guestItem.ProductId, guestItem.ProductVariantId);

                if (existing != null)
                {
                    // Cộng dồn số lượng (không overwrite)
                    var inventory = await GetInventoryAsync(
                        guestItem.ProductVariantId, guestItem.ProductId);
                    var maxQty = inventory?.AvailableQuantity ?? existing.Quantity;

                    existing.Quantity  = Math.Min(existing.Quantity + guestItem.Quantity, maxQty);
                    existing.UpdatedAt = DateTime.UtcNow;
                    _context.CartItems.Remove(guestItem);
                }
                else
                {
                    guestItem.UserId  = userId;
                    guestItem.GuestId = null;
                }
            }

            await _context.SaveChangesAsync();

            // Xóa cookie guest sau khi merge
            _httpContextAccessor.HttpContext?.Response.Cookies.Delete(GuestCartCookieName);
            _logger.LogInformation("Merged guest cart {GuestId} → user {UserId}", guestId, userId);
        }

        // ── Private Helpers ─────────────────────────────────────────

        private (string? UserId, string? GuestId) GetIdentity()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null) return (null, null);

            var userId = httpContext.User.Identity?.IsAuthenticated == true
                ? httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                : null;

            if (userId != null) return (userId, null);

            // Guest: tạo mới hoặc đọc từ cookie
            var guestId = GetGuestId() ?? Guid.NewGuid().ToString();
            return (null, guestId);
        }

        private string? GetGuestId()
            => _httpContextAccessor.HttpContext?.Request.Cookies[GuestCartCookieName];

        private void EnsureGuestCookie(string guestId)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null) return;

            if (!httpContext.Request.Cookies.ContainsKey(GuestCartCookieName))
            {
                httpContext.Response.Cookies.Append(GuestCartCookieName, guestId, new CookieOptions
                {
                    HttpOnly = true,
                    SameSite = SameSiteMode.Lax,
                    Expires  = DateTimeOffset.UtcNow.AddDays(30)
                });
            }
        }

        private async Task<List<DbCartItem>> LoadDbItemsAsync(string? userId, string? guestId)
        {
            return await _context.CartItems
                .Include(c => c.Product)
                    .ThenInclude(p => p!.Category)
                .Include(c => c.Product)
                    .ThenInclude(p => p!.Images)
                .Include(c => c.Variant)
                    .ThenInclude(v => v!.Inventories)
                .Where(c => userId != null ? c.UserId == userId : c.GuestId == guestId)
                .AsNoTracking()
                .ToListAsync();
        }

        private async Task<DbCartItem?> GetDbItemAsync(
            string? userId, string? guestId, int productId, int productVariantId)
        {
            return await _context.CartItems
                .AsNoTracking()
                .FirstOrDefaultAsync(c =>
                    (userId != null ? c.UserId == userId : c.GuestId == guestId)
                    && c.ProductId        == productId
                    && c.ProductVariantId == productVariantId);
        }

        private async Task<Inventory?> GetInventoryAsync(int productVariantId, int productId)
        {
            // Ưu tiên lookup theo variant, fallback sang productId
            if (productVariantId > 0)
            {
                var byVariant = await _context.Inventories
                    .AsNoTracking()
                    .FirstOrDefaultAsync(i => i.ProductVariantId == productVariantId);
                if (byVariant != null) return byVariant;
            }
            return await _context.Inventories
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.ProductId == productId);
        }

        /// <summary>
        /// Map DbCartItem → CartItem (enriched với giá từ DB).
        /// Null nếu product/variant đã bị xóa.
        /// </summary>
        private CartItem? MapToCartItem(DbCartItem dbItem)
        {
            if (dbItem.Product == null || dbItem.Variant == null) return null;

            var rawImageUrl = dbItem.Product.Images?.FirstOrDefault(h => h.IsDefault)?.ImageUrl
                           ?? dbItem.Product.Images?.FirstOrDefault()?.ImageUrl;

            return new CartItem(
                productId:        dbItem.ProductId,
                productVariantId: dbItem.ProductVariantId,
                productName:      dbItem.Product.Name,
                price:            dbItem.Variant.Price,   // luôn lấy giá mới nhất từ DB
                quantity:         dbItem.Quantity,
                skuSnapshot:      dbItem.Variant.Sku,
                specSnapshot:     BuildSpecSnapshot(dbItem.Variant),
                ImageUrl:         ImageHelper.GetImagePath(rawImageUrl, dbItem.Product.Category?.Name),
                categoryName:     dbItem.Product.Category?.Name);
        }

        private static string BuildSpecSnapshot(ProductVariant v)
        {
            var parts = new[] { v.Cpu, v.Ram, v.Storage, v.Gpu }
                .Where(s => !string.IsNullOrWhiteSpace(s));
            return string.Join(" / ", parts);
        }
    }
}
