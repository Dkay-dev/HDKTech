using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using HDKTech.Data;
using HDKTech.Models;
using HDKTech.Services;
using Microsoft.EntityFrameworkCore;

namespace HDKTech.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Manager")]
    [Route("admin/[controller]")]
    public class OrderController : Controller
    {
        private readonly HDKTechContext _context;
        private readonly ILogger<OrderController> _logger;
        private readonly ISystemLogService _logService;

        private static readonly string[] StatusNames =
            { "Chờ xác nhận", "Đang xử lý", "Đang giao", "Đã giao", "Đã hủy" };

        public OrderController(
            HDKTechContext context,
            ILogger<OrderController> logger,
            ISystemLogService logService)
        {
            _context = context;
            _logger = logger;
            _logService = logService;
        }

        // ──────────────────────────────────────────────────────────────
        // INDEX — danh sách đơn hàng với filter + phân trang
        // GET: /admin/order
        // ──────────────────────────────────────────────────────────────
        [HttpGet("")]
        [HttpGet("index")]
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

                // Guard: không thể đổi từ trạng thái cuối
                if (order.Status == 3 && newStatus != 4)
                    return Json(new { success = false, message = "Đơn hàng đã giao không thể thay đổi trạng thái." });

                var oldStatusName = GetStatusName(order.Status);
                order.Status = newStatus;
                await _context.SaveChangesAsync();

                // ── Khi đơn hàng giao thành công → trừ kho + audit log ──
                if (newStatus == 3 && order.Items != null)
                {
                    foreach (var item in order.Items)
                    {
                        var inv = await _context.Inventories
                            .FirstOrDefaultAsync(i => i.ProductId == item.ProductId);
                        if (inv != null)
                        {
                            inv.Quantity  = Math.Max(0, inv.Quantity - item.Quantity);
                            inv.UpdatedAt = DateTime.Now;
                        }
                    }
                    await _context.SaveChangesAsync();

                    await _logService.LogActionAsync(
                        username   : User.Identity?.Name ?? "System",
                        actionType : "OrderCompleted",
                        module     : "Order",
                        description: $"Đơn hàng #{order.OrderCode} giao thành công — đã trừ tồn kho.",
                        entityId   : order.Id.ToString(),
                        entityName : order.OrderCode,
                        oldValue   : oldStatusName,
                        newValue   : "Đã giao",
                        userRole   : User.IsInRole("Admin") ? "Admin" : "Manager",
                        userId     : User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);
                }

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
        public async Task<IActionResult> CancelOrder(int orderId)
        {
            try
            {
                var order = await _context.Orders.FindAsync(orderId);
                if (order == null)
                    return Json(new { success = false, message = "Không tìm thấy đơn hàng." });

                if (order.Status == 3)
                    return Json(new { success = false, message = "Không thể hủy đơn hàng đã giao thành công." });

                if (order.Status == 4)
                    return Json(new { success = false, message = "Đơn hàng đã bị hủy trước đó." });

                order.Status = 4;
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Đã hủy đơn hàng thành công." });
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
