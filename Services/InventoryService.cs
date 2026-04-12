using HDKTech.Data;
using HDKTech.Models;
using Microsoft.EntityFrameworkCore;

namespace HDKTech.Services
{
    /// <summary>
    /// Giai đoạn 1 — Inventory Sync: Engine xử lý tồn kho.
    /// Thay đổi entity qua DbContext chung (scoped) — transaction do caller quản lý.
    /// </summary>
    public class InventoryService : IInventoryService
    {
        private readonly HDKTechContext     _context;
        private readonly ISystemLogService  _logService;
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

        // ─────────────────────────────────────────────────────────────────────
        // ReserveStock — Trừ kho khi đặt hàng (gọi trong Transaction)
        // KHÔNG gọi SaveChanges: caller sẽ save cùng lúc với Order.
        // ─────────────────────────────────────────────────────────────────────
        public async Task<(bool Success, string Message)> ReserveStockAsync(List<CartItem> items)
        {
            foreach (var item in items)
            {
                var inv = await _context.Inventories
                    .FirstOrDefaultAsync(i => i.ProductId == item.ProductId);

                if (inv == null)
                    return (false, $"Không tìm thấy tồn kho cho sản phẩm ID {item.ProductId}.");

                if (inv.Quantity < item.Quantity)
                    return (false,
                        $"Sản phẩm ID {item.ProductId} không đủ hàng " +
                        $"(còn {inv.Quantity}, cần {item.Quantity}).");

                // Chỉ thay đổi entity — SaveChanges do OrderRepository.CreateOrderAsync xử lý
                inv.Quantity  -= item.Quantity;
                inv.UpdatedAt  = DateTime.Now;
            }

            return (true, "OK");
        }

        // ─────────────────────────────────────────────────────────────────────
        // ReleaseStock — Hoàn kho khi hủy đơn (standalone, tự SaveChanges + Log)
        // ─────────────────────────────────────────────────────────────────────
        public async Task<bool> ReleaseStockAsync(
            List<OrderItem> items,
            string username = "System",
            string userId   = null)
        {
            if (items == null || !items.Any()) return true;

            var releasedItems = new List<(int ProductId, int Released, int NewQty)>();

            foreach (var item in items)
            {
                var inv = await _context.Inventories
                    .FirstOrDefaultAsync(i => i.ProductId == item.ProductId);

                if (inv == null)
                {
                    _logger.LogWarning("ReleaseStock: Không có bản ghi Inventory cho ProductId {Id}", item.ProductId);
                    continue;
                }

                var newQty   = inv.Quantity + item.Quantity;
                inv.Quantity  = newQty;
                inv.UpdatedAt = DateTime.Now;

                releasedItems.Add((item.ProductId, item.Quantity, newQty));
            }

            // Lưu toàn bộ thay đổi kho trong 1 lần
            await _context.SaveChangesAsync();

            // ── Auto Audit Log ─────────────────────────────────────────────
            foreach (var (productId, released, newQty) in releasedItems)
            {
                await _logService.LogActionAsync(
                    username   : username,
                    actionType : "InventoryRelease",
                    module     : "Inventory",
                    description: $"Hoàn kho SP#{productId}: +{released} (tồn kho mới: {newQty})",
                    entityId   : productId.ToString(),
                    entityName : $"Product#{productId}",
                    oldValue   : (newQty - released).ToString(),
                    newValue   : newQty.ToString(),
                    userId     : userId);
            }

            return true;
        }

        // ─────────────────────────────────────────────────────────────────────
        // CheckStockAvailability — Kiểm tra nhanh trước khi checkout
        // ─────────────────────────────────────────────────────────────────────
        public async Task<bool> CheckStockAvailabilityAsync(List<CartItem> items)
        {
            foreach (var item in items)
            {
                var inv = await _context.Inventories
                    .AsNoTracking()
                    .FirstOrDefaultAsync(i => i.ProductId == item.ProductId);

                if (inv == null || inv.Quantity < item.Quantity)
                    return false;
            }
            return true;
        }

        // ─────────────────────────────────────────────────────────────────────
        // GetLowStockProducts — Dữ liệu cảnh báo cho Dashboard
        // ─────────────────────────────────────────────────────────────────────
        public async Task<List<LowStockProductItem>> GetLowStockProductsAsync(int threshold = 5)
        {
            return await _context.Inventories
                .AsNoTracking()
                .Include(i => i.Product)
                .Where(i => i.Quantity < threshold)
                .OrderBy(i => i.Quantity)
                .Take(20)
                .Select(i => new LowStockProductItem
                {
                    ProductId    = i.ProductId,
                    ProductName  = i.Product != null ? i.Product.Name : $"SP#{i.ProductId}",
                    CurrentStock = i.Quantity,
                    Threshold    = threshold
                })
                .ToListAsync();
        }
    }
}
