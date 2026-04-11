using HDKTech.Models;
using HDKTech.Repositories;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace HDKTech.Controllers
{
    /// <summary>
    /// API endpoint for banner analytics tracking
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class BannerAnalyticsController : ControllerBase
    {
        private readonly BannerClickEventRepository _clickEventRepository;
        private readonly BannerRepository _bannerRepository;
        private readonly ILogger<BannerAnalyticsController> _logger;

        public BannerAnalyticsController(
            BannerClickEventRepository clickEventRepository,
            BannerRepository bannerRepository,
            ILogger<BannerAnalyticsController> logger)
        {
            _clickEventRepository = clickEventRepository;
            _bannerRepository = bannerRepository;
            _logger = logger;
        }

        /// <summary>
        /// Log a banner click event
        /// POST /api/banneranalytics/click
        /// </summary>
        [HttpPost("click")]
        public async Task<IActionResult> LogBannerClick([FromBody] BannerClickRequest request)
        {
            if (request == null || request.BannerId <= 0)
            {
                return BadRequest(new { success = false, message = "Invalid banner ID" });
            }

            try
            {
                // Get banner info
                var banner = await _bannerRepository.GetBannerByIdAsync(request.BannerId);
                if (banner == null)
                {
                    return NotFound(new { success = false, message = "Banner not found" });
                }

                // Get user info
                var userId = User.Identity?.IsAuthenticated == true ? User.FindFirst("sub")?.Value : null;
                var userIpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
                var userAgent = Request.Headers["User-Agent"].ToString();
                var referer = Request.Headers["Referer"].ToString();

                // Log the click
                var clickEvent = await _clickEventRepository.LogClickAsync(
                    bannerId: request.BannerId,
                    userIpAddress: userIpAddress,
                    userAgent: userAgent,
                    referer: referer,
                    userId: string.IsNullOrEmpty(userId) ? null : int.TryParse(userId, out var id) ? id : null,
                    bannerName: banner.TenBanner,
                    bannerType: banner.LoaiBanner
                );

                _logger.LogInformation($"Banner click logged: {banner.TenBanner} (ID: {request.BannerId}) from {userIpAddress}");

                return Ok(new
                {
                    success = true,
                    message = "Click logged successfully",
                    clickId = clickEvent.Id,
                    timestamp = clickEvent.ClickedAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error logging banner click: {ex.Message}");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Track banner click (alternative endpoint for JavaScript)
        /// POST /api/banneranalytics/track-click/{bannerId}
        /// </summary>
        [HttpPost("track-click/{bannerId}")]
        public async Task<IActionResult> TrackClick(int bannerId)
        {
            if (bannerId <= 0)
            {
                return BadRequest(new { success = false, message = "Invalid banner ID" });
            }

            try
            {
                // Get banner info
                var banner = await _bannerRepository.GetBannerByIdAsync(bannerId);
                if (banner == null)
                {
                    return NotFound(new { success = false, message = "Banner not found" });
                }

                // Get user info
                var userIpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
                var userAgent = Request.Headers["User-Agent"].ToString();
                var referer = Request.Headers["Referer"].ToString();
                var userId = User.Identity?.IsAuthenticated == true ? User.FindFirst("sub")?.Value : null;

                // Log the click
                var clickEvent = await _clickEventRepository.LogClickAsync(
                    bannerId: bannerId,
                    userIpAddress: userIpAddress,
                    userAgent: userAgent,
                    referer: referer,
                    userId: string.IsNullOrEmpty(userId) ? null : int.TryParse(userId, out var id) ? id : null,
                    bannerName: banner.TenBanner,
                    bannerType: banner.LoaiBanner
                );

                _logger.LogInformation($"Banner click tracked: {banner.TenBanner} (ID: {bannerId}) from {userIpAddress}");

                return Ok(new
                {
                    success = true,
                    message = "Click tracked successfully",
                    clickId = clickEvent.Id,
                    timestamp = clickEvent.ClickedAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error tracking banner click: {ex.Message}");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get banner analytics
        /// GET /api/banneranalytics/{bannerId}
        /// </summary>
        [HttpGet("{bannerId}")]
        public async Task<IActionResult> GetBannerAnalytics(int bannerId)
        {
            try
            {
                var analytics = await _clickEventRepository.GetBannerAnalyticsAsync(bannerId);
                return Ok(new { success = true, data = analytics });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting banner analytics: {ex.Message}");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get all banners analytics
        /// GET /api/banneranalytics/all
        /// </summary>
        [HttpGet("all")]
        public async Task<IActionResult> GetAllBannersAnalytics()
        {
            try
            {
                var analyticsList = await _clickEventRepository.GetAllBannerAnalyticsAsync();
                return Ok(new { success = true, data = analyticsList });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting all banners analytics: {ex.Message}");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }
    }

    /// <summary>
    /// Request model for banner click
    /// </summary>
    public class BannerClickRequest
    {
        public int BannerId { get; set; }
    }
}
