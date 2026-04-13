using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using HDKTech.Areas.Admin.Services;

namespace HDKTech.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Manager")]
    [Route("admin/[controller]")]
    public class DashboardController : Controller
    {
        private readonly IDashboardService _dashboardService;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(IDashboardService dashboardService, ILogger<DashboardController> logger)
        {
            _dashboardService = dashboardService;
            _logger = logger;
        }

        /// <summary>
        /// Dashboard - lấy dữ liệu thực qua DashboardService, truyền ViewModel đầy đủ ra View.
        /// GET: /admin/dashboard
        /// </summary>
        [HttpGet("")]
        [HttpGet("index")]
        public async Task<IActionResult> Index()
        {
            try
            {
                var vm = await _dashboardService.GetDashboardDataAsync();
                return View(vm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi tải Dashboard");
                TempData["Error"] = "Không thể tải dữ liệu Dashboard. Vui lòng thử lại.";
                return View(new Areas.Admin.ViewModels.DashboardViewModel());
            }
        }
    }
}
