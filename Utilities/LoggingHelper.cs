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

        // ════════════════════════════════════════════════════════════════════
        // Giai đoạn 1 — Audit Log Helpers (Auto-hóa ghi nhật ký)
        // Mọi thao tác thay đổi kho / trạng thái đơn hàng đều dùng các hàm dưới đây
        // ════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Auto-log khi trừ kho (ReserveStock tại thời điểm đặt hàng).
        /// </summary>
        public static async Task LogInventoryReserveAsync(
            string username,
            int    productId,
            string productName,
            int    reservedQty,
            int    remainingQty,
            string orderCode,
            string userId = null)
        {
            await LogAsync(
                username   : username,
                actionType : "InventoryReserve",
                module     : "Inventory",
                description: $"[ĐẶT HÀNG #{orderCode}] Trừ kho SP '{productName}' (ID:{productId}): " +
                             $"-{reservedQty} → còn {remainingQty}",
                entityId   : productId.ToString(),
                entityName : productName,
                oldValue   : new { Quantity = remainingQty + reservedQty },
                newValue   : new { Quantity = remainingQty },
                userId     : userId);
        }

        /// <summary>
        /// Auto-log khi hoàn kho (ReleaseStock khi hủy đơn hàng).
        /// </summary>
        public static async Task LogInventoryReleaseAsync(
            string username,
            int    productId,
            string productName,
            int    releasedQty,
            int    newQty,
            string orderCode,
            string userId = null)
        {
            await LogAsync(
                username   : username,
                actionType : "InventoryRelease",
                module     : "Inventory",
                description: $"[HỦY ĐƠN #{orderCode}] Hoàn kho SP '{productName}' (ID:{productId}): " +
                             $"+{releasedQty} → còn {newQty}",
                entityId   : productId.ToString(),
                entityName : productName,
                oldValue   : new { Quantity = newQty - releasedQty },
                newValue   : new { Quantity = newQty },
                userId     : userId);
        }

        /// <summary>
        /// Auto-log khi cập nhật trạng thái đơn hàng.
        /// Gọi sau mỗi thay đổi Status trong OrderController.
        /// </summary>
        public static async Task LogOrderStatusChangeAsync(
            string username,
            int    orderId,
            string orderCode,
            string oldStatus,
            string newStatus,
            string userId   = null,
            string userRole = null)
        {
            await LogAsync(
                username   : username,
                actionType : "OrderStatusChange",
                module     : "Order",
                description: $"Đơn #{orderCode} (ID:{orderId}): {oldStatus} → {newStatus}",
                entityId   : orderId.ToString(),
                entityName : orderCode,
                oldValue   : new { Status = oldStatus },
                newValue   : new { Status = newStatus },
                userRole   : userRole,
                userId     : userId);
        }
    }
}
