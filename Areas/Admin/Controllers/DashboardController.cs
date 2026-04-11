using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using HDKTech.Data;
using HDKTech.Models;
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
        /// Display admin dashboard with key metrics and analytics
        /// GET: /admin/dashboard
        /// </summary>
        [HttpGet]
        [Route("")]
        [Route("index")]
        public async Task<IActionResult> Index()
        {
            try
            {
                // Get total revenue (sum of all orders)
                var totalRevenue = await _context.DonHangs
                    .AsNoTracking()
                    .SumAsync(o => o.TongTien);

                // Get total orders count
                var totalOrders = await _context.DonHangs
                    .AsNoTracking()
                    .CountAsync();

                // Get low stock products (quantity < 10)
                var lowStockCount = await _context.KhoHangs
                    .AsNoTracking()
                    .Where(k => k.SoLuong < 10)
                    .CountAsync();

                // Get new customers (registered in last 30 days)
                var thirtyDaysAgo = DateTime.Now.AddDays(-30);
                var newCustomers = await _context.Users
                    .AsNoTracking()
                    .OfType<NguoiDung>()
                    .Where(u => u.NgayTao >= thirtyDaysAgo)
                    .CountAsync();

                // Get recent orders (last 5)
                var recentOrders = await _context.DonHangs
                    .AsNoTracking()
                    .Include(o => o.NguoiDung)
                    .OrderByDescending(o => o.NgayDatHang)
                    .Take(5)
                    .ToListAsync();

                // Daily revenue for the last 7 days
                var dailyRevenueData = new List<DailyRevenueData>();
                for (int i = 6; i >= 0; i--)
                {
                    var date = DateTime.Now.AddDays(-i);
                    var dateOnly = date.Date;
                    var revenue = await _context.DonHangs
                        .AsNoTracking()
                        .Where(o => o.NgayDatHang.Date == dateOnly)
                        .SumAsync(o => o.TongTien);

                    dailyRevenueData.Add(new DailyRevenueData
                    {
                        Date = dateOnly,
                        DayName = date.ToString("ddd"),
                        Revenue = revenue
                    });
                }

                ViewBag.TotalRevenue = totalRevenue;
                ViewBag.TotalOrders = totalOrders;
                ViewBag.LowStockCount = lowStockCount;
                ViewBag.NewCustomers = newCustomers;
                ViewBag.RecentOrders = recentOrders;
                ViewBag.DailyRevenue = dailyRevenueData;

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dashboard");
                TempData["Error"] = "Lỗi khi tải dashboard";
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
