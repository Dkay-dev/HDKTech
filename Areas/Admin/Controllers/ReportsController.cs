using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using HDKTech.Services;

namespace HDKTech.Areas.Admin.Controllers
{
    /// <summary>
    /// Giai đoạn 3 — Smart Reporting
    /// Controller xuất báo cáo Excel cho Admin.
    /// Được bảo vệ bởi Policy "Report.Export".
    /// </summary>
    [Area("Admin")]
    [Authorize(Policy = "RequireManager")]      // Fallback bảo vệ toàn Controller
    [Authorize(Policy = "Report.Export")]       // Granular Security — GĐ1
    [Route("admin/[controller]")]
    public class ReportsController : Controller
    {
        private readonly IReportService  _reportService;
        private readonly ISystemLogService _logService;
        private readonly ILogger<ReportsController> _logger;

        public ReportsController(
            IReportService reportService,
            ISystemLogService logService,
            ILogger<ReportsController> logger)
        {
            _reportService = reportService;
            _logService    = logService;
            _logger        = logger;
        }

        // ─────────────────────────────────────────────────────────────────────
        // INDEX — Trang báo cáo với form chọn ngày
        // GET: /admin/reports
        // ─────────────────────────────────────────────────────────────────────

        [HttpGet("")]
        [HttpGet("index")]
        public IActionResult Index()
        {
            // Mặc định: tháng hiện tại
            ViewBag.StartDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1)
                                    .ToString("yyyy-MM-dd");
            ViewBag.EndDate   = DateTime.Now.ToString("yyyy-MM-dd");
            return View();
        }

        // ─────────────────────────────────────────────────────────────────────
        // EXPORT REVENUE — Xuất Excel Doanh Thu
        // GET: /admin/reports/revenue?start=yyyy-MM-dd&end=yyyy-MM-dd
        // ─────────────────────────────────────────────────────────────────────

        [HttpGet("revenue")]
        public async Task<IActionResult> ExportRevenue(
            [FromQuery] DateTime? start,
            [FromQuery] DateTime? end)
        {
            try
            {
                var startDate = start ?? new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                var endDate   = end   ?? DateTime.Now;

                if (startDate > endDate)
                    return BadRequest("Ngày bắt đầu không thể lớn hơn ngày kết thúc.");

                var bytes = await _reportService.ExportRevenueExcelAsync(startDate, endDate);

                // Audit Log
                await _logService.LogActionAsync(
                    username:   User.Identity?.Name ?? "Admin",
                    actionType: "Export",
                    module:     "Reports",
                    description: $"Admin đã xuất báo cáo Doanh Thu " +
                                 $"({startDate:dd/MM/yyyy} – {endDate:dd/MM/yyyy})",
                    userRole:   User.IsInRole("Admin") ? "Admin" : "Manager",
                    userId:     User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                );

                var fileName = $"BaoCao_DoanhThu_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}.xlsx";
                return File(
                    bytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi xuất báo cáo Doanh Thu");
                TempData["Error"] = "Không thể xuất báo cáo. Vui lòng thử lại.";
                return RedirectToAction(nameof(Index));
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // EXPORT INVENTORY — Xuất Excel Tồn Kho
        // GET: /admin/reports/inventory
        // ─────────────────────────────────────────────────────────────────────

        [HttpGet("inventory")]
        public async Task<IActionResult> ExportInventory()
        {
            try
            {
                var bytes = await _reportService.ExportInventoryExcelAsync();

                // Audit Log
                await _logService.LogActionAsync(
                    username:   User.Identity?.Name ?? "Admin",
                    actionType: "Export",
                    module:     "Reports",
                    description: "Admin đã xuất báo cáo Tồn Kho",
                    userRole:   User.IsInRole("Admin") ? "Admin" : "Manager",
                    userId:     User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                );

                var fileName = $"BaoCao_TonKho_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";
                return File(
                    bytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi xuất báo cáo Tồn Kho");
                TempData["Error"] = "Không thể xuất báo cáo. Vui lòng thử lại.";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
