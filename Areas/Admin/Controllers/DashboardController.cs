using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using HDKTech.Data;
using HDKTech.Models;
using HDKTech.Areas.Admin.Models;
using Microsoft.EntityFrameworkCore;

namespace HDKTech.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Manager")]
    [Route("admin/[controller]")]
    public class DashboardController : Controller
    {
        private readonly HDKTechContext _context;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(HDKTechContext context, ILogger<DashboardController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Dashboard với số liệu kinh doanh thực tế.
        /// GET: /admin/dashboard
        /// </summary>
        [HttpGet("")]
        [HttpGet("index")]
        public async Task<IActionResult> Index()
        {
            try
            {
                // ── Doanh thu: chỉ tính đơn hàng Status == 3 (Đã giao) ──
                var totalRevenue = await _context.Orders
                    .AsNoTracking()
                    .Where(o => o.Status == 3)
                    .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

                // ── Tổng đơn hàng ──────────────────────────────────────
                var totalOrders = await _context.Orders.AsNoTracking().CountAsync();

                // ── Đơn chờ xử lý (Status 0 + 1) ──────────────────────
                var pendingOrders = await _context.Orders.AsNoTracking()
                    .CountAsync(o => o.Status == 0 || o.Status == 1);

                // ── Tồn kho thấp (< 10) ────────────────────────────────
                var lowStockCount = await _context.Inventories.AsNoTracking()
                    .Where(i => i.Quantity < 10)
                    .CountAsync();

                // ── Khách hàng mới (30 ngày) ───────────────────────────
                var thirtyDaysAgo = DateTime.Now.AddDays(-30);
                var newCustomers = await _context.Users.AsNoTracking()
                    .OfType<AppUser>()
                    .Where(u => u.CreatedAt >= thirtyDaysAgo)
                    .CountAsync();

                // ── Khuyến mãi đang chạy ───────────────────────────────
                var now = DateTime.Now;
                var activePromotions = await _context.Promotions.AsNoTracking()
                    .CountAsync(p => p.IsActive && p.StartDate <= now && p.EndDate >= now);

                // ── 5 đơn hàng gần nhất ────────────────────────────────
                var recentOrders = await _context.Orders.AsNoTracking()
                    .Include(o => o.User)
                    .OrderByDescending(o => o.OrderDate)
                    .Take(5)
                    .ToListAsync();

                // ── Doanh thu 7 ngày (chỉ đơn Status==3) ──────────────
                var dailyRevenueData = new List<DailyRevenueData>();
                for (int i = 6; i >= 0; i--)
                {
                    var date    = DateTime.Now.AddDays(-i);
                    var dateOnly = date.Date;
                    var revenue = await _context.Orders.AsNoTracking()
                        .Where(o => o.Status == 3 && o.OrderDate.Date == dateOnly)
                        .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

                    dailyRevenueData.Add(new DailyRevenueData
                    {
                        Date    = dateOnly,
                        DayName = date.ToString("ddd"),
                        Revenue = revenue
                    });
                }

                ViewBag.TotalRevenue      = totalRevenue;
                ViewBag.TotalOrders       = totalOrders;
                ViewBag.PendingOrders     = pendingOrders;
                ViewBag.LowStockCount     = lowStockCount;
                ViewBag.NewCustomers      = newCustomers;
                ViewBag.ActivePromotions  = activePromotions;
                ViewBag.RecentOrders      = recentOrders;
                ViewBag.DailyRevenue      = dailyRevenueData;

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi tải dashboard");
                TempData["Error"] = "Lỗi khi tải dashboard.";
                return View();
            }
        }
    }

    public class DailyRevenueData
    {
        public DateTime Date { get; set; }
        public string DayName { get; set; }
        public decimal Revenue { get; set; }
    }
}

