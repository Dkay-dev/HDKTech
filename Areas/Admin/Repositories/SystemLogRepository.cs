using HDKTech.Data;
using HDKTech.Models;
using Microsoft.EntityFrameworkCore;

namespace HDKTech.Areas.Admin.Repositories
{
    public class SystemLogRepository : ISystemLogRepository
    {
        private readonly HDKTechContext _context;

        public SystemLogRepository(HDKTechContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Get all logs with pagination
        /// </summary>
        public async Task<(List<SystemLog> logs, int total)> GetLogsAsync(int pageNumber = 1, int pageSize = 10)
        {
            var query = _context.Set<SystemLog>().AsNoTracking();
            var total = await query.CountAsync();

            var logs = await query
                .OrderByDescending(l => l.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (logs, total);
        }

        /// <summary>
        /// Search logs with filters
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
                    l.UserId.Contains(searchText));
            }

            // Filter by action type
            if (!string.IsNullOrWhiteSpace(actionType) && actionType != "All")
            {
                query = query.Where(l => l.Action == actionType);
            }

            // Filter by module
            if (!string.IsNullOrWhiteSpace(module) && module != "All")
            {
                query = query.Where(l => l.LogLevel == module);
            }

            // Filter by username
            if (!string.IsNullOrWhiteSpace(username))
            {
                query = query.Where(l => l.UserId.Contains(username));
            }

            // Filter by date range
            if (fromDate.HasValue)
            {
                query = query.Where(l => l.CreatedAt >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(l => l.CreatedAt < toDate.Value.AddDays(1));
            }

            var total = await query.CountAsync();

            var logs = await query
                .OrderByDescending(l => l.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (logs, total);
        }

        /// <summary>
        /// Get log by ID
        /// </summary>
        public async Task<SystemLog> GetLogByIdAsync(int id)
        {
            return await _context.Set<SystemLog>().FindAsync(id);
        }

        /// <summary>
        /// Add new log
        /// </summary>
        public async Task<SystemLog> AddLogAsync(SystemLog log)
        {
            _context.Set<SystemLog>().Add(log);
            await _context.SaveChangesAsync();
            return log;
        }

        /// <summary>
        /// Get logs for a specific user
        /// </summary>
        public async Task<List<SystemLog>> GetUserLogsAsync(string username, int limit = 50)
        {
            return await _context.Set<SystemLog>()
                .Where(l => l.UserId == username)
                .OrderByDescending(l => l.CreatedAt)
                .Take(limit)
                .ToListAsync();
        }

        /// <summary>
        /// Get logs for a specific entity
        /// </summary>
        public async Task<List<SystemLog>> GetEntityLogsAsync(string entityId, string module = null)
        {
            var query = _context.Set<SystemLog>()
                .Where(l => l.Description.Contains(entityId));

            if (!string.IsNullOrWhiteSpace(module))
            {
                query = query.Where(l => l.Action == module);
            }

            return await query
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();
        }

        /// <summary>
        /// Get today's stats
        /// </summary>
        public async Task<(int totalToday, int loginCount, int createCount, int updateCount, int deleteCount)> GetTodayStatsAsync()
        {
            var today = DateTime.Now.Date;
            var query = _context.Set<SystemLog>()
                .Where(l => l.CreatedAt.Date == today);

            var total = await query.CountAsync();
            var loginCount = await query.Where(l => l.LogLevel == "Info").CountAsync();
            var createCount = await query.Where(l => l.Action == "Create").CountAsync();
            var updateCount = await query.Where(l => l.Action == "Update").CountAsync();
            var deleteCount = await query.Where(l => l.Action == "Delete").CountAsync();

            return (total, loginCount, createCount, updateCount, deleteCount);
        }

        /// <summary>
        /// Get total logs count
        /// </summary>
        public async Task<int> GetTotalLogsAsync()
        {
            return await _context.Set<SystemLog>().CountAsync();
        }

        /// <summary>
        /// Delete old logs (older than X days)
        /// </summary>
        public async Task DeleteOldLogsAsync(int daysOld = 90)
        {
            var cutoffDate = DateTime.Now.AddDays(-daysOld);
            var oldLogs = await _context.Set<SystemLog>()
                .Where(l => l.CreatedAt < cutoffDate)
                .ToListAsync();

            _context.Set<SystemLog>().RemoveRange(oldLogs);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Get list of modules with logs
        /// LogLevel = cột Module trong DB theo alias của SystemLog entity
        /// </summary>
        public async Task<List<string>> GetModulesAsync()
        {
            return await _context.Set<SystemLog>()
                .Select(l => l.LogLevel)   // LogLevel alias → Module
                .Distinct()
                .OrderBy(m => m)
                .ToListAsync();
        }

        /// <summary>
        /// Get list of action types
        /// Action = cột ActionType trong DB theo alias của SystemLog entity
        /// </summary>
        public async Task<List<string>> GetActionTypesAsync()
        {
            return await _context.Set<SystemLog>()
                .Select(l => l.Action)     // Action alias → ActionType
                .Distinct()
                .OrderBy(a => a)
                .ToListAsync();
        }

        /// <summary>
        /// Get list of usernames
        /// </summary>
        public async Task<List<string>> GetUsernamesAsync()
        {
            return await _context.Set<SystemLog>()
                .Select(l => l.UserId)
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
