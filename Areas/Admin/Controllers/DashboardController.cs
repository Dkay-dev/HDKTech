using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using HDKTech.Data;
using HDKTech.Models;
using Microsoft.EntityFrameworkCore;

using HDKTech.Areas.Admin.Repositories;

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
                var totalRevenue = await _context.Orders
                    .AsNoTracking()
                    .SumAsync(o => o.TotalAmount);

                // Get total orders count
                var totalOrders = await _context.Orders
                    .AsNoTracking()
                    .CountAsync();

                // Get low stock products (quantity < 10)
                var lowStockCount = await _context.Inventories
                    .AsNoTracking()
                    .Where(k => k.Quantity < 10)
                    .CountAsync();

                // Get new customers (registered in last 30 days)
                var thirtyDaysAgo = DateTime.Now.AddDays(-30);
                var newCustomers = await _context.Users
                    .AsNoTracking()
                    .OfType<AppUser>()
                    .Where(u => u.CreatedAt >= thirtyDaysAgo)
                    .CountAsync();

                // Get recent orders (last 5)
                var recentOrders = await _context.Orders
                    .AsNoTracking()
                    .Include(o => o.User)
                    .OrderByDescending(o => o.OrderDate)
                    .Take(5)
                    .ToListAsync();

                // Daily revenue for the last 7 days
                var dailyRevenueData = new List<DailyRevenueData>();
                for (int i = 6; i >= 0; i--)
                {
                    var date = DateTime.Now.AddDays(-i);
                    var dateOnly = date.Date;
                    var revenue = await _context.Orders
                        .AsNoTracking()
                        .Where(o => o.OrderDate.Date == dateOnly)
                        .SumAsync(o => o.TotalAmount);

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

