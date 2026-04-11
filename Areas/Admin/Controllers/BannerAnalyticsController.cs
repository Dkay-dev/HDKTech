using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HDKTech.Repositories;

namespace HDKTech.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Manager")]
    [Route("Admin/[controller]")]
    public class BannerAnalyticsController : Controller
    {
        private readonly BannerClickEventRepository _clickRepository;
        private readonly BannerRepository _bannerRepository;
        private readonly ILogger<BannerAnalyticsController> _logger;

        public BannerAnalyticsController(
            BannerClickEventRepository clickRepository,
            BannerRepository bannerRepository,
            ILogger<BannerAnalyticsController> logger)
        {
            _clickRepository = clickRepository;
            _bannerRepository = bannerRepository;
            _logger = logger;
        }

        /// <summary>
        /// Display overall banner analytics dashboard
        /// </summary>
        [HttpGet]
        [Route("")]
        [Route("Index")]
        public async Task<IActionResult> Index()
        {
            try
            {
                var analytics = await _clickRepository.GetAllBannerAnalyticsAsync();
                return View(analytics);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error loading banner analytics: {ex.Message}");
                TempData["Error"] = "Lỗi tải dữ liệu phân tích";
                return RedirectToAction("Index", "Banner");
            }
        }

        /// <summary>
        /// Display detailed analytics for a specific banner
        /// </summary>
        [HttpGet("details/{bannerId}")]
        public async Task<IActionResult> Details(int bannerId)
        {
            try
            {
                var banner = await _bannerRepository.GetBannerByIdAsync(bannerId);
                if (banner == null)
                {
                    TempData["Error"] = "Banner không tồn tại";
                    return RedirectToAction("Index");
                }

                var analytics = await _clickRepository.GetBannerAnalyticsAsync(bannerId);
                
                // Store banner info in ViewBag for display
                ViewBag.Banner = banner;
                
                return View(analytics);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error loading banner details: {ex.Message}");
                TempData["Error"] = "Lỗi tải chi tiết phân tích";
                return RedirectToAction("Index");
            }
        }

        /// <summary>
        /// Display click events for a banner with pagination
        /// </summary>
        [HttpGet("clicks/{bannerId}")]
        public async Task<IActionResult> ClickEvents(int bannerId, int page = 1, int pageSize = 50)
        {
            try
            {
                var banner = await _bannerRepository.GetBannerByIdAsync(bannerId);
                if (banner == null)
                {
                    TempData["Error"] = "Banner không tồn tại";
                    return RedirectToAction("Index");
                }

                var clicks = await _clickRepository.GetBannerClicksAsync(bannerId);

                // Pagination
                var totalItems = clicks.Count;
                var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
                var paginatedClicks = clicks
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                ViewBag.Banner = banner;
                ViewBag.Page = page;
                ViewBag.PageSize = pageSize;
                ViewBag.TotalPages = totalPages;
                ViewBag.TotalItems = totalItems;

                return View(paginatedClicks);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error loading click events: {ex.Message}");
                TempData["Error"] = "Lỗi tải sự kiện click";
                return RedirectToAction("Index");
            }
        }

        /// <summary>
        /// Display analytics for a date range
        /// </summary>
        [HttpPost("date-range")]
        public async Task<IActionResult> DateRangeReport(int bannerId, DateTime startDate, DateTime endDate)
        {
            try
            {
                var banner = await _bannerRepository.GetBannerByIdAsync(bannerId);
                if (banner == null)
                {
                    TempData["Error"] = "Banner không tồn tại";
                    return RedirectToAction("Index");
                }

                if (startDate > endDate)
                {
                    TempData["Error"] = "Ngày bắt đầu không được sau ngày kết thúc";
                    return RedirectToAction("Details", new { bannerId });
                }

                var clicks = await _clickRepository.GetClicksByDateRangeAsync(bannerId, startDate, endDate);

                ViewBag.Banner = banner;
                ViewBag.StartDate = startDate;
                ViewBag.EndDate = endDate;
                ViewBag.TotalClicks = clicks.Count;
                ViewBag.UniqueIps = clicks.Select(c => c.UserIpAddress).Distinct().Count();

                return View("DateRangeReport", clicks);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error generating date range report: {ex.Message}");
                TempData["Error"] = "Lỗi tạo báo cáo theo ngày";
                return RedirectToAction("Index");
            }
        }

        /// <summary>
        /// Export analytics data as CSV
        /// </summary>
        [HttpGet("export/{bannerId}")]
        public async Task<IActionResult> ExportAnalytics(int bannerId)
        {
            try
            {
                var banner = await _bannerRepository.GetBannerByIdAsync(bannerId);
                if (banner == null)
                {
                    TempData["Error"] = "Banner không tồn tại";
                    return RedirectToAction("Index");
                }

                var clicks = await _clickRepository.GetBannerClicksAsync(bannerId);

                // Create CSV content
                var csv = "Mã Banner,Tên Banner,Thời Gian Click,IP Address,User Agent\n";
                foreach (var click in clicks)
                {
                    csv += $"{click.BannerId},\"{banner.TenBanner}\",\"{click.ClickedAt:yyyy-MM-dd HH:mm:ss}\",\"{click.UserIpAddress}\",\"{click.UserAgent}\"\n";
                }

                var bytes = System.Text.Encoding.UTF8.GetBytes(csv);
                return File(bytes, "text/csv", $"banner-analytics-{bannerId}-{DateTime.Now:yyyyMMdd}.csv");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error exporting analytics: {ex.Message}");
                TempData["Error"] = "Lỗi xuất dữ liệu";
                return RedirectToAction("Index");
            }
        }

        /// <summary>
        /// Clean old click events
        /// </summary>
        [HttpPost("cleanup")]
        public async Task<IActionResult> CleanupOldData(int daysToKeep = 90)
        {
            try
            {
                var deletedCount = await _clickRepository.ClearOldClicksAsync(daysToKeep);
                TempData["Success"] = $"Đã xóa {deletedCount} sự kiện click cũ hơn {daysToKeep} ngày";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error cleaning up data: {ex.Message}");
                TempData["Error"] = "Lỗi xóa dữ liệu cũ";
                return RedirectToAction("Index");
            }
        }
    }
}
