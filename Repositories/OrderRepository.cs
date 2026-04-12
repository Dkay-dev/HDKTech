using HDKTech.Data;
using HDKTech.Models;
using HDKTech.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HDKTech.Repositories
{
    public class OrderRepository : GenericRepository<Order>, IOrderRepository
    {
        public OrderRepository(HDKTechContext context) : base(context)
        {
        }

        public async Task<Order> CreateOrderAsync(string userId, string RecipientName, string soDienThoai, 
                                                    string ShippingAddress, List<CartItem> items, decimal ShippingFee = 0)
        {
            // Tính tổng tiền
            var TotalAmount = items.Sum(x => x.Price * x.Quantity);

            // Tạo mã đơn hàng: HDK + timestamp + random 4 digits
            // Ví dụ: HDK20260410180530_4821
            var OrderCode = $"HDK{DateTime.Now:yyyyMMddHHmmss}_{Random.Shared.Next(1000, 9999)}";

            // Đảm bảo mã đơn hàng unique (retries 3 lần nếu trùng)
            var retries = 3;
            while (retries-- > 0)
            {
                var existingOrder = await _context.Set<Order>()
                    .FirstOrDefaultAsync(x => x.OrderCode == OrderCode);

                if (existingOrder == null)
                    break; // Mã unique, dùng được

                // Nếu trùng, tạo mã mới
                OrderCode = $"HDK{DateTime.Now:yyyyMMddHHmmss}_{Random.Shared.Next(1000, 9999)}";
            }

            var Order = new Order
            {
                UserId = userId,
                OrderCode = OrderCode,
                RecipientName = RecipientName,
                RecipientPhone = soDienThoai,
                ShippingAddress = ShippingAddress,
                TotalAmount = TotalAmount,
                ShippingFee = ShippingFee,
                Status = 0, // Chờ xác nhận
                OrderDate = DateTime.Now,
                Items = new List<OrderItem>()
            };

            // Thêm chi tiết đơn hàng
            foreach (var item in items)
            {
                var chiTiet = new OrderItem
                {
                    Id = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = item.Price
                };
                Order.Items.Add(chiTiet);
            }

            // Lưu vào database
            await _context.AddAsync(Order);
            await _context.SaveChangesAsync();

            return Order;
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


