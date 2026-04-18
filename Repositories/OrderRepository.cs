using HDKTech.Data;
using HDKTech.Models;
using HDKTech.Repositories.Interfaces;
using HDKTech.Services;
using Microsoft.EntityFrameworkCore;

namespace HDKTech.Repositories
{
    /// <summary>
    /// OrderRepository — refactor:
    ///  - Map CartItem.ProductVariantId sang OrderItem.ProductVariantId.
    ///  - Ghi snapshot: ProductNameSnapshot, SkuSnapshot, SpecSnapshot, UnitPrice, LineTotal.
    ///  - Dùng enum OrderStatus / PaymentStatus thay cho magic string/number.
    ///  - Map tham số ShippingAddress sang Order.ShippingAddressLine.
    /// </summary>
    public class OrderRepository : GenericRepository<Order>, IOrderRepository
    {
        private readonly IInventoryService _inventoryService;

        public OrderRepository(HDKTechContext context, IInventoryService inventoryService)
            : base(context)
        {
            _inventoryService = inventoryService;
        }

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
            var subTotal   = items.Sum(x => x.Price * x.Quantity);
            var totalAmount = subTotal + ShippingFee;

            var orderCode = await GenerateUniqueOrderCodeAsync();

            var order = new Order
            {
                UserId               = userId,
                OrderCode            = orderCode,
                RecipientName        = RecipientName,
                RecipientPhone       = soDienThoai,
                ShippingAddressLine  = ShippingAddress ?? string.Empty,
                ShippingAddressFull  = ShippingAddress,
                SubTotal             = subTotal,
                DiscountAmount       = 0,
                ShippingFee          = ShippingFee,
                TotalAmount          = totalAmount,
                Status               = OrderStatus.Pending,
                OrderDate            = DateTime.Now,
                PaymentMethod        = paymentMethod ?? "COD",
                PaymentStatus        = ParsePaymentStatus(paymentStatus),
                PaidAt               = ParsePaymentStatus(paymentStatus) == PaymentStatus.Paid
                                        ? DateTime.Now
                                        : (DateTime?)null,
                Items                = new List<OrderItem>()
            };

            foreach (var item in items)
            {
                order.Items.Add(new OrderItem
                {
                    ProductId           = item.ProductId,
                    ProductVariantId    = item.ProductVariantId,
                    ProductNameSnapshot = item.ProductName,
                    SkuSnapshot         = item.SkuSnapshot,
                    SpecSnapshot        = item.SpecSnapshot,
                    Quantity            = item.Quantity,
                    UnitPrice           = item.Price,
                    DiscountAmount      = 0,
                    LineTotal           = item.Price * item.Quantity
                });
            }

            await _context.AddAsync(order);
            await _context.SaveChangesAsync();

            return order;
        }

        public async Task<Order> GetOrderByMaDonHangAsync(string OrderCode)
        {
            return await _context.Set<Order>()
                .Include(x => x.Items)
                .Include(x => x.User)
                .FirstOrDefaultAsync(x => x.OrderCode == OrderCode);
        }

        public async Task<IEnumerable<Order>> GetUserOrdersAsync(string userId)
        {
            return await _context.Set<Order>()
                .Include(x => x.Items)
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
                .FirstOrDefaultAsync(x => x.OrderCode == maOrder.ToString());

            if (order == null) return false;

            _context.RemoveRange(order.Items);
            _context.Remove(order);
            await _context.SaveChangesAsync();
            return true;
        }

        // ────────────────────────────────────────────────────────────
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
                "paid"     => PaymentStatus.Paid,
                "refunded" => PaymentStatus.Refunded,
                "failed"   => PaymentStatus.Failed,
                _          => PaymentStatus.Unpaid
            };
    }
}
