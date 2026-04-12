using HDKTech.Areas.Admin.ViewModels;
using HDKTech.Data;
using HDKTech.Models;
using Microsoft.EntityFrameworkCore;

namespace HDKTech.Areas.Admin.Services
{
    /// <summary>
    /// Service xử lý toàn bộ logic nghiệp vụ cho Dashboard.
    /// Lấy dữ liệu THỰC từ HDKTechContext - không dùng hardcoded.
    /// </summary>
    public class DashboardService : IDashboardService
    {
        private readonly HDKTechContext _context;
        private readonly ILogger<DashboardService> _logger;

        public DashboardService(HDKTechContext context, ILogger<DashboardService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<DashboardViewModel> GetDashboardDataAsync()
        {
            var vm = new DashboardViewModel();

            try
            {
                // ── [1] Doanh thu: chỉ tính đơn Status == 3 (Đã giao thành công) ──
                vm.TotalRevenue = await _context.Orders
                    .AsNoTracking()
                    .Where(o => o.Status == 3)
                    .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

                // ── [2] Tổng đơn hàng ──────────────────────────────────────────────
                vm.TotalOrders = await _context.Orders.AsNoTracking().CountAsync();

                // ── [3] Đơn chờ xử lý (Chờ xác nhận = 0, Đã xác nhận = 1) ─────────
                vm.PendingOrders = await _context.Orders.AsNoTracking()
                    .CountAsync(o => o.Status == 0 || o.Status == 1);

                // ── [4] Tồn kho thấp ──────────────────────────────────────────────
                vm.LowStockCount = await _context.Inventories.AsNoTracking()
                    .Where(i => i.Quantity < 10)
                    .CountAsync();

                // ── [5] Khách hàng mới (30 ngày gần đây) ─────────────────────────
                var thirtyDaysAgo = DateTime.Now.AddDays(-30);
                vm.NewCustomers = await _context.Users.AsNoTracking()
                    .OfType<AppUser>()
                    .Where(u => u.CreatedAt >= thirtyDaysAgo)
                    .CountAsync();

                // ── [6] Khuyến mãi đang chạy ──────────────────────────────────────
                var now = DateTime.Now;
                vm.ActivePromotions = await _context.Promotions.AsNoTracking()
                    .CountAsync(p => p.IsActive && p.StartDate <= now && p.EndDate >= now);

                // ── [7] Thống kê Banner từ DB ──────────────────────────────────────
                vm.TotalBanners = await _context.Banners.AsNoTracking().CountAsync();
                vm.ActiveBanners = await _context.Banners.AsNoTracking()
                    .CountAsync(b => b.IsActive);

                // ── [8] 5 đơn hàng gần nhất ───────────────────────────────────────
                vm.RecentOrders = await _context.Orders.AsNoTracking()
                    .Include(o => o.User)
                    .OrderByDescending(o => o.OrderDate)
                    .Take(5)
                    .ToListAsync();

                // ── [9] 5 hành động mới nhất từ Audit Log (SystemLog) ─────────────
                var recentLogs = await _context.SystemLogs.AsNoTracking()
                    .OrderByDescending(l => l.CreatedAt)
                    .Take(5)
                    .ToListAsync();

                vm.RecentAuditLogs = recentLogs.Select(l => new RecentAuditLogItem
                {
                    Id          = l.Id,
                    Timestamp   = l.CreatedAt,
                    Username    = l.Username ?? l.UserId ?? "Hệ thống",
                    Action      = l.Action ?? "N/A",
                    Module      = l.LogLevel ?? "N/A",
                    Description = l.Description ?? "",
                    Status      = l.Status ?? "Success"
                }).ToList();

                // ── [10] Doanh thu 7 ngày gần nhất cho Chart.js ───────────────────
                var dailyRevenue = new List<DailyRevenueData>();
                for (int i = 6; i >= 0; i--)
                {
                    var date    = DateTime.Now.AddDays(-i);
                    var dateOnly = date.Date;
                    var revenue = await _context.Orders.AsNoTracking()
                        .Where(o => o.Status == 3 && o.OrderDate.Date == dateOnly)
                        .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

                    dailyRevenue.Add(new DailyRevenueData
                    {
                        Date    = dateOnly,
                        DayName = date.ToString("ddd dd/MM", new System.Globalization.CultureInfo("vi-VN")),
                        Revenue = revenue
                    });
                }
                vm.DailyRevenue = dailyRevenue;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tải dữ liệu Dashboard từ DashboardService");
            }

            return vm;
        }
    }
}
