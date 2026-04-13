using HDKTech.Areas.Admin.ViewModels;
using HDKTech.Data;
using HDKTech.Models;
using HDKTech.Services;   // LowStockProductItem
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace HDKTech.Areas.Admin.Services
{
    /// <summary>
    /// Giai đoạn 2 — Observability Engine.
    /// Tổng hợp dữ liệu từ Order, Product, Banner, SystemLog.
    /// Kết quả được cache 5 phút qua IMemoryCache để tránh query SQL nặng.
    /// </summary>
    public class DashboardService : IDashboardService
    {
        private readonly HDKTechContext          _context;
        private readonly IMemoryCache            _cache;
        private readonly ILogger<DashboardService> _logger;

        // ── Cache keys ──────────────────────────────────────────────────────
        private const string MainCacheKey   = "dashboard_v2_main";
        private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

        public DashboardService(
            HDKTechContext context,
            IMemoryCache cache,
            ILogger<DashboardService> logger)
        {
            _context = context;
            _cache   = cache;
            _logger  = logger;
        }

        // ─────────────────────────────────────────────────────────────────────
        // PUBLIC: GetDashboardDataAsync — với caching 5 phút
        // ─────────────────────────────────────────────────────────────────────
        public async Task<DashboardViewModel> GetDashboardDataAsync()
        {
            if (_cache.TryGetValue(MainCacheKey, out DashboardViewModel? cached) && cached != null)
            {
                _logger.LogDebug("[Dashboard] Trả về từ cache (còn hạn).");
                return cached;
            }

            _logger.LogInformation("[Dashboard] Cache miss — đang build từ DB...");
            var vm = await BuildAsync();

            _cache.Set(MainCacheKey, vm, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = CacheTtl,
                SlidingExpiration               = TimeSpan.FromMinutes(2),
                Priority                        = CacheItemPriority.Normal
            });

            return vm;
        }

        // ─────────────────────────────────────────────────────────────────────
        // PUBLIC: InvalidateCacheAsync — gọi sau tạo/hủy đơn
        // ─────────────────────────────────────────────────────────────────────
        public Task InvalidateCacheAsync()
        {
            _cache.Remove(MainCacheKey);
            _logger.LogInformation("[Dashboard] Cache đã bị xóa thủ công.");
            return Task.CompletedTask;
        }

        // ─────────────────────────────────────────────────────────────────────
        // PRIVATE: BuildAsync — logic tổng hợp dữ liệu thực
        // ─────────────────────────────────────────────────────────────────────
        private async Task<DashboardViewModel> BuildAsync()
        {
            var vm    = new DashboardViewModel { CachedAt = DateTime.Now };
            var today = DateTime.Now.Date;
            var now   = DateTime.Now;

            try
            {
                // ── [1] Doanh thu tổng (chỉ đơn Đã giao = Status 3) ─────────
                vm.TotalRevenue = await _context.Orders
                    .AsNoTracking()
                    .Where(o => o.Status == 3)
                    .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

                // ── [2] Tổng đơn hàng ────────────────────────────────────────
                vm.TotalOrders = await _context.Orders.AsNoTracking().CountAsync();

                // ── [3] Đơn chờ xử lý (Status 0 = Chờ xác nhận, 1 = Đang xử lý)
                vm.PendingOrders = await _context.Orders.AsNoTracking()
                    .CountAsync(o => o.Status == 0 || o.Status == 1);

                // ── [4a] Tồn kho thấp — count ────────────────────────────────
                const int lowStockThreshold = 5;
                vm.LowStockCount = await _context.Inventories.AsNoTracking()
                    .Where(i => i.Quantity < 10)
                    .CountAsync();

                // ── [4b] Giai đoạn 1 data: danh sách chi tiết cảnh báo đỏ ───
                vm.LowStockProducts = await _context.Inventories
                    .AsNoTracking()
                    .Include(i => i.Product)
                    .Where(i => i.Quantity < lowStockThreshold)
                    .OrderBy(i => i.Quantity)
                    .Take(10)
                    .Select(i => new LowStockProductItem
                    {
                        ProductId    = i.ProductId,
                        ProductName  = i.Product != null ? i.Product.Name : $"SP#{i.ProductId}",
                        CurrentStock = i.Quantity,
                        Threshold    = lowStockThreshold
                    })
                    .ToListAsync();

                // ── [5] Khách hàng mới (30 ngày) ─────────────────────────────
                var thirtyDaysAgo = now.AddDays(-30);
                vm.NewCustomers = await _context.Users.AsNoTracking()
                    .OfType<AppUser>()
                    .Where(u => u.CreatedAt >= thirtyDaysAgo)
                    .CountAsync();

                // ── [6] Khuyến mãi đang chạy ─────────────────────────────────
                vm.ActivePromotions = await _context.Promotions.AsNoTracking()
                    .CountAsync(p => p.IsActive && p.StartDate <= now && p.EndDate >= now);

                // ── [7] Banner stats ──────────────────────────────────────────
                vm.TotalBanners  = await _context.Banners.AsNoTracking().CountAsync();
                vm.ActiveBanners = await _context.Banners.AsNoTracking()
                    .CountAsync(b => b.IsActive);

                // ── [8] 5 đơn hàng gần nhất ──────────────────────────────────
                vm.RecentOrders = await _context.Orders.AsNoTracking()
                    .Include(o => o.User)
                    .OrderByDescending(o => o.OrderDate)
                    .Take(5)
                    .ToListAsync();

                // ── [9] 5 audit log gần nhất ─────────────────────────────────
                var recentLogs = await _context.SystemLogs.AsNoTracking()
                    .OrderByDescending(l => l.CreatedAt)
                    .Take(5)
                    .ToListAsync();

                vm.RecentAuditLogs = recentLogs.Select(l => new RecentAuditLogItem
                {
                    Id          = l.Id,
                    Timestamp   = l.CreatedAt,
                    Username    = l.Username ?? l.UserId ?? "Hệ thống",
                    Action      = l.Action   ?? "N/A",
                    Module      = l.LogLevel ?? "N/A",
                    Description = l.Description ?? "",
                    Status      = l.Status   ?? "Success"
                }).ToList();

                // ── [10] Doanh thu 7 ngày (cho Line Chart) ───────────────────
                var dailyRevenue = new List<DailyRevenueData>();
                for (int i = 6; i >= 0; i--)
                {
                    var date    = now.AddDays(-i);
                    var dateOnly = date.Date;
                    var nextDay  = dateOnly.AddDays(1);
                    var rev = await _context.Orders.AsNoTracking()
                        .Where(o => o.Status == 3
                            && o.OrderDate >= dateOnly
                            && o.OrderDate < nextDay)
                        .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

                    dailyRevenue.Add(new DailyRevenueData
                    {
                        Date    = dateOnly,
                        DayName = date.ToString("ddd dd/MM",
                                    new System.Globalization.CultureInfo("vi-VN")),
                        Revenue = rev
                    });
                }
                vm.DailyRevenue = dailyRevenue;

                // ── [11] Giai đoạn 2: Metrics hôm nay ───────────────────────
                var tomorrow = today.AddDays(1);

                vm.TodayOrderCount = await _context.Orders.AsNoTracking()
                    .CountAsync(o => o.OrderDate >= today && o.OrderDate < tomorrow);

                vm.TodayRevenue = await _context.Orders.AsNoTracking()
                    .Where(o => o.Status == 3
                        && o.OrderDate >= today
                        && o.OrderDate < tomorrow)
                    .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

                // ── [12] Giá trị đơn hàng trung bình (AOV) ──────────────────
                vm.AverageOrderValue = await _context.Orders.AsNoTracking()
                    .Where(o => o.Status == 3)
                    .AverageAsync(o => (decimal?)o.TotalAmount) ?? 0;

                // ── [13] Giai đoạn 2: Banner ROI — Top 3 ────────────────────
                await BuildBannerRoiAsync(vm, today, tomorrow);

                // ── [14] Tổng clicks banner hôm nay ──────────────────────────
                vm.TotalClicksToday = await _context.BannerClickEvents.AsNoTracking()
                    .CountAsync(e => e.ClickedAt >= today && e.ClickedAt < tomorrow);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Dashboard] Lỗi khi build dữ liệu.");
            }

            return vm;
        }

        // ─────────────────────────────────────────────────────────────────────
        // PRIVATE: Banner ROI — tính hiệu quả và ước tính doanh thu
        // ─────────────────────────────────────────────────────────────────────
        private async Task BuildBannerRoiAsync(
            DashboardViewModel vm,
            DateTime today,
            DateTime tomorrow)
        {
            try
            {
                var sevenDaysAgo = today.AddDays(-7);

                // [13a] Lấy thống kê click theo BannerId (1 query tổng hợp)
                var clickStats = await _context.BannerClickEvents
                    .AsNoTracking()
                    .GroupBy(e => e.BannerId)
                    .Select(g => new
                    {
                        BannerId       = g.Key,
                        Total          = g.Count(),
                        Last7Days      = g.Count(e => e.ClickedAt >= sevenDaysAgo),
                        Today          = g.Count(e => e.ClickedAt >= today && e.ClickedAt < tomorrow),
                        UniqueReach    = g.Select(e => e.UserIpAddress).Distinct().Count()
                    })
                    .ToListAsync();

                // [13b] Lấy active banners
                var activeBanners = await _context.Banners
                    .AsNoTracking()
                    .Where(b => b.IsActive)
                    .ToListAsync();

                // [13c] Join in-memory + tính EstimatedRevenue
                // Công thức: Clicks7D × Conv.Rate(5%) × AOV
                // Note: chính xác 100% cần session-level tracking
                var conversionRate = 0.05m;
                var aov            = vm.AverageOrderValue;

                vm.TopBanners = activeBanners
                    .Select(b =>
                    {
                        var stats = clickStats.FirstOrDefault(s => s.BannerId == b.Id);
                        var clicks7d = stats?.Last7Days ?? 0;
                        return new BannerRoiItem
                        {
                            BannerId         = b.Id,
                            BannerTitle      = b.Title ?? $"Banner #{b.Id}",
                            BannerType       = b.BannerType ?? "Main",
                            BannerUrl        = b.LinkUrl ?? "#",
                            TotalClicks      = stats?.Total       ?? 0,
                            ClicksLast7Days  = clicks7d,
                            ClicksToday      = stats?.Today       ?? 0,
                            UniqueReach      = stats?.UniqueReach ?? 0,
                            EstimatedRevenue = Math.Round(clicks7d * conversionRate * aov, 0)
                        };
                    })
                    .OrderByDescending(b => b.TotalClicks)
                    .Take(3)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[Dashboard] Không thể load Banner ROI — bỏ qua.");
                vm.TopBanners = new List<BannerRoiItem>();
            }
        }
    }
}
