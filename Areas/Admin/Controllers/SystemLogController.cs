using Microsoft.AspNetCore.Mvc;
using HDKTech.Repositories;

namespace HDKTech.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("admin/[controller]")]
    public class SystemLogController : Controller
    {
        private readonly ISystemLogRepository _logRepository;

        public SystemLogController(ISystemLogRepository logRepository)
        {
            _logRepository = logRepository;
        }

        /// <summary>
        /// Hiển thị danh sách logs
        /// </summary>
        [HttpGet("")]
        [HttpGet("Index")]
        public async Task<IActionResult> Index(int page = 1, string search = null, 
            string actionType = null, string module = null, string username = null)
        {
            const int pageSize = 10;

            // Lấy danh sách logs
            var (logs, total) = await _logRepository.SearchLogsAsync(
                searchText: search,
                actionType: actionType,
                module: module,
                username: username,
                pageNumber: page,
                pageSize: pageSize
            );

            // Lấy stats
            var stats = await _logRepository.GetTodayStatsAsync();
            var totalLogs = await _logRepository.GetTotalLogsAsync();

            // Tính toán phần trăm tăng (mock)
            var percentIncrease = 12; // Mock data

            ViewBag.Logs = logs;
            ViewBag.TotalLogs = totalLogs;
            ViewBag.TodayStats = stats;
            ViewBag.PercentIncrease = percentIncrease;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)total / pageSize);
            ViewBag.PageSize = pageSize;
            ViewBag.TotalCount = total;
            ViewBag.SearchText = search;
            ViewBag.SelectedActionType = actionType;
            ViewBag.SelectedModule = module;
            ViewBag.SelectedUsername = username;

            // Lấy danh sách filter options
            var modules = await _logRepository.GetModulesAsync();
            var actionTypes = await _logRepository.GetActionTypesAsync();
            var usernames = await _logRepository.GetUsernamesAsync();

            ViewBag.Modules = modules;
            ViewBag.ActionTypes = actionTypes;
            ViewBag.Usernames = usernames;

            return View();
        }

        /// <summary>
        /// Xem chi tiết log
        /// </summary>
        [HttpGet("Details/{id}")]
        public async Task<IActionResult> Details(int id)
        {
            var log = await _logRepository.GetLogByIdAsync(id);
            if (log == null)
                return NotFound();

            return Ok(log);
        }

        /// <summary>
        /// API: Lấy logs phân trang (AJAX)
        /// </summary>
        [HttpGet("api/logs")]
        public async Task<IActionResult> GetLogs(int page = 1, int pageSize = 10, 
            string search = null, string actionType = null, string module = null)
        {
            var (logs, total) = await _logRepository.SearchLogsAsync(
                searchText: search,
                actionType: actionType,
                module: module,
                pageNumber: page,
                pageSize: pageSize
            );

            return Ok(new
            {
                success = true,
                data = logs,
                pagination = new
                {
                    currentPage = page,
                    pageSize = pageSize,
                    totalCount = total,
                    totalPages = (int)Math.Ceiling((double)total / pageSize)
                }
            });
        }

        /// <summary>
        /// API: Lấy stats (AJAX)
        /// </summary>
        [HttpGet("api/stats")]
        public async Task<IActionResult> GetStats()
        {
            var stats = await _logRepository.GetTodayStatsAsync();
            var totalLogs = await _logRepository.GetTotalLogsAsync();

            return Ok(new
            {
                success = true,
                data = new
                {
                    totalLogs = totalLogs,
                    todayActions = stats.totalToday,
                    loginCount = stats.loginCount,
                    createCount = stats.createCount,
                    updateCount = stats.updateCount,
                    deleteCount = stats.deleteCount,
                    securityAlerts = 24 // Mock data
                }
            });
        }

        /// <summary>
        /// API: Export logs to CSV
        /// </summary>
        [HttpGet("export")]
        public async Task<IActionResult> Export(string search = null, 
            string actionType = null, string module = null, string username = null)
        {
            var (logs, _) = await _logRepository.SearchLogsAsync(
                searchText: search,
                actionType: actionType,
                module: module,
                username: username,
                pageNumber: 1,
                pageSize: 10000 // Export tất cả
            );

            var csv = GenerateCsv(logs);
            var fileName = $"SystemLogs_{DateTime.Now:yyyyMMdd_HHmmss}.csv";

            return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", fileName);
        }

        private string GenerateCsv(List<Models.SystemLog> logs)
        {
            var csv = "Thời gian,Người thực hiện,Hành động,Phần hành,Chi tiết,IP Address,Trạng thái\n";

            foreach (var log in logs)
            {
                csv += $"\"{log.Timestamp:dd/MM/yyyy HH:mm:ss}\",\"{log.Username}\",\"{log.ActionType}\",\"{log.Module}\",\"{log.Description}\",\"{log.IpAddress}\",\"{log.Status}\"\n";
            }

            return csv;
        }
    }
}
