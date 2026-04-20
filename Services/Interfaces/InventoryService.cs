using HDKTech.Data;
using HDKTech.Models;
using Microsoft.EntityFrameworkCore;

namespace HDKTech.Services.Interfaces
{
    /// <summary>
    /// InventoryService — refactor sau khi Inventory gắn vào ProductVariant.
    /// Các hàm lookup giờ dùng ProductVariantId (từ CartItem / OrderItem).
    /// Khi vẫn cần tương thích, fallback sang ProductId (denormalized).
    /// </summary>
    public class InventoryService : IInventoryService
    {
        private readonly HDKTechContext            _context;
        private readonly ISystemLogService         _logService;
        private readonly ILogger<InventoryService> _logger;

        public InventoryService(
            HDKTechContext context,
            ISystemLogService logService,
            ILogger<InventoryService> logger)
        {
            _context    = context;
            _logService = logService;
            _logger     = logger;
        }

        // ── ReserveStock ────────────────────────────────────────────
        public async Task<(bool Success, string Message)> ReserveStockAsync(List<CartItem> items)
        {
            foreach (var item in items)
            {
                var inv = await GetInventoryForAsync(item.ProductVariantId, item.ProductId);

                if (inv == null)
                    return (false, $"Không tìm thấy tồn kho cho cấu hình ID {item.ProductVariantId}.");

                if (inv.Quantity < item.Quantity)
                    return (false,
                        $"Cấu hình ID {item.ProductVariantId} không đủ hàng " +
                        $"(còn {inv.Quantity}, cần {item.Quantity}).");

                inv.Quantity  -= item.Quantity;
                inv.UpdatedAt  = DateTime.Now;
            }

            return (true, "OK");
        }

        // ── ReleaseStock ────────────────────────────────────────────
        public async Task<bool> ReleaseStockAsync(
            List<OrderItem> items,
            string username = "System",
            string userId   = null)
        {
            if (items == null || !items.Any()) return true;

            var released = new List<(int VariantId, int Released, int NewQty)>();

            foreach (var item in items)
            {
                var inv = await GetInventoryForAsync(item.ProductVariantId, item.ProductId);
                if (inv == null)
                {
                    _logger.LogWarning(
                        "ReleaseStock: Không có Inventory cho variant {V} / product {P}",
                        item.ProductVariantId, item.ProductId);
                    continue;
                }

                var newQty    = inv.Quantity + item.Quantity;
                inv.Quantity  = newQty;
                inv.UpdatedAt = DateTime.Now;

                released.Add((item.ProductVariantId, item.Quantity, newQty));
            }

            await _context.SaveChangesAsync();

            foreach (var (variantId, amount, newQty) in released)
            {
                await _logService.LogActionAsync(
                    username   : username,
                    actionType : "InventoryRelease",
                    module     : "Inventory",
                    description: $"Hoàn kho Variant#{variantId}: +{amount} (tồn mới: {newQty})",
                    entityId   : variantId.ToString(),
                    entityName : $"Variant#{variantId}",
                    oldValue   : (newQty - amount).ToString(),
                    newValue   : newQty.ToString(),
                    userId     : userId);
            }

            return true;
        }

        // ── CheckStockAvailability ──────────────────────────────────
        public async Task<bool> CheckStockAvailabilityAsync(List<CartItem> items)
        {
            foreach (var item in items)
            {
                var inv = await _context.Inventories
                    .AsNoTracking()
                    .FirstOrDefaultAsync(i => i.ProductVariantId == item.ProductVariantId);

                if (inv == null || inv.Quantity < item.Quantity)
                    return false;
            }
            return true;
        }

        // ── LowStock products cho Dashboard ─────────────────────────
        public async Task<List<LowStockProductItem>> GetLowStockProductsAsync(int threshold = 10)
        {
            return await _context.Inventories
                .AsNoTracking()
                .Include(i => i.Product)
                .Include(i => i.Variant)
                .Where(i => i.Quantity < threshold)
                .OrderBy(i => i.Quantity)
                .Take(20)
                .Select(i => new LowStockProductItem
                {
                    ProductId    = i.ProductId,
                    ProductName  = i.Product != null
                                        ? i.Product.Name + (i.Variant != null ? $" / {i.Variant.Sku}" : "")
                                        : $"SP#{i.ProductId}",
                    CurrentStock = i.Quantity,
                    Threshold    = threshold
                })
                .ToListAsync();
        }

        // ────────────────────────────────────────────────────────────
        /// <summary>
        /// Tra Inventory theo ProductVariantId, fallback ProductId (tương thích
        /// dữ liệu cũ chưa gán variant).
        /// </summary>
        private async Task<Inventory?> GetInventoryForAsync(int productVariantId, int productId)
        {
            if (productVariantId > 0)
            {
                var byVariant = await _context.Inventories
                    .FirstOrDefaultAsync(i => i.ProductVariantId == productVariantId);
                if (byVariant != null) return byVariant;
            }

            return await _context.Inventories
                .FirstOrDefaultAsync(i => i.ProductId == productId);
        }
    }
}
