using HDKTech.Data;
using HDKTech.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HDKTech.Controllers
{
    [ApiController]
    [Route("api/analytics")]
    [Authorize(Policy = "RequireAdminArea")]
    public class AnalyticsApiController : ControllerBase
    {
        private readonly HDKTechContext _context;

        public AnalyticsApiController(HDKTechContext context) => _context = context;

        [HttpGet("revenue")]
        [Authorize(Policy = "RevenueAnalytics.Read")]
        public async Task<IActionResult> Revenue([FromQuery] string? start, [FromQuery] string? end)
        {
            var startDate = DateTime.TryParse(start, out var sd) ? sd.Date : DateTime.Today.AddDays(-6);
            var endDate   = DateTime.TryParse(end,   out var ed) ? ed.Date : DateTime.Today;
            var endDateInclusive = endDate.AddDays(1);

            // ── Load tất cả đơn trong khoảng ngày ────────────────────
            var allOrders = await _context.Orders
                .Include(o => o.User)
                .Where(o => o.OrderDate >= startDate && o.OrderDate < endDateInclusive)
                .AsNoTracking()
                .ToListAsync();

            var deliveredOrders = allOrders.Where(o => o.Status == OrderStatus.Delivered).ToList();

            // ── Load order items của đơn đã giao (kèm product/category/brand) ─
            var deliveredIds = deliveredOrders.Select(o => o.Id).ToHashSet();
            var items = await _context.OrderItems
                .Include(oi => oi.Variant)
                    .ThenInclude(v => v!.Product)
                        .ThenInclude(p => p.Category)
                .Include(oi => oi.Variant)
                    .ThenInclude(v => v!.Product)
                        .ThenInclude(p => p.Brand)
                .Where(oi => deliveredIds.Contains(oi.OrderId))
                .AsNoTracking()
                .ToListAsync();

            // ── Timeline: gom theo ngày hoặc tháng ───────────────────
            var totalDays = (endDate - startDate).Days + 1;
            var groupByMonth = totalDays > 31;

            string LabelFor(DateTime d) => groupByMonth
                ? $"T{d.Month}/{d.Year % 100}"
                : $"{d.Day}/{d.Month}";

            var revenueByDay = deliveredOrders
                .GroupBy(o => o.OrderDate.Date)
                .ToDictionary(g => g.Key, g => (rev: g.Sum(o => o.TotalAmount), cnt: g.Count()));

            var labels        = new List<string>();
            var revenueList   = new List<decimal>();
            var ordersList    = new List<int>();

            for (var d = startDate; d <= endDate; d = d.AddDays(1))
            {
                var label = LabelFor(d);
                if (!labels.Contains(label)) { labels.Add(label); revenueList.Add(0); ordersList.Add(0); }
                var idx = labels.IndexOf(label);
                if (revenueByDay.TryGetValue(d, out var day))
                {
                    revenueList[idx] += day.rev;
                    ordersList[idx]  += day.cnt;
                }
            }

            var totalRevenue    = deliveredOrders.Sum(o => o.TotalAmount);
            var totalDelivered  = deliveredOrders.Count;
            var totalOrders     = allOrders.Count;
            var aov             = totalDelivered > 0 ? Math.Round(totalRevenue / totalDelivered, 0) : 0;
            var completionRate  = totalOrders > 0 ? Math.Round((double)totalDelivered / totalOrders * 100, 1) : 0.0;

            // ── Khách hàng mới (đăng ký trong kỳ) ────────────────────
            var newCustomers = await _context.Users
                .CountAsync(u => u.CreatedAt >= startDate && u.CreatedAt < endDateInclusive);

            // ── Tổng sản phẩm bán được ────────────────────────────────
            var totalProductsSold = items.Sum(oi => oi.Quantity);

            // ── Doanh thu theo danh mục ───────────────────────────────
            var categories = items
                .Where(oi => oi.Variant?.Product?.Category != null)
                .GroupBy(oi => oi.Variant!.Product.Category.Name)
                .Select(g => new { name = g.Key, revenue = g.Sum(oi => oi.LineTotal) })
                .OrderByDescending(x => x.revenue)
                .Take(6)
                .ToList();

            // ── Phương thức thanh toán ────────────────────────────────
            var payments = deliveredOrders
                .GroupBy(o => o.PaymentMethod)
                .Select(g => new { name = g.Key, count = g.Count() })
                .OrderByDescending(x => x.count)
                .ToList();

            // ── Top sản phẩm bán chạy ─────────────────────────────────
            var topProducts = items
                .Where(oi => oi.Variant?.Product != null)
                .GroupBy(oi => new
                {
                    Name         = oi.Variant!.Product.Name,
                    CategoryName = oi.Variant.Product.Category?.Name ?? ""
                })
                .Select(g => new
                {
                    name     = g.Key.Name,
                    category = g.Key.CategoryName,
                    revenue  = g.Sum(oi => oi.LineTotal),
                    qty      = g.Sum(oi => oi.Quantity)
                })
                .OrderByDescending(x => x.revenue)
                .Take(5)
                .ToList();

            // ── Đơn hàng gần đây (không lọc theo kỳ) ─────────────────
            var recentOrders = await _context.Orders
                .Include(o => o.User)
                .OrderByDescending(o => o.OrderDate)
                .Take(6)
                .Select(o => new
                {
                    code     = o.OrderCode,
                    customer = o.RecipientName,
                    amount   = o.TotalAmount,
                    status   = o.Status.ToString()
                })
                .AsNoTracking()
                .ToListAsync();

            // ── Doanh thu theo thương hiệu ────────────────────────────
            var brands = items
                .Where(oi => oi.Variant?.Product?.Brand != null)
                .GroupBy(oi => oi.Variant!.Product.Brand!.Name)
                .Select(g => new { name = g.Key, revenue = g.Sum(oi => oi.LineTotal) })
                .OrderByDescending(x => x.revenue)
                .Take(9)
                .ToList();

            // ── Doanh thu trung bình theo thứ trong tuần ─────────────
            var weekCount = new int[7];
            for (var d = startDate; d <= endDate; d = d.AddDays(1))
                weekCount[(int)d.DayOfWeek]++;

            var weekdayRevMap = deliveredOrders
                .GroupBy(o => (int)o.OrderDate.DayOfWeek)
                .ToDictionary(g => g.Key, g => g.Sum(o => o.TotalAmount));

            var weekdayRevenue = Enumerable.Range(0, 7)
                .Select(d => weekCount[d] > 0 && weekdayRevMap.TryGetValue(d, out var v)
                    ? Math.Round(v / weekCount[d], 0)
                    : 0m)
                .ToList();

            return Ok(new
            {
                labels,
                revenue          = revenueList,
                orders           = ordersList,
                totalRevenue,
                deliveredOrders  = totalDelivered,
                totalOrders,
                aov,
                newCustomers,
                totalProductsSold,
                completionRate,
                categories,
                payments,
                topProducts,
                recentOrders,
                brands,
                weekdayRevenue
            });
        }
    }
}
