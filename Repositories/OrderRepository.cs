using HDKTech.Data;
using HDKTech.Models;
using HDKTech.Repositories.Interfaces;
using HDKTech.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Text.Json;

namespace HDKTech.Repositories
{
    /// <summary>
    /// OrderRepository — refactored Module A + MERGED commit Huy 3f92de9.
    ///
    /// Thay đổi so với bản Khoa hiện tại:
    ///  • GetOrderByMaDonHangAsync / GetUserOrdersAsync  →  load thêm
    ///      Items → Product → Images / Category    (cần cho view Chi tiết)
    ///  • Thêm CancelOrderAsync — phục vụ OrderController.Cancel()
    ///  • Vẫn giữ logic re-fetch giá từ DB, transaction, idempotency.
    /// </summary>
    public class OrderRepository : GenericRepository<Order>, IOrderRepository
    {
        private readonly IInventoryService _inventoryService;

        public OrderRepository(HDKTechContext context, IInventoryService inventoryService)
            : base(context)
        {
            _inventoryService = inventoryService;
        }

        // ── COD / Direct checkout ────────────────────────────────────
        public async Task<Order> CreateOrderAsync(
            string userId,
            string RecipientName,
            string soDienThoai,
            string ShippingAddress,
            List<CartItem> items,
            decimal ShippingFee = 0,
            string paymentMethod = "COD",
            string paymentStatus = "Unpaid")
        {
            // Re-fetch giá từ DB — KHÔNG tin giá trong CartItem
            var variantIds = items.Select(i => i.ProductVariantId).Distinct().ToList();
            var variants = await _context.ProductVariants
                .AsNoTracking()
                .Where(v => variantIds.Contains(v.Id))
                .ToDictionaryAsync(v => v.Id);

            decimal subTotal = 0;
            foreach (var item in items)
            {
                if (!variants.TryGetValue(item.ProductVariantId, out var variant))
                    throw new InvalidOperationException(
                        $"Không tìm thấy variant ID {item.ProductVariantId}.");
                subTotal += variant.Price * item.Quantity;
            }

            await using var transaction = await _context.Database
                .BeginTransactionAsync(IsolationLevel.RepeatableRead);
            try
            {
                var (reserved, errMsg) = await _inventoryService.ReserveStockAsync(items);
                if (!reserved)
                {
                    await transaction.RollbackAsync();
                    throw new InvalidOperationException(errMsg);
                }

                var orderCode = await GenerateUniqueOrderCodeAsync();
                var totalAmount = subTotal + ShippingFee;

                var order = new Order
                {
                    UserId = userId,
                    OrderCode = orderCode,
                    RecipientName = RecipientName,
                    RecipientPhone = soDienThoai,
                    ShippingAddressLine = ShippingAddress ?? string.Empty,
                    ShippingAddressFull = ShippingAddress,
                    SubTotal = subTotal,
                    DiscountAmount = 0,
                    ShippingFee = ShippingFee,
                    TotalAmount = totalAmount,
                    Status = OrderStatus.Pending,
                    OrderDate = DateTime.Now,
                    PaymentMethod = paymentMethod ?? "COD",
                    PaymentStatus = ParsePaymentStatus(paymentStatus),
                    PaidAt = ParsePaymentStatus(paymentStatus) == PaymentStatus.Paid
                                              ? DateTime.Now : (DateTime?)null,
                    Items = items.Select(item => new OrderItem
                    {
                        ProductId = item.ProductId,
                        ProductVariantId = item.ProductVariantId,
                        ProductNameSnapshot = item.ProductName,
                        SkuSnapshot = item.SkuSnapshot,
                        SpecSnapshot = item.SpecSnapshot,
                        Quantity = item.Quantity,
                        UnitPrice = variants[item.ProductVariantId].Price,
                        DiscountAmount = 0,
                        LineTotal = variants[item.ProductVariantId].Price * item.Quantity
                    }).ToList()
                };

                await _context.AddAsync(order);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return order;
            }
            catch (DbUpdateConcurrencyException)
            {
                await transaction.RollbackAsync();
                throw new InvalidOperationException(
                    "Có xung đột dữ liệu tồn kho. Vui lòng thử lại.");
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // ── Tạo Order từ PendingCheckout (sau payment callback) ──────
        public async Task<(bool Success, string? Error, Order? Order)> CreateFromPendingCheckoutAsync(
            PendingCheckout pending)
        {
            if (pending.Status != CheckoutStatus.Pending)
                return (false, "Checkout session đã được xử lý hoặc hết hạn.", null);

            if (pending.ExpiresAt < DateTime.UtcNow)
            {
                pending.Status = CheckoutStatus.Expired;
                await _context.SaveChangesAsync();
                return (false, "Checkout session đã hết hạn (30 phút). Vui lòng đặt hàng lại.", null);
            }

            List<CartItemSnapshot> snapshots;
            try
            {
                snapshots = JsonSerializer.Deserialize<List<CartItemSnapshot>>(pending.CartSnapshot)
                    ?? new List<CartItemSnapshot>();
            }
            catch
            {
                return (false, "Lỗi đọc thông tin giỏ hàng.", null);
            }

            if (!snapshots.Any())
                return (false, "Giỏ hàng trống.", null);

            var variantIds = snapshots.Select(s => s.ProductVariantId).Distinct().ToList();
            var variants = await _context.ProductVariants
                .AsNoTracking()
                .Where(v => variantIds.Contains(v.Id))
                .ToDictionaryAsync(v => v.Id);

            await using var transaction = await _context.Database
                .BeginTransactionAsync(IsolationLevel.RepeatableRead);
            try
            {
                var cartItems = snapshots.Select(s => new CartItem(
                    s.ProductId, s.ProductVariantId, s.ProductName,
                    s.UnitPrice, s.Quantity,
                    s.SkuSnapshot, s.SpecSnapshot, s.ImageUrl)).ToList();

                var (reserved, errMsg) = await _inventoryService.ReserveStockAsync(cartItems);
                if (!reserved)
                {
                    await transaction.RollbackAsync();
                    pending.Status = CheckoutStatus.Failed;
                    await _context.SaveChangesAsync();
                    return (false, errMsg, null);
                }

                var orderCode = await GenerateUniqueOrderCodeAsync();
                var totalAmount = pending.SubTotal + pending.ShippingFee - pending.Discount;

                var order = new Order
                {
                    UserId = pending.UserId,
                    OrderCode = orderCode,
                    RecipientName = pending.RecipientName,
                    RecipientPhone = pending.RecipientPhone,
                    ShippingAddressLine = pending.ShippingAddress,
                    ShippingAddressFull = pending.ShippingAddress,
                    Note = pending.Note,
                    SubTotal = pending.SubTotal,
                    DiscountAmount = pending.Discount,
                    ShippingFee = pending.ShippingFee,
                    TotalAmount = totalAmount,
                    Status = OrderStatus.Pending,
                    OrderDate = DateTime.Now,
                    PaymentMethod = pending.PaymentMethod,
                    PaymentStatus = PaymentStatus.Paid,
                    PaidAt = DateTime.Now,
                    Items = snapshots.Select(s => new OrderItem
                    {
                        ProductId = s.ProductId,
                        ProductVariantId = s.ProductVariantId,
                        ProductNameSnapshot = s.ProductName,
                        SkuSnapshot = s.SkuSnapshot,
                        SpecSnapshot = s.SpecSnapshot,
                        Quantity = s.Quantity,
                        UnitPrice = variants.TryGetValue(s.ProductVariantId, out var v)
                                                   ? v.Price : s.UnitPrice,
                        DiscountAmount = 0,
                        LineTotal = (variants.TryGetValue(s.ProductVariantId, out var v2)
                                                   ? v2.Price : s.UnitPrice) * s.Quantity
                    }).ToList()
                };

                await _context.AddAsync(order);

                pending.Status = CheckoutStatus.Paid;
                pending.PaidAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return (true, null, order);
            }
            catch (DbUpdateConcurrencyException)
            {
                await transaction.RollbackAsync();
                return (false, "Xung đột tồn kho. Vui lòng thử đặt hàng lại.", null);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // ── Query Methods ────────────────────────────────────────────
        //  ✅ MERGED từ commit Huy: eager-load Product → Images / Category
        //     để trang Order/Details có thể render ảnh + tên sản phẩm.
        // ─────────────────────────────────────────────────────────────

        public async Task<Order> GetOrderByMaDonHangAsync(string OrderCode)
        {
            return await _context.Set<Order>()
                .Include(x => x.Items)
                    .ThenInclude(i => i.Product)
                        .ThenInclude(p => p!.Images)
                .Include(x => x.Items)
                    .ThenInclude(i => i.Product)
                        .ThenInclude(p => p!.Category)
                .Include(x => x.Items)
                    .ThenInclude(i => i.Variant)
                .Include(x => x.User)
                .FirstOrDefaultAsync(x => x.OrderCode == OrderCode);
        }

        public async Task<IEnumerable<Order>> GetUserOrdersAsync(string userId)
        {
            return await _context.Set<Order>()
                .Include(x => x.Items)
                    .ThenInclude(i => i.Product)
                        .ThenInclude(p => p!.Images)
                .Include(x => x.Items)
                    .ThenInclude(i => i.Product)
                        .ThenInclude(p => p!.Category)
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.OrderDate)
                .ToListAsync();
        }

        public async Task<bool> UpdateOrderStatusAsync(int maOrder, int trangThaiMoi)
        {
            var order = await _context.Set<Order>().FindAsync(maOrder);
            if (order == null) return false;

            order.Status = (OrderStatus)trangThaiMoi;
            _context.Update(order);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteOrderAsync(int maOrder)
        {
            var order = await _context.Set<Order>()
                .Include(x => x.Items)
                .FirstOrDefaultAsync(x => x.Id == maOrder);

            if (order == null) return false;

            _context.RemoveRange(order.Items);
            _context.Remove(order);
            await _context.SaveChangesAsync();
            return true;
        }

        // ── CANCEL (merged từ yêu cầu Huy: "Hủy đơn hàng phải đi kèm xem chi tiết") ─
        public async Task<(bool Ok, string? Error)> CancelOrderAsync(
            int orderId,
            string userId,
            string? cancelReason)
        {
            var order = await _context.Set<Order>()
                .FirstOrDefaultAsync(x => x.Id == orderId);

            if (order == null)
                return (false, "Không tìm thấy đơn hàng.");

            if (order.UserId != userId)
                return (false, "Bạn không có quyền huỷ đơn hàng này.");

            if (order.Status != OrderStatus.Pending &&
                order.Status != OrderStatus.Confirmed)
                return (false, "Đơn hàng đã được xử lý, không thể huỷ.");

            order.Status = OrderStatus.Cancelled;
            order.CancelledAt = DateTime.Now;
            order.CancelReason = string.IsNullOrWhiteSpace(cancelReason)
                                     ? "Khách huỷ đơn"
                                     : cancelReason.Trim();

            _context.Update(order);
            await _context.SaveChangesAsync();
            return (true, null);
        }

        // ── Private Helpers ──────────────────────────────────────────
        private async Task<string> GenerateUniqueOrderCodeAsync()
        {
            string code;
            var retries = 3;
            do
            {
                code = $"HDK{DateTime.Now:yyyyMMddHHmmss}_{Random.Shared.Next(1000, 9999)}";
                var exists = await _context.Set<Order>().AnyAsync(x => x.OrderCode == code);
                if (!exists) return code;
            } while (--retries > 0);

            return code;
        }

        private static PaymentStatus ParsePaymentStatus(string? value) =>
            value?.Trim().ToLowerInvariant() switch
            {
                "paid" => PaymentStatus.Paid,
                "refunded" => PaymentStatus.Refunded,
                "failed" => PaymentStatus.Failed,
                _ => PaymentStatus.Unpaid
            };
    }
}
