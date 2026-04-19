using HDKTech.Areas.Admin.ViewModels;
using HDKTech.Data;
using HDKTech.Models;
using HDKTech.Services;   // LowStockProductItem
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace HDKTech.Areas.Admin.Services
{
    /// <summary>
    /// Module D — Performance: Dashboard queries chạy song song qua Task.WhenAll.
    /// Mỗi nhóm query nhận DbContext riêng từ IDbContextFactory để tránh
    /// shared-context race condition. Daily revenue dùng 1 GROUP BY query
    /// thay vì 7 queries riêng lẻ.
    /// </summary>
    public class DashboardService : IDashboardService
    {
        private readonly IDbContextFactory<HDKTechContext> _contextFactory;
        private readonly IMemoryCache                      _cache;
        private readonly ILogger<DashboardService>         _logger;

        // ── Cache keys ──────────────────────────────────────────────────────
        private const string MainCacheKey = "dashboard_v2_main";
        private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

        public DashboardService(
            IDbContextFactory<HDKTechContext> contextFactory,
            IMemoryCache                      cache,
            ILogger<DashboardService>         logger)
        {
            _contextFactory = contextFactory;
            _cache          = cache;
            _logger         = logger;
        }

        // ─────────────────────────────────────────────────────────────────────
        // PUBLIC: GetDashboardDataAsync — với caching 5 phút
        // ─────────────────────────────────────────────────────────────────────
        public async Task<DashboardViewModel> GetDashboardDataAsync()
            => await GetDashboardDataAsync(string.Empty);

        /// <summary>
        /// Overload role-aware: Staff chỉ nhận dữ liệu kho + đơn cần xử lý.
        /// Các role khác (Admin, Manager) nhận toàn bộ data được cache.
        /// </summary>
        public async Task<DashboardViewModel> GetDashboardDataAsync(string viewerRole)
        {
            // Staff: build nhanh, không cache (data nhẹ, không cần revenue)
            if (string.Equals(viewerRole, "Staff", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogDebug("[Dashboard] Staff mode — build warehouse-only data.");
                return await BuildWarehouseAsync(viewerRole);
            }

            // Admin / Manager: full data + cache 5 phút
            if (_cache.TryGetValue(MainCacheKey, out DashboardViewModel? cached) && cached != null)
            {
                _logger.LogDebug("[Dashboard] Trả về từ cache (còn hạn).");
                cached.ViewerRole = viewerRole;
                return cached;
            }

            _logger.LogInformation("[Dashboard] Cache miss — đang build từ DB song song...");
            var vm = await BuildAsync();
            vm.ViewerRole = viewerRole;

            _cache.Set(MainCacheKey, vm, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = CacheTtl,
                SlidingExpiration               = TimeSpan.FromMinutes(2),
                Priority                        = CacheItemPriority.Normal
            });

            return vm;
        }

        // ─────────────────────────────────────────────────────────────────────
        // PUBLIC: InvalidateCacheAsync
        // ─────────────────────────────────────────────────────────────────────
        public Task InvalidateCacheAsync()
        {
            _cache.Remove(MainCacheKey);
            _logger.LogInformation("[Dashboard] Cache đã bị xóa thủ công.");
            return Task.CompletedTask;
        }

        // ─────────────────────────────────────────────────────────────────────
        // PRIVATE: BuildWarehouseAsync — Staff-only (lightweight)
        // ─────────────────────────────────────────────────────────────────────
        private async Task<DashboardViewModel> BuildWarehouseAsync(string role)
        {
            const int lowStockThreshold = 10;
            var vm = new DashboardViewModel
            {
                CachedAt        = DateTime.Now,
                ShowRevenueData = false,
                ViewerRole      = role
            };

            try
            {
                await using var ctx = await _contextFactory.CreateDbContextAsync();

                var today    = DateTime.Now.Date;
                var tomorrow = today.AddDays(1);

                vm.PendingOrders = await ctx.Orders.AsNoTracking()
                    .CountAsync(o => o.Status == OrderStatus.Pending || o.Status == OrderStatus.Confirmed);

                vm.TotalOrders = await ctx.Orders.AsNoTracking().CountAsync();

                vm.TodayOrderCount = await ctx.Orders.AsNoTracking()
                    .CountAsync(o => o.OrderDate >= today && o.OrderDate < tomorrow);

                vm.LowStockCount = await ctx.Inventories.AsNoTracking()
                    .Where(i => i.Quantity < lowStockThreshold)
                    .CountAsync();

                vm.LowStockProducts = await ctx.Inventories
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

                vm.RecentOrders = await ctx.Orders.AsNoTracking()
                    .Include(o => o.User)
                    .Where(o => o.Status == OrderStatus.Pending || o.Status == OrderStatus.Confirmed)
                    .OrderByDescending(o => o.OrderDate)
                    .Take(5)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Dashboard] Lỗi build Staff data.");
            }

            return vm;
        }

        // ─────────────────────────────────────────────────────────────────────
        // PRIVATE: BuildAsync — 4 nhóm query chạy song song
        // ─────────────────────────────────────────────────────────────────────
        private async Task<DashboardViewModel> BuildAsync()
        {
            var vm    = new DashboardViewModel { CachedAt = DateTime.Now };
            var today = DateTime.Now.Date;
            var now   = DateTime.Now;

            try
            {
                // Chạy 4 nhóm độc lập song song — mỗi nhóm dùng DbContext riêng
                await Task.WhenAll(
                    LoadOrderStatsAsync(vm, today, now),
                    LoadInventoryStatsAsync(vm),
                    LoadUserAndPromoStatsAsync(vm, now),
                    LoadRecentAndBannerDataAsync(vm, today, now)
                );

                // Banner ROI cần AverageOrderValue đã load ở bước trên → chạy sau
                await BuildBannerRoiAsync(vm, today, today.AddDays(1));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Dashboard] Lỗi khi build dữ liệu.");
            }

            return vm;
        }

        // ── Nhóm 1: Order & Revenue stats ─────────────────────────────────
        private async Task LoadOrderStatsAsync(DashboardViewModel vm, DateTime today, DateTime now)
        {
            await using var ctx = await _contextFactory.CreateDbContextAsync();

            var tomorrow = today.AddDays(1);

            vm.TotalRevenue = await ctx.Orders
                .AsNoTracking()
                .Where(o => o.Status == OrderStatus.Delivered)
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

            vm.TotalOrders = await ctx.Orders.AsNoTracking().CountAsync();

            vm.PendingOrders = await ctx.Orders.AsNoTracking()
                .CountAsync(o => o.Status == OrderStatus.Pending || o.Status == OrderStatus.Confirmed);

            vm.TodayOrderCount = await ctx.Orders.AsNoTracking()
                .CountAsync(o => o.OrderDate >= today && o.OrderDate < tomorrow);

            vm.TodayRevenue = await ctx.Orders.AsNoTracking()
                .Where(o => o.Status == OrderStatus.Delivered
                    && o.OrderDate >= today
                    && o.OrderDate < tomorrow)
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

            vm.AverageOrderValue = await ctx.Orders.AsNoTracking()
                .Where(o => o.Status == OrderStatus.Delivered)
                .AverageAsync(o => (decimal?)o.TotalAmount) ?? 0;

            // Module D: thay 7 queries riêng lẻ bằng 1 GROUP BY duy nhất
            var sevenDaysAgo = today.AddDays(-6);
            var rawDaily = await ctx.Orders.AsNoTracking()
                .Where(o => o.Status == OrderStatus.Delivered
                         && o.OrderDate >= sevenDaysAgo
                         && o.OrderDate < tomorrow)
                .GroupBy(o => o.OrderDate.Date)
                .Select(g => new { Date = g.Key, Revenue = g.Sum(o => o.TotalAmount) })
                .ToListAsync();

            var dailyRevenue = new List<DailyRevenueData>();
            for (int i = 6; i >= 0; i--)
            {
                var date = today.AddDays(-i);
                var rev  = rawDaily.FirstOrDefault(r => r.Date == date)?.Revenue ?? 0;
                dailyRevenue.Add(new DailyRevenueData
                {
                    Date    = date,
                    DayName = date.ToString("ddd dd/MM",
                                new System.Globalization.CultureInfo("vi-VN")),
                    Revenue = rev
                });
            }
            vm.DailyRevenue = dailyRevenue;
        }

        // ── Nhóm 2: Inventory / Low-stock stats ───────────────────────────
        private async Task LoadInventoryStatsAsync(DashboardViewModel vm)
        {
            await using var ctx = await _contextFactory.CreateDbContextAsync();

            const int lowStockThreshold = 10;

            vm.LowStockCount = await ctx.Inventories.AsNoTracking()
                .Where(i => i.Quantity < lowStockThreshold)
                .CountAsync();

            vm.LowStockProducts = await ctx.Inventories
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
        }

        // ── Nhóm 3: Users + Promotions + Banner counts ────────────────────
        private async Task LoadUserAndPromoStatsAsync(DashboardViewModel vm, DateTime now)
        {
            await using var ctx = await _contextFactory.CreateDbContextAsync();

            var thirtyDaysAgo = now.AddDays(-30);

            vm.NewCustomers = await ctx.Users.AsNoTracking()
                .OfType<AppUser>()
                .Where(u => u.CreatedAt >= thirtyDaysAgo)
                .CountAsync();

            vm.ActivePromotions = await ctx.Promotions.AsNoTracking()
                .CountAsync(p => p.IsActive && p.StartDate <= now && p.EndDate >= now);

            vm.TotalBanners  = await ctx.Banners.AsNoTracking().CountAsync();
            vm.ActiveBanners = await ctx.Banners.AsNoTracking()
                .CountAsync(b => b.IsActive);
        }

        // ── Nhóm 4: Recent orders + Audit logs + Banner click totals ─────
        private async Task LoadRecentAndBannerDataAsync(
            DashboardViewModel vm, DateTime today, DateTime now)
        {
            await using var ctx = await _contextFactory.CreateDbContextAsync();

            var tomorrow = today.AddDays(1);

            vm.RecentOrders = await ctx.Orders.AsNoTracking()
                .Include(o => o.User)
                .OrderByDescending(o => o.OrderDate)
                .Take(5)
                .ToListAsync();

            var recentLogs = await ctx.SystemLogs.AsNoTracking()
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

            vm.TotalClicksToday = await ctx.BannerClickEvents.AsNoTracking()
                .CountAsync(e => e.ClickedAt >= today && e.ClickedAt < tomorrow);
        }

        // ─────────────────────────────────────────────────────────────────────
        // PRIVATE: Banner ROI — chạy sau khi AverageOrderValue đã có
        // ─────────────────────────────────────────────────────────────────────
        private async Task BuildBannerRoiAsync(
            DashboardViewModel vm,
            DateTime today,
            DateTime tomorrow)
        {
            try
            {
                await using var ctx = await _contextFactory.CreateDbContextAsync();

                var sevenDaysAgo = today.AddDays(-7);

                // Lấy thống kê click theo BannerId (1 query tổng hợp)
                var clickStats = await ctx.BannerClickEvents
                    .AsNoTracking()
                    .GroupBy(e => e.BannerId)
                    .Select(g => new
                    {
                        BannerId    = g.Key,
                        Total       = g.Count(),
                        Last7Days   = g.Count(e => e.ClickedAt >= sevenDaysAgo),
                        Today       = g.Count(e => e.ClickedAt >= today && e.ClickedAt < tomorrow),
                        UniqueReach = g.Select(e => e.UserIpAddress).Distinct().Count()
                    })
                    .ToListAsync();

                var activeBanners = await ctx.Banners
                    .AsNoTracking()
                    .Where(b => b.IsActive)
                    .ToListAsync();

                var conversionRate = 0.05m;
                var aov            = vm.AverageOrderValue;

                vm.TopBanners = activeBanners
                    .Select(b =>
                    {
                        var stats    = clickStats.FirstOrDefault(s => s.BannerId == b.Id);
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
