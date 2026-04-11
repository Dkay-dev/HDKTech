using HDKTech.Models;
using HDKTech.Repositories;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace HDKTech.Services
{
    public interface ISystemLogService
    {
        Task LogActionAsync(string username, string actionType, string module, string description,
            string entityId = null, string entityName = null, string oldValue = null, string newValue = null,
            string userRole = null, string userId = null);

        Task LogLoginAsync(string username, string ipAddress, string userAgent, string userRole = null);
        Task LogLogoutAsync(string username);
        Task LogErrorAsync(string username, string module, string description, string errorMessage);
    }

    public class SystemLogService : ISystemLogService
    {
        private readonly ISystemLogRepository _logRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public SystemLogService(ISystemLogRepository logRepository, IHttpContextAccessor httpContextAccessor)
        {
            _logRepository = logRepository;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Ghi log hành động chung
        /// </summary>
        public async Task LogActionAsync(string username, string actionType, string module, string description,
            string entityId = null, string entityName = null, string oldValue = null, string newValue = null,
            string userRole = null, string userId = null)
        {
            try
            {
                var ipAddress = GetClientIpAddress();
                var userAgent = _httpContextAccessor?.HttpContext?.Request.Headers["User-Agent"].ToString();

                var log = new SystemLog
                {
                    Timestamp = DateTime.Now,
                    Username = username ?? "System",
                    ActionType = actionType,
                    Module = module,
                    Description = description,
                    EntityId = entityId,
                    EntityName = entityName,
                    OldValue = oldValue,
                    NewValue = newValue,
                    IpAddress = ipAddress,
                    UserAgent = userAgent,
                    Status = "Success",
                    UserRole = userRole,
                    UserId = userId
                };

                await _logRepository.AddLogAsync(log);
            }
            catch (Exception ex)
            {
                // Log error to console/debug - không throw exception
                System.Diagnostics.Debug.WriteLine($"Failed to log action: {ex.Message}");
            }
        }

        /// <summary>
        /// Ghi log đăng nhập
        /// </summary>
        public async Task LogLoginAsync(string username, string ipAddress, string userAgent, string userRole = null)
        {
            await LogActionAsync(
                username: username,
                actionType: "Login",
                module: "System",
                description: $"Đăng nhập thành công từ IP {ipAddress}",
                userRole: userRole
            );
        }

        /// <summary>
        /// Ghi log đăng xuất
        /// </summary>
        public async Task LogLogoutAsync(string username)
        {
            await LogActionAsync(
                username: username,
                actionType: "Logout",
                module: "System",
                description: "Đăng xuất khỏi hệ thống"
            );
        }

        /// <summary>
        /// Ghi log lỗi
        /// </summary>
        public async Task LogErrorAsync(string username, string module, string description, string errorMessage)
        {
            try
            {
                var ipAddress = GetClientIpAddress();
                var userAgent = _httpContextAccessor?.HttpContext?.Request.Headers["User-Agent"].ToString();

                var log = new SystemLog
                {
                    Timestamp = DateTime.Now,
                    Username = username ?? "System",
                    ActionType = "Error",
                    Module = module,
                    Description = description,
                    IpAddress = ipAddress,
                    UserAgent = userAgent,
                    Status = "Failed",
                    ErrorMessage = errorMessage
                };

                await _logRepository.AddLogAsync(log);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to log error: {ex.Message}");
            }
        }

        /// <summary>
        /// Lấy địa chỉ IP của client
        /// </summary>
        private string GetClientIpAddress()
        {
            var httpContext = _httpContextAccessor?.HttpContext;
            if (httpContext == null) return "Unknown";

            var xff = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(xff))
            {
                var ip = xff.Split(',').FirstOrDefault()?.Trim();
                if (!string.IsNullOrEmpty(ip))
                    return ip;
            }

            return httpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        }
    }

    /// <summary>
    /// Extension method để log objects thành JSON
    /// </summary>
    public static class SystemLogExtensions
    {
        public static string ToJson<T>(this T obj) where T : class
        {
            try
            {
                return JsonSerializer.Serialize(obj, new JsonSerializerOptions { WriteIndented = false });
            }
            catch
            {
                return obj?.ToString() ?? "";
            }
        }
    }
}
