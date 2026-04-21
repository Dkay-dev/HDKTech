using HDKTech.Areas.Admin.Services.Interfaces;
using HDKTech.Data;
using HDKTech.Models;
using HDKTech.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HDKTech.Areas.Admin.Services
{
    public class OrderAdminService : IOrderAdminService
    {
        private readonly HDKTechContext   _context;
        private readonly IInventoryService _inventoryService;
        private readonly ILogger<OrderAdminService> _logger;

        public OrderAdminService(
            HDKTechContext context,
            IInventoryService inventoryService,
            ILogger<OrderAdminService> logger)
        {
            _context          = context;
            _inventoryService = inventoryService;
            _logger           = logger;
        }

        public async Task<OrderListResult> GetOrdersPagedAsync(
            int page, int pageSize, string searchTerm, int statusFilter, string sortBy)
        {
            IQueryable<Order> query = _context.Orders
                .AsNoTracking()
                .Include(o => o.User)
                .Include(o => o.Items);

            if (!string.IsNullOrWhiteSpace(searchTerm))
                query = query.Where(o =>
                    o.OrderCode.Contains(searchTerm) ||
                    o.RecipientName.Contains(searchTerm) ||
                    o.RecipientPhone.Contains(searchTerm));

            if (statusFilter >= 0)
            {
                var s = (OrderStatus)statusFilter;
                query = query.Where(o => o.Status == s);
            }

            query = sortBy switch
            {
                "amount_high" => query.OrderByDescending(o => o.TotalAmount),
                "amount_low"  => query.OrderBy(o => o.TotalAmount),
                "customer"    => query.OrderBy(o => o.RecipientName),
                _             => query.OrderByDescending(o => o.OrderDate)
            };

            var totalCount = await query.CountAsync();
            var orders = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var stats = await _context.Orders.AsNoTracking()
                .GroupBy(o => o.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            var today        = DateTime.Now.Date;
            var todayRevenue = await _context.Orders.AsNoTracking()
                .Where(o => o.OrderDate.Date == today && o.Status == OrderStatus.Delivered)
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;
            var todayCount   = await _context.Orders.AsNoTracking()
                .Where(o => o.OrderDate.Date == today)
                .CountAsync();

            return new OrderListResult
            {
                Orders         = orders,
                TotalCount     = totalCount,
                PendingCount   = stats.FirstOrDefault(s => s.Status == OrderStatus.Pending)?.Count    ?? 0,
                ProcessingCount= stats.FirstOrDefault(s => s.Status == OrderStatus.Confirmed)?.Count  ?? 0,
                ShippingCount  = stats.FirstOrDefault(s => s.Status == OrderStatus.Shipping)?.Count   ?? 0,
                DeliveredCount = stats.FirstOrDefault(s => s.Status == OrderStatus.Delivered)?.Count  ?? 0,
                CancelledCount = stats.FirstOrDefault(s => s.Status == OrderStatus.Cancelled)?.Count  ?? 0,
                TodayRevenue   = todayRevenue,
                TodayOrderCount= todayCount
            };
        }

        public async Task<Order?> GetOrderDetailsAsync(int id)
            => await _context.Orders
                .AsNoTracking()
                .Include(o => o.User)
                .Include(o => o.Items)
                    .ThenInclude(i => i.Product)
                        .ThenInclude(p => p!.Images)
                .FirstOrDefaultAsync(o => o.Id == id);

        public async Task<(bool Success, string Message)> UpdateStatusAsync(
            int orderId, int newStatus, string username, string? userId)
        {
            if (!Enum.IsDefined(typeof(OrderStatus), newStatus))
                return (false, "Trạng thái không hợp lệ.");

            var order = await _context.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
                return (false, "Không tìm thấy đơn hàng.");

            var target = (OrderStatus)newStatus;

            if (order.Status == OrderStatus.Delivered &&
                target != OrderStatus.Cancelled && target != OrderStatus.Returned)
                return (false, "Đơn hàng đã giao không thể thay đổi trạng thái.");

            order.Status = target;
            if (target == OrderStatus.Delivered) order.DeliveredAt = DateTime.Now;
            await _context.SaveChangesAsync();

            return (true, $"Cập nhật trạng thái → \"{GetStatusName(target)}\" thành công.");
        }

        public async Task<(bool Success, string Message)> CancelOrderAsync(
            int orderId, string username, string? userId)
        {
            var order = await _context.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
                return (false, "Không tìm thấy đơn hàng.");

            if (order.Status == OrderStatus.Delivered)
                return (false, "Không thể hủy đơn hàng đã giao thành công.");

            if (order.Status == OrderStatus.Cancelled)
                return (false, "Đơn hàng đã bị hủy trước đó.");

            order.Status      = OrderStatus.Cancelled;
            order.CancelledAt = DateTime.Now;
            await _context.SaveChangesAsync();

            if (order.Items != null && order.Items.Any())
            {
                await _inventoryService.ReleaseStockAsync(
                    items   : order.Items.ToList(),
                    username: username,
                    userId  : userId);
            }

            return (true, "Đã hủy đơn hàng và hoàn kho thành công.");
        }

        public async Task<List<Order>> GetOrdersForExportAsync(string searchTerm, int statusFilter)
        {
            var query = _context.Orders.AsNoTracking().Include(o => o.User).AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
                query = query.Where(o =>
                    o.OrderCode.Contains(searchTerm) ||
                    o.RecipientName.Contains(searchTerm));

            if (statusFilter >= 0)
            {
                var s = (OrderStatus)statusFilter;
                query = query.Where(o => o.Status == s);
            }

            return await query.OrderByDescending(o => o.OrderDate).ToListAsync();
        }

        public static string GetStatusName(OrderStatus status) => status switch
        {
            OrderStatus.Pending   => "Chờ xác nhận",
            OrderStatus.Confirmed => "Đã xác nhận",
            OrderStatus.Packing   => "Đang đóng gói",
            OrderStatus.Shipping  => "Đang giao",
            OrderStatus.Delivered => "Đã giao",
            OrderStatus.Cancelled => "Đã hủy",
            OrderStatus.Returned  => "Trả hàng",
            _                     => "Không xác định"
        };
    }
}
