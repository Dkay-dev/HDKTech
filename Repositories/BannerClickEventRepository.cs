using HDKTech.Data;
using HDKTech.Models;
using Microsoft.EntityFrameworkCore;

namespace HDKTech.Repositories
{
    public class BannerClickEventRepository
    {
        private readonly HDKTechContext _context;

        public BannerClickEventRepository(HDKTechContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Log a banner click event
        /// </summary>
        public async Task<BannerClickEvent> LogClickAsync(
            int bannerId, 
            string userIpAddress, 
            string userAgent, 
            string referer,
            int? userId = null,
            string bannerName = null,
            string bannerType = null)
        {
            var clickEvent = new BannerClickEvent
            {
                BannerId = bannerId,
                ClickedAt = DateTime.Now,
                UserIpAddress = userIpAddress,
                UserAgent = userAgent,
                Referer = referer,
                UserId = userId,
                BannerName = bannerName,
                BannerType = bannerType
            };

            _context.BannerClickEvents.Add(clickEvent);
            await _context.SaveChangesAsync();

            return clickEvent;
        }

        /// <summary>
        /// Get all clicks for a banner
        /// </summary>
        public async Task<List<BannerClickEvent>> GetBannerClicksAsync(int bannerId)
        {
            return await _context.BannerClickEvents
                .Where(c => c.BannerId == bannerId)
                .OrderByDescending(c => c.ClickedAt)
                .ToListAsync();
        }

        /// <summary>
        /// Get banner analytics data
        /// </summary>
        public async Task<BannerAnalytics> GetBannerAnalyticsAsync(int bannerId)
        {
            var clicks = await GetBannerClicksAsync(bannerId);
            var banner = await _context.Banners.FindAsync(bannerId);

            var today = DateTime.Now.Date;
            var last30Days = DateTime.Now.AddDays(-30).Date;

            return new BannerAnalytics
            {
                BannerId = bannerId,
                BannerName = banner?.TenBanner,
                BannerType = banner?.LoaiBanner,
                TotalClicks = clicks.Count,
                ClicksToday = clicks.Count(c => c.ClickedAt.Date == today),
                ClicksLast30Days = clicks.Count(c => c.ClickedAt.Date >= last30Days),
                UniqueIpAddresses = clicks.Select(c => c.UserIpAddress).Distinct().Count(),
                FirstClickDate = clicks.OrderBy(c => c.ClickedAt).FirstOrDefault()?.ClickedAt,
                LastClickDate = clicks.OrderByDescending(c => c.ClickedAt).FirstOrDefault()?.ClickedAt,
                ClicksByDate = clicks.GroupBy(c => c.ClickedAt.Date)
                    .Select(g => new ClicksByDateInfo { Date = g.Key, Count = g.Count() })
                    .OrderBy(x => x.Date)
                    .ToList()
            };
        }

        /// <summary>
        /// Get analytics for all active banners
        /// </summary>
        public async Task<List<BannerAnalytics>> GetAllBannerAnalyticsAsync()
        {
            var banners = await _context.Banners
                .Where(b => b.IsActive)
                .ToListAsync();

            var analyticsList = new List<BannerAnalytics>();
            foreach (var banner in banners)
            {
                var analytics = await GetBannerAnalyticsAsync(banner.MaBanner);
                analyticsList.Add(analytics);
            }

            return analyticsList.OrderByDescending(a => a.TotalClicks).ToList();
        }

        /// <summary>
        /// Get clicks for a date range
        /// </summary>
        public async Task<List<BannerClickEvent>> GetClicksByDateRangeAsync(
            int bannerId, 
            DateTime startDate, 
            DateTime endDate)
        {
            return await _context.BannerClickEvents
                .Where(c => c.BannerId == bannerId 
                    && c.ClickedAt >= startDate 
                    && c.ClickedAt <= endDate)
                .OrderByDescending(c => c.ClickedAt)
                .ToListAsync();
        }

        /// <summary>
        /// Clear old click events (older than 90 days)
        /// </summary>
        public async Task<int> ClearOldClicksAsync(int daysToKeep = 90)
        {
            var cutoffDate = DateTime.Now.AddDays(-daysToKeep);
            var oldClicks = await _context.BannerClickEvents
                .Where(c => c.ClickedAt < cutoffDate)
                .ToListAsync();

            _context.BannerClickEvents.RemoveRange(oldClicks);
            await _context.SaveChangesAsync();

            return oldClicks.Count;
        }
    }

    /// <summary>
    /// Analytics data for a single banner
    /// </summary>
    public class BannerAnalytics
    {
        public int BannerId { get; set; }
        public string BannerName { get; set; }
        public string BannerType { get; set; }
        public int TotalClicks { get; set; }
        public int ClicksToday { get; set; }
        public int ClicksLast30Days { get; set; }
        public int UniqueIpAddresses { get; set; }
        public DateTime? FirstClickDate { get; set; }
        public DateTime? LastClickDate { get; set; }
        public List<ClicksByDateInfo> ClicksByDate { get; set; } = new();
    }

    /// <summary>
    /// Helper class for clicks by date data
    /// </summary>
    public class ClicksByDateInfo
    {
        public DateTime Date { get; set; }
        public int Count { get; set; }
    }
}
