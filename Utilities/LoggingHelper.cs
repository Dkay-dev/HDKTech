using HDKTech.Services;
using HDKTech.Repositories;
using System.Text.Json;

namespace HDKTech.Utilities
{
    /// <summary>
    /// Helper class để logging actions trong các controllers
    /// Sử dụng: await LoggingHelper.LogAsync(User.Identity.Name, "Create", "Banner", "Thêm banner mới", bannerId.ToString())
    /// </summary>
    public static class LoggingHelper
    {
        private static ISystemLogService _logService;

        public static void Initialize(ISystemLogService logService)
        {
            _logService = logService;
        }

        /// <summary>
        /// Log hành động chung
        /// </summary>
        public static async Task LogAsync(
            string username,
            string actionType,
            string module,
            string description,
            string entityId = null,
            string entityName = null,
            object oldValue = null,
            object newValue = null,
            string userRole = null,
            string userId = null)
        {
            if (_logService == null) return;

            try
            {
                var oldValueJson = oldValue != null ? JsonSerializer.Serialize(oldValue) : null;
                var newValueJson = newValue != null ? JsonSerializer.Serialize(newValue) : null;

                await _logService.LogActionAsync(
                    username: username,
                    actionType: actionType,
                    module: module,
                    description: description,
                    entityId: entityId,
                    entityName: entityName,
                    oldValue: oldValueJson,
                    newValue: newValueJson,
                    userRole: userRole,
                    userId: userId
                );
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Logging error: {ex.Message}");
            }
        }

        /// <summary>
        /// Log thao tác Create
        /// </summary>
        public static async Task LogCreateAsync(
            string username,
            string module,
            string entityName,
            string entityId,
            object newValue = null,
            string description = null)
        {
            description ??= $"Thêm {module.ToLower()} mới '{entityName}'";
            await LogAsync(username, "Create", module, description, entityId, entityName, null, newValue);
        }

        /// <summary>
        /// Log thao tác Update
        /// </summary>
        public static async Task LogUpdateAsync(
            string username,
            string module,
            string entityName,
            string entityId,
            object oldValue,
            object newValue,
            string description = null)
        {
            description ??= $"Cập nhật {module.ToLower()} '{entityName}'";
            await LogAsync(username, "Update", module, description, entityId, entityName, oldValue, newValue);
        }

        /// <summary>
        /// Log thao tác Delete
        /// </summary>
        public static async Task LogDeleteAsync(
            string username,
            string module,
            string entityName,
            string entityId,
            object oldValue = null,
            string description = null)
        {
            description ??= $"Xóa {module.ToLower()} '{entityName}'";
            await LogAsync(username, "Delete", module, description, entityId, entityName, oldValue, null);
        }

        /// <summary>
        /// Log thao tác Toggle/Enable/Disable
        /// </summary>
        public static async Task LogToggleAsync(
            string username,
            string module,
            string entityName,
            string entityId,
            string action,
            string description = null)
        {
            description ??= $"{action} {module.ToLower()} '{entityName}'";
            await LogAsync(username, "Update", module, description, entityId, entityName);
        }

        /// <summary>
        /// Log lỗi
        /// </summary>
        public static async Task LogErrorAsync(
            string username,
            string module,
            string description,
            string errorMessage = null)
        {
            if (_logService == null) return;
            await _logService.LogErrorAsync(username, module, description, errorMessage);
        }
    }
}
