using HDKTech.Services;
using System.Text.Json;

namespace HDKTech.Utilities
{
    /// <summary>
    /// Static convenience wrapper cho ISystemLogService.
    /// Giữ API tĩnh để không phá code cũ; delegate mọi call vào ISystemLogService
    /// thông qua IServiceScopeFactory (an toàn thread, không giữ scoped instance).
    ///
    /// Initialization (Program.cs, sau Build()):
    ///   LoggingHelper.Initialize(app.Services.GetRequiredService&lt;IServiceScopeFactory&gt;());
    ///
    /// Lưu ý: _scopeFactory được ghi một lần duy nhất trong startup (single-writer),
    /// volatile đảm bảo visibility cho mọi thread đọc sau đó — không race condition.
    /// </summary>
    public static class LoggingHelper
    {
        // volatile: đảm bảo mọi thread thấy giá trị mới nhất sau khi Initialize ghi.
        // Không cần lock vì Initialize() chỉ được gọi 1 lần trong startup (single-writer).
        private static volatile IServiceScopeFactory? _scopeFactory;

        /// <summary>
        /// Gọi 1 lần duy nhất trong Program.cs, sau app.Build():
        ///   LoggingHelper.Initialize(app.Services.GetRequiredService&lt;IServiceScopeFactory&gt;());
        /// </summary>
        public static void Initialize(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        }

        // ─── Backward-compat overload ──────────────────────────────────────────
        /// <summary>
        /// Overload legacy: Initialize(ISystemLogService) — giữ để không phá
        /// code cũ nếu còn chỗ nào gọi kiểu này. Bỏ qua tham số vì ta không
        /// store scoped service nữa; _scopeFactory phải được set qua overload chính.
        /// </summary>
        [Obsolete("Use Initialize(IServiceScopeFactory) instead to avoid disposed-scope bug.")]
        public static void Initialize(ISystemLogService _)
        {
            // No-op: legacy callers compile thành công nhưng không làm gì.
            // _scopeFactory phải được set qua Initialize(IServiceScopeFactory).
        }

        // ─── Core log method ───────────────────────────────────────────────────

        /// <summary>Log hành động chung.</summary>
        public static async Task LogAsync(
            string  username,
            string  actionType,
            string  module,
            string  description,
            string? entityId   = null,
            string? entityName = null,
            object? oldValue   = null,
            object? newValue   = null,
            string? userRole   = null,
            string? userId     = null)
        {
            if (_scopeFactory is null) return;

            try
            {
                var oldValueJson = oldValue is not null ? JsonSerializer.Serialize(oldValue) : null;
                var newValueJson = newValue is not null ? JsonSerializer.Serialize(newValue) : null;

                // Tạo scope mới mỗi lần log → ISystemLogService không bao giờ bị disposed
                await using var scope = _scopeFactory.CreateAsyncScope();
                var logService = scope.ServiceProvider.GetRequiredService<ISystemLogService>();

                await logService.LogActionAsync(
                    username:    username,
                    actionType:  actionType,
                    module:      module,
                    description: description,
                    entityId:    entityId,
                    entityName:  entityName,
                    oldValue:    oldValueJson,
                    newValue:    newValueJson,
                    userRole:    userRole,
                    userId:      userId
                );
            }
            catch (Exception ex)
            {
                // Logging không được làm crash app
                System.Diagnostics.Debug.WriteLine($"[LoggingHelper] error: {ex.Message}");
            }
        }

        // ─── Convenience helpers ───────────────────────────────────────────────

        /// <summary>Log thao tác Create.</summary>
        public static async Task LogCreateAsync(
            string  username,
            string  module,
            string  entityName,
            string  entityId,
            object? newValue    = null,
            string? description = null)
        {
            description ??= $"Thêm {module.ToLower()} mới '{entityName}'";
            await LogAsync(username, "Create", module, description, entityId, entityName, null, newValue);
        }

