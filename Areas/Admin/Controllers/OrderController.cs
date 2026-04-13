using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using HDKTech.Data;
using HDKTech.Models;
using HDKTech.Services;
using HDKTech.Utilities;
using Microsoft.EntityFrameworkCore;

using HDKTech.Areas.Admin.Repositories;

namespace HDKTech.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Manager")]   // Fallback: chỉ Admin/Manager vào được
    [Route("admin/[controller]")]
    public class OrderController : Controller
    {
        private readonly HDKTechContext          _context;
        private readonly ILogger<OrderController> _logger;
        private readonly ISystemLogService        _logService;
        // ── Giai đoạn 1: Inventory Sync ──────────────────────────────────────
        private readonly IInventoryService        _inventoryService;

        private static readonly string[] StatusNames =
            { "Chờ xác nhận", "Đang xử lý", "Đang giao", "Đã giao", "Đã hủy" };

        public OrderController(
            HDKTechContext context,
            ILogger<OrderController> logger,
            ISystemLogService logService,
            IInventoryService inventoryService)
        {
            _context          = context;
            _logger           = logger;
            _logService       = logService;
            _inventoryService = inventoryService;
        }

        // ──────────────────────────────────────────────────────────────
        // INDEX — danh sách đơn hàng với filter + phân trang
        // GET: /admin/order
        // ──────────────────────────────────────────────────────────────
        [HttpGet("")]
        [HttpGet("index")]
        [Authorize(Policy = "Order.Read")]   // ← Granular Security
        public async Task<IActionResult> Index(
            int page = 1,
            int pageSize = 20,
            string searchTerm = "",
            int statusFilter = -1,
            string sortBy = "date")
        {
            try
            {
                IQueryable<Order> query = _context.Orders
                    .AsNoTracking()
                    .Include(o => o.User)
                    .Include(o => o.Items);

                if (!string.IsNullOrWhiteSpace(searchTerm))
                    query = query.Where(o =>
                        o.OrderCode.Contains(searchTerm) ||
                        o.RecipientName.Contains(searchTerm) ||
                        o.RecipientPhone.Contains(searchTerm));

                if (statusFilter >= 0)
                    query = query.Where(o => o.Status == statusFilter);

                query = sortBy switch
                {
                    "amount_high" => query.OrderByDescending(o => o.TotalAmount),
                    "amount_low"  => query.OrderBy(o => o.TotalAmount),
                    "customer"    => query.OrderBy(o => o.RecipientName),
                    _             => query.OrderByDescending(o => o.OrderDate)
                };

                var totalCount = await query.CountAsync();
                var orders = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                ViewBag.Orders       = orders;
                ViewBag.TotalCount   = totalCount;
                ViewBag.Page         = page;
                ViewBag.PageSize     = pageSize;
                ViewBag.TotalPages   = (int)Math.Ceiling((double)totalCount / pageSize);
                ViewBag.SearchTerm   = searchTerm;
                ViewBag.StatusFilter = statusFilter;
                ViewBag.SortBy       = sortBy;

                // Stats per status (single query)
                var stats = await _context.Orders.AsNoTracking()
                    .GroupBy(o => o.Status)
                    .Select(g => new { Status = g.Key, Count = g.Count() })
                    .ToListAsync();

                ViewBag.PendingCount    = stats.FirstOrDefault(s => s.Status == 0)?.Count ?? 0;
                ViewBag.ProcessingCount = stats.FirstOrDefault(s => s.Status == 1)?.Count ?? 0;
                ViewBag.ShippingCount   = stats.FirstOrDefault(s => s.Status == 2)?.Count ?? 0;
                ViewBag.DeliveredCount  = stats.FirstOrDefault(s => s.Status == 3)?.Count ?? 0;
                ViewBag.CancelledCount  = stats.FirstOrDefault(s => s.Status == 4)?.Count ?? 0;

                // Today
                var today = DateTime.Now.Date;
                var todayRevenue = await _context.Orders.AsNoTracking()
                    .Where(o => o.OrderDate.Date == today && o.Status == 3)
                    .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;
                var todayCount = await _context.Orders.AsNoTracking()
                    .Where(o => o.OrderDate.Date == today)
                    .CountAsync();

                ViewBag.TodayRevenue    = todayRevenue;
                ViewBag.TodayOrderCount = todayCount;

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi tải danh sách đơn hàng");
                TempData["Error"] = "Lỗi khi tải danh sách đơn hàng.";
                return View();
            }
        }

        // ──────────────────────────────────────────────────────────────
        // DETAILS
        // GET: /admin/order/details/{id}
        // ──────────────────────────────────────────────────────────────
        [HttpGet("details/{id:int}")]
        [Authorize(Policy = "Order.Read")]   // ← Granular Security
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var order = await _context.Orders
                    .AsNoTracking()
                    .Include(o => o.User)
                    .Include(o => o.Items)
                        .ThenInclude(i => i.Product)
                            .ThenInclude(p => p!.Images)
                    .FirstOrDefaultAsync(o => o.Id == id);

                if (order == null)
                {
                    TempData["Error"] = "Không tìm thấy đơn hàng.";
                    return RedirectToAction(nameof(Index));
                }

                return View(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi tải chi tiết đơn hàng Id: {Id}", id);
                TempData["Error"] = "Lỗi khi tải chi tiết đơn hàng.";
                return RedirectToAction(nameof(Index));
            }
        }

        // ──────────────────────────────────────────────────────────────
        // UPDATE STATUS (AJAX)
        // POST: /admin/order/update-status
        // ──────────────────────────────────────────────────────────────
        [HttpPost("update-status")]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "Order.Update")]   // ← Granular Security
        public async Task<IActionResult> UpdateStatus(int orderId, int newStatus)
        {
            if (newStatus < 0 || newStatus > 4)
                return Json(new { success = false, message = "Trạng thái không hợp lệ." });

            try
            {
                var order = await _context.Orders
                    .Include(o => o.Items)
                    .FirstOrDefaultAsync(o => o.Id == orderId);

                if (order == null)
                    return Json(new { success = false, message = "Không tìm thấy đơn hàng." });

                // Guard: đơn đã giao chỉ có thể chuyển sang Đã hủy
                if (order.Status == 3 && newStatus != 4)
                    return Json(new { success = false, message = "Đơn hàng đã giao không thể thay đổi trạng thái." });

                var oldStatusName = GetStatusName(order.Status);
                var username      = User.Identity?.Name ?? "System";
                var userId        = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                var userRole      = User.IsInRole("Admin") ? "Admin" : "Manager";

                order.Status = newStatus;
                await _context.SaveChangesAsync();

                // ── NOTE: Kho đã được trừ tại CreateOrderAsync (ReserveStock).
                //    Không cần trừ lại ở đây. Chỉ ghi Audit Log trạng thái đơn hàng.
                await LoggingHelper.LogOrderStatusChangeAsync(
                    username   : username,
                    orderId    : order.Id,
                    orderCode  : order.OrderCode,
                    oldStatus  : oldStatusName,
                    newStatus  : GetStatusName(newStatus),
                    userId     : userId,
                    userRole   : userRole);

                return Json(new { success = true, message = $"Cập nhật trạng thái → \"{GetStatusName(newStatus)}\" thành công." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi cập nhật trạng thái đơn hàng Id: {Id}", orderId);
                return Json(new { success = false, message = "Lỗi khi cập nhật trạng thái." });
            }
        }

        // ──────────────────────────────────────────────────────────────
        // CANCEL ORDER (AJAX)
        // POST: /admin/order/cancel
        // ──────────────────────────────────────────────────────────────
        [HttpPost("cancel")]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "Order.Delete")]   // ← Granular Security
        public async Task<IActionResult> CancelOrder(int orderId)
        {
            try
            {
                // Cần Include Items để hoàn kho
                var order = await _context.Orders
                    .Include(o => o.Items)
                    .FirstOrDefaultAsync(o => o.Id == orderId);

                if (order == null)
                    return Json(new { success = false, message = "Không tìm thấy đơn hàng." });

                if (order.Status == 3)
                    return Json(new { success = false, message = "Không thể hủy đơn hàng đã giao thành công." });

                if (order.Status == 4)
                    return Json(new { success = false, message = "Đơn hàng đã bị hủy trước đó." });

                var oldStatusName = GetStatusName(order.Status);
                var username      = User.Identity?.Name ?? "System";
                var userId        = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

                // ── [1] Cập nhật trạng thái đơn hàng ─────────────────────────
                order.Status = 4;
                await _context.SaveChangesAsync();

                // ── [2] Hoàn kho: ReleaseStock + auto Audit Log ───────────────
                if (order.Items != null && order.Items.Any())
                {
                    await _inventoryService.ReleaseStockAsync(
                        items    : order.Items.ToList(),
                        username : username,
                        userId   : userId);
                }

                // ── [3] Audit Log trạng thái đơn hàng ────────────────────────
                await LoggingHelper.LogOrderStatusChangeAsync(
                    username  : username,
                    orderId   : order.Id,
                    orderCode : order.OrderCode,
                    oldStatus : oldStatusName,
                    newStatus : "Đã hủy",
                    userId    : userId,
                    userRole  : User.IsInRole("Admin") ? "Admin" : "Manager");

                return Json(new { success = true, message = "Đã hủy đơn hàng và hoàn kho thành công." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi hủy đơn hàng Id: {Id}", orderId);
                return Json(new { success = false, message = "Lỗi khi hủy đơn hàng." });
            }
        }

        // ──────────────────────────────────────────────────────────────
        // EXPORT CSV
        // GET: /admin/order/export
        // ──────────────────────────────────────────────────────────────
        [HttpGet("export")]
        public async Task<IActionResult> Export(string searchTerm = "", int statusFilter = -1)
        {
            var query = _context.Orders.AsNoTracking().Include(o => o.User).AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
                query = query.Where(o => o.OrderCode.Contains(searchTerm) || o.RecipientName.Contains(searchTerm));

            if (statusFilter >= 0)
                query = query.Where(o => o.Status == statusFilter);

            var orders = await query.OrderByDescending(o => o.OrderDate).ToListAsync();
            var csv = "Mã Đơn Hàng,Người Nhận,Số ĐT,Địa chỉ,Tổng Tiền,Phí Ship,Trạng Thái,Ngày Đặt\n";

            foreach (var o in orders)
                csv += $"\"{o.OrderCode}\",\"{o.RecipientName}\",\"{o.RecipientPhone}\",\"{o.ShippingAddress}\"," +
                       $"{o.TotalAmount},{o.ShippingFee},\"{GetStatusName(o.Status)}\",{o.OrderDate:yyyy-MM-dd}\n";

            return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv",
                        $"orders_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
        }

        // ── Helper ────────────────────────────────────────────────────
        private static string GetStatusName(int status) =>
            status >= 0 && status < StatusNames.Length ? StatusNames[status] : "Không xác định";
    }
}

