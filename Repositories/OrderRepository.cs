using HDKTech.Data;
using HDKTech.Models;
using HDKTech.Repositories.Interfaces;
using HDKTech.Services;
using Microsoft.EntityFrameworkCore;

namespace HDKTech.Repositories
{
    public class OrderRepository : GenericRepository<Order>, IOrderRepository
    {
        // ── Giai đoạn 1: Inventory Sync — inject IInventoryService ──────────
        private readonly IInventoryService _inventoryService;

        public OrderRepository(HDKTechContext context, IInventoryService inventoryService) : base(context)
        {
            _inventoryService = inventoryService;
        }

        public async Task<Order> CreateOrderAsync(string userId, string RecipientName, string soDienThoai,
                                                    string ShippingAddress, List<CartItem> items, decimal ShippingFee = 0)
        {
            // ── [1] Tính tổng tiền ───────────────────────────────────────────
            var TotalAmount = items.Sum(x => x.Price * x.Quantity);

            // ── [2] Tạo mã đơn hàng unique: HDK + timestamp + random 4 digits ─
            var OrderCode = $"HDK{DateTime.Now:yyyyMMddHHmmss}_{Random.Shared.Next(1000, 9999)}";
            var retries = 3;
            while (retries-- > 0)
            {
                var existingOrder = await _context.Set<Order>()
                    .FirstOrDefaultAsync(x => x.OrderCode == OrderCode);
                if (existingOrder == null) break;
                OrderCode = $"HDK{DateTime.Now:yyyyMMddHHmmss}_{Random.Shared.Next(1000, 9999)}";
            }

            // ── [3] Khởi tạo Order entity ────────────────────────────────────
            var order = new Order
            {
                UserId          = userId,
                OrderCode       = OrderCode,
                RecipientName   = RecipientName,
                RecipientPhone  = soDienThoai,
                ShippingAddress = ShippingAddress,
                TotalAmount     = TotalAmount,
                ShippingFee     = ShippingFee,
                Status          = 0, // Chờ xác nhận
                OrderDate       = DateTime.Now,
                Items           = new List<OrderItem>()
            };

            foreach (var item in items)
            {
                order.Items.Add(new OrderItem
                {
                    ProductId = item.ProductId,
                    Quantity  = item.Quantity,
                    UnitPrice = item.Price
                });
            }

            // ── [4] DATABASE TRANSACTION: trừ kho + lưu đơn hàng nguyên tử ──
            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // [4a] ReserveStock: chỉ sửa entity trong DbContext (chưa SaveChanges)
                    var stockResult = await _inventoryService.ReserveStockAsync(items);
                    if (!stockResult.Success)
                        throw new InvalidOperationException(
                            $"Đặt hàng thất bại — không đủ tồn kho: {stockResult.Message}");

                    // [4b] Lưu Order + inventory thay đổi trong 1 lần SaveChanges
                    await _context.AddAsync(order);
                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();
                    return order;
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });
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
            var Order = await _context.Set<Order>().FindAsync(maOrder);
            if (Order == null)
                return false;

            Order.Status = trangThaiMoi;
            _context.Update(Order);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteOrderAsync(int maOrder)
        {
            var Order = await _context.Set<Order>()
                .Include(x => x.Items)
                .FirstOrDefaultAsync(x => x.OrderCode == maOrder.ToString());

            if (Order == null)
                return false;

            _context.RemoveRange(Order.Items);
            _context.Remove(Order);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}


