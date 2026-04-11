using HDKTech.Data;
using HDKTech.Models;
using Microsoft.EntityFrameworkCore;

namespace HDKTech.Repositories
{
    public class SystemLogRepository : ISystemLogRepository
    {
        private readonly HDKTechContext _context;

        public SystemLogRepository(HDKTechContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Lấy tất cả logs với phân trang
        /// </summary>
        public async Task<(List<SystemLog> logs, int total)> GetLogsAsync(int pageNumber = 1, int pageSize = 10)
        {
            var query = _context.Set<SystemLog>().AsNoTracking();
            var total = await query.CountAsync();
            
            var logs = await query
                .OrderByDescending(l => l.Timestamp)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (logs, total);
        }

        /// <summary>
        /// Tìm kiếm logs với filter
        /// </summary>
        public async Task<(List<SystemLog> logs, int total)> SearchLogsAsync(
            string searchText = null,
            string actionType = null,
            string module = null,
            string username = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            int pageNumber = 1,
            int pageSize = 10)
        {
            var query = _context.Set<SystemLog>().AsNoTracking();

            // Filter by search text
            if (!string.IsNullOrWhiteSpace(searchText))
            {
                query = query.Where(l => 
                    l.Description.Contains(searchText) || 
                    l.EntityName.Contains(searchText) ||
                    l.Username.Contains(searchText));
            }

            // Filter by action type
            if (!string.IsNullOrWhiteSpace(actionType) && actionType != "All")
            {
                query = query.Where(l => l.ActionType == actionType);
            }

            // Filter by module
            if (!string.IsNullOrWhiteSpace(module) && module != "All")
            {
                query = query.Where(l => l.Module == module);
            }

            // Filter by username
            if (!string.IsNullOrWhiteSpace(username))
            {
                query = query.Where(l => l.Username.Contains(username));
            }

            // Filter by date range
            if (fromDate.HasValue)
            {
                query = query.Where(l => l.Timestamp >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(l => l.Timestamp < toDate.Value.AddDays(1));
            }

            var total = await query.CountAsync();

            var logs = await query
                .OrderByDescending(l => l.Timestamp)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (logs, total);
        }

        /// <summary>
        /// Lấy log theo ID
        /// </summary>
        public async Task<SystemLog> GetLogByIdAsync(int id)
        {
            return await _context.Set<SystemLog>().FindAsync(id);
        }

        /// <summary>
        /// Thêm log mới
        /// </summary>
        public async Task<SystemLog> AddLogAsync(SystemLog log)
        {
            _context.Set<SystemLog>().Add(log);
            await _context.SaveChangesAsync();
            return log;
        }

        /// <summary>
        /// Lấy logs của một người dùng
        /// </summary>
        public async Task<List<SystemLog>> GetUserLogsAsync(string username, int limit = 50)
        {
            return await _context.Set<SystemLog>()
                .Where(l => l.Username == username)
                .OrderByDescending(l => l.Timestamp)
                .Take(limit)
                .ToListAsync();
        }

        /// <summary>
        /// Lấy logs của một entity cụ thể
        /// </summary>
        public async Task<List<SystemLog>> GetEntityLogsAsync(string entityId, string module = null)
        {
            var query = _context.Set<SystemLog>()
                .Where(l => l.EntityId == entityId);

            if (!string.IsNullOrWhiteSpace(module))
            {
                query = query.Where(l => l.Module == module);
            }

            return await query
                .OrderByDescending(l => l.Timestamp)
                .ToListAsync();
        }

        /// <summary>
        /// Lấy stats ngày hôm nay
        /// </summary>
        public async Task<(int totalToday, int loginCount, int createCount, int updateCount, int deleteCount)> GetTodayStatsAsync()
        {
            var today = DateTime.Now.Date;
            var query = _context.Set<SystemLog>()
                .Where(l => l.Timestamp.Date == today);

            var total = await query.CountAsync();
            var loginCount = await query.Where(l => l.ActionType == "Login").CountAsync();
            var createCount = await query.Where(l => l.ActionType == "Create").CountAsync();
            var updateCount = await query.Where(l => l.ActionType == "Update").CountAsync();
            var deleteCount = await query.Where(l => l.ActionType == "Delete").CountAsync();

            return (total, loginCount, createCount, updateCount, deleteCount);
        }

        /// <summary>
        /// Lấy tổng số logs
        /// </summary>
        public async Task<int> GetTotalLogsAsync()
        {
            return await _context.Set<SystemLog>().CountAsync();
        }

        /// <summary>
        /// Xóa logs cũ (hơn X ngày)
        /// </summary>
        public async Task DeleteOldLogsAsync(int daysOld = 90)
        {
            var cutoffDate = DateTime.Now.AddDays(-daysOld);
            var oldLogs = await _context.Set<SystemLog>()
                .Where(l => l.Timestamp < cutoffDate)
                .ToListAsync();

            _context.Set<SystemLog>().RemoveRange(oldLogs);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Lấy danh sách modules có logs
        /// </summary>
        public async Task<List<string>> GetModulesAsync()
        {
            return await _context.Set<SystemLog>()
                .Select(l => l.Module)
                .Distinct()
                .OrderBy(m => m)
                .ToListAsync();
        }

        /// <summary>
        /// Lấy danh sách action types
        /// </summary>
        public async Task<List<string>> GetActionTypesAsync()
        {
            return await _context.Set<SystemLog>()
                .Select(l => l.ActionType)
                .Distinct()
                .OrderBy(a => a)
                .ToListAsync();
        }

        /// <summary>
        /// Lấy danh sách usernames
        /// </summary>
        public async Task<List<string>> GetUsernamesAsync()
        {
            return await _context.Set<SystemLog>()
                .Select(l => l.Username)
                .Distinct()
                .OrderBy(u => u)
                .ToListAsync();
        }
    }

    public interface ISystemLogRepository
    {
        Task<(List<SystemLog> logs, int total)> GetLogsAsync(int pageNumber = 1, int pageSize = 10);
        Task<(List<SystemLog> logs, int total)> SearchLogsAsync(
            string searchText = null,
            string actionType = null,
            string module = null,
            string username = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            int pageNumber = 1,
            int pageSize = 10);
        Task<SystemLog> GetLogByIdAsync(int id);
        Task<SystemLog> AddLogAsync(SystemLog log);
        Task<List<SystemLog>> GetUserLogsAsync(string username, int limit = 50);
        Task<List<SystemLog>> GetEntityLogsAsync(string entityId, string module = null);
        Task<(int totalToday, int loginCount, int createCount, int updateCount, int deleteCount)> GetTodayStatsAsync();
        Task<int> GetTotalLogsAsync();
        Task DeleteOldLogsAsync(int daysOld = 90);
        Task<List<string>> GetModulesAsync();
        Task<List<string>> GetActionTypesAsync();
        Task<List<string>> GetUsernamesAsync();
    }
}