        /// <summary>Log thao tác Update.</summary>
        public static async Task LogUpdateAsync(
            string  username,
            string  module,
            string  entityName,
            string  entityId,
            object? oldValue,
            object? newValue,
            string? description = null)
        {
            description ??= $"Cập nhật {module.ToLower()} '{entityName}'";
            await LogAsync(username, "Update", module, description, entityId, entityName, oldValue, newValue);
        }

        /// <summary>Log thao tác Delete.</summary>
        public static async Task LogDeleteAsync(
            string  username,
            string  module,
            string  entityName,
            string  entityId,
            object? oldValue    = null,
            string? description = null)
        {
            description ??= $"Xóa {module.ToLower()} '{entityName}'";
            await LogAsync(username, "Delete", module, description, entityId, entityName, oldValue, null);
        }

        /// <summary>Log thao tác Toggle/Enable/Disable.</summary>
        public static async Task LogToggleAsync(
            string  username,
            string  module,
            string  entityName,
            string  entityId,
            string  action,
            string? description = null)
        {
            description ??= $"{action} {module.ToLower()} '{entityName}'";
            await LogAsync(username, "Update", module, description, entityId, entityName);
        }

        /// <summary>Log lỗi.</summary>
        public static async Task LogErrorAsync(
            string  username,
            string  module,
            string  description,
            string? errorMessage = null)
        {
            if (_scopeFactory is null) return;

            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var logService = scope.ServiceProvider.GetRequiredService<ISystemLogService>();
                await logService.LogErrorAsync(username, module, description, errorMessage ?? string.Empty);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LoggingHelper.LogErrorAsync] error: {ex.Message}");
            }
        }

        // ════════════════════════════════════════════════════════════════════════
        // Audit Log Helpers — Inventory & Order
        // ════════════════════════════════════════════════════════════════════════

        /// <summary>Auto-log khi trừ kho (ReserveStock tại thời điểm đặt hàng).</summary>
        public static async Task LogInventoryReserveAsync(
            string  username,
            int     productId,
            string  productName,
            int     reservedQty,
            int     remainingQty,
            string  orderCode,
            string? userId = null)
        {
            await LogAsync(
                username:    username,
                actionType:  "InventoryReserve",
                module:      "Inventory",
                description: $"[ĐẶT HÀNG #{orderCode}] Trừ kho SP '{productName}' (ID:{productId}): " +
                             $"-{reservedQty} → còn {remainingQty}",
                entityId:    productId.ToString(),
                entityName:  productName,
                oldValue:    new { Quantity = remainingQty + reservedQty },
                newValue:    new { Quantity = remainingQty },
                userId:      userId);
        }

        /// <summary>Auto-log khi hoàn kho (ReleaseStock khi hủy đơn hàng).</summary>
        public static async Task LogInventoryReleaseAsync(
            string  username,
            int     productId,
            string  productName,
            int     releasedQty,
            int     newQty,
            string  orderCode,
            string? userId = null)
        {
            await LogAsync(
                username:    username,
                actionType:  "InventoryRelease",
                module:      "Inventory",
                description: $"[HỦY ĐƠN #{orderCode}] Hoàn kho SP '{productName}' (ID:{productId}): " +
                             $"+{releasedQty} → còn {newQty}",
                entityId:    productId.ToString(),
                entityName:  productName,
                oldValue:    new { Quantity = newQty - releasedQty },
                newValue:    new { Quantity = newQty },
                userId:      userId);
        }

        /// <summary>Auto-log khi cập nhật trạng thái đơn hàng.</summary>
        public static async Task LogOrderStatusChangeAsync(
            string  username,
            int     orderId,
            string  orderCode,
            string  oldStatus,
            string  newStatus,
            string? userId   = null,
            string? userRole = null)
        {
            await LogAsync(
                username:    username,
                actionType:  "OrderStatusChange",
                module:      "Order",
                description: $"Đơn #{orderCode} (ID:{orderId}): {oldStatus} → {newStatus}",
                entityId:    orderId.ToString(),
                entityName:  orderCode,
                oldValue:    new { Status = oldStatus },
                newValue:    new { Status = newStatus },
                userRole:    userRole,
                userId:      userId);
        }
    }
}
