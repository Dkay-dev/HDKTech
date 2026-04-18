using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using HDKTech.Models;
using HDKTech.Areas.Admin.Repositories;
using HDKTech.Areas.Admin.ViewModels;
using HDKTech.Areas.Admin.Repositories;

namespace HDKTech.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Policy = "RequireAdmin")]
    [Route("admin/[controller]")]
    public class SystemLogController : Controller
    {
        private readonly ISystemLogRepository _logRepository;

        public SystemLogController(ISystemLogRepository logRepository)
        {
            _logRepository = logRepository;
        }

        /// <summary>
        /// Danh sách Audit Log - bảng Thời gian | Người dùng | Hành động | Chi tiết
        /// GET: /admin/systemlog
        /// </summary>
        [HttpGet("")]
        [HttpGet("Index")]
        public async Task<IActionResult> Index(
            int page = 1,
            string? search = null,
            string? actionType = null,
            string? module = null,
            string? username = null)
        {
            const int pageSize = 20;

            var (logs, total) = await _logRepository.SearchLogsAsync(
                searchText: search,
                actionType: actionType,
                module:     module,
                username:   username,
                pageNumber: page,
                pageSize:   pageSize
            );

            var stats     = await _logRepository.GetTodayStatsAsync();
            var totalLogs = await _logRepository.GetTotalLogsAsync();

            var vm = new SystemLogViewModel
            {
                Logs               = logs,
                TotalLogs          = totalLogs,
                TodayActions       = stats.totalToday,
                LoginCount         = stats.loginCount,
                CreateCount        = stats.createCount,
                UpdateCount        = stats.updateCount,
                DeleteCount        = stats.deleteCount,
                CurrentPage        = page,
                TotalPages         = (int)Math.Ceiling((double)total / pageSize),
                PageSize           = pageSize,
                TotalCount         = total,
                SearchText         = search,
                SelectedActionType = actionType,
                SelectedModule     = module,
                SelectedUsername   = username,
                ActionTypes        = (await _logRepository.GetModulesAsync())
                                        .Where(x => !string.IsNullOrEmpty(x)).ToList(),
                Modules            = (await _logRepository.GetActionTypesAsync())
                                        .Where(x => !string.IsNullOrEmpty(x)).ToList(),
                Usernames          = (await _logRepository.GetUsernamesAsync())
                                        .Where(x => !string.IsNullOrEmpty(x)).ToList()
            };

            return View(vm);
        }

        /// <summary>
        /// Chi tiết log (JSON response)
        /// </summary>
        [HttpGet("Details/{id}")]
        public async Task<IActionResult> Details(int id)
        {
            var log = await _logRepository.GetLogByIdAsync(id);
            if (log == null) return NotFound();
            return Ok(log);
        }

        /// <summary>
        /// API: Lấy logs phân trang (AJAX)
        /// </summary>
        [HttpGet("api/logs")]
        public async Task<IActionResult> GetLogs(int page = 1, int pageSize = 20,
            string? search = null, string? actionType = null, string? module = null)
        {
            var (logs, total) = await _logRepository.SearchLogsAsync(
                searchText: search,
                actionType: actionType,
                module:     module,
                pageNumber: page,
                pageSize:   pageSize
            );

            return Ok(new
            {
                success    = true,
                data       = logs,
                pagination = new
                {
                    currentPage = page,
                    pageSize,
                    totalCount  = total,
                    totalPages  = (int)Math.Ceiling((double)total / pageSize)
                }
            });
        }

        /// <summary>
        /// Export logs to CSV
        /// </summary>
        [HttpGet("export")]
        public async Task<IActionResult> Export(string? search = null,
            string? actionType = null, string? module = null, string? username = null)
        {
            var (logs, _) = await _logRepository.SearchLogsAsync(
                searchText: search,
                actionType: actionType,
                module:     module,
                username:   username,
                pageNumber: 1,
                pageSize:   10000
            );

            var csv = GenerateCsv(logs);
            var fileName = $"AuditLog_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", fileName);
        }

        private string GenerateCsv(List<SystemLog> logs)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Thời gian,Người dùng,Hành động,Module,Chi tiết,IP,Trạng thái");
            foreach (var log in logs)
            {
                sb.AppendLine(
                    $"\"{log.CreatedAt:dd/MM/yyyy HH:mm:ss}\"," +
                    $"\"{log.Username ?? log.UserId}\"," +
                    $"\"{log.Action}\"," +
                    $"\"{log.LogLevel}\"," +
                    $"\"{log.Description?.Replace("\"", "'")}\"," +
                    $"\"{log.IpAddress}\"," +
                    $"\"{log.Status}\""
                );
            }
            return sb.ToString();
        }
    }
}

