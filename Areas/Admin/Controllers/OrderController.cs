using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using HDKTech.Models;
using HDKTech.Utilities;
using HDKTech.Areas.Admin.Services;
using HDKTech.Areas.Admin.Services.Interfaces;

namespace HDKTech.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Policy = "RequireAdminArea")]
    [Route("admin/[controller]")]
    public class OrderController : Controller
    {
        private readonly IOrderAdminService           _orderService;
        private readonly ILogger<OrderController>     _logger;

        public OrderController(
            IOrderAdminService       orderService,
            ILogger<OrderController> logger)
        {
            _orderService = orderService;
            _logger       = logger;
        }

        [HttpGet("")]
        [HttpGet("index")]
        [Authorize(Policy = "Order.Read")]
        public async Task<IActionResult> Index(
            int page          = 1,
            int pageSize      = 20,
            string searchTerm = "",
            int statusFilter  = -1,
            string sortBy     = "date")
        {
            try
            {
                var result = await _orderService.GetOrdersPagedAsync(page, pageSize, searchTerm, statusFilter, sortBy);

                ViewBag.Orders          = result.Orders;
                ViewBag.TotalCount      = result.TotalCount;
                ViewBag.Page            = page;
                ViewBag.PageSize        = pageSize;
                ViewBag.TotalPages      = (int)Math.Ceiling((double)result.TotalCount / pageSize);
                ViewBag.SearchTerm      = searchTerm;
                ViewBag.StatusFilter    = statusFilter;
                ViewBag.SortBy          = sortBy;
                ViewBag.PendingCount    = result.PendingCount;
                ViewBag.ProcessingCount = result.ProcessingCount;
                ViewBag.ShippingCount   = result.ShippingCount;
                ViewBag.DeliveredCount  = result.DeliveredCount;
                ViewBag.CancelledCount  = result.CancelledCount;
                ViewBag.TodayRevenue    = result.TodayRevenue;
                ViewBag.TodayOrderCount = result.TodayOrderCount;

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi tải danh sách đơn hàng");
                TempData["Error"] = "Lỗi khi tải danh sách đơn hàng.";
                return View();
            }
        }

        [HttpGet("details/{id:int}")]
        [Authorize(Policy = "Order.Read")]
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var order = await _orderService.GetOrderDetailsAsync(id);
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

        [HttpPost("update-status")]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "Order.Update")]
        public async Task<IActionResult> UpdateStatus(int orderId, int newStatus)
        {
            try
            {
                var username = User.Identity?.Name ?? "System";
                var userId   = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                var userRole = User.IsInRole("Admin") ? "Admin" : "Manager";

                var (success, message) = await _orderService.UpdateStatusAsync(orderId, newStatus, username, userId);

                if (success)
                {
                    var order = await _orderService.GetOrderDetailsAsync(orderId);
                    if (order != null)
                    {
                        await LoggingHelper.LogOrderStatusChangeAsync(
                            username  : username,
                            orderId   : order.Id,
                            orderCode : order.OrderCode,
                            oldStatus : "Trước",
                            newStatus : OrderAdminService.GetStatusName((OrderStatus)newStatus),
                            userId    : userId,
                            userRole  : userRole);
                    }
                }

                return Json(new { success, message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi cập nhật trạng thái đơn hàng Id: {Id}", orderId);
                return Json(new { success = false, message = "Lỗi khi cập nhật trạng thái." });
            }
        }

        [HttpPost("cancel")]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "Order.Delete")]
        public async Task<IActionResult> CancelOrder(int orderId)
        {
            try
            {
                var username = User.Identity?.Name ?? "System";
                var userId   = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                var userRole = User.IsInRole("Admin") ? "Admin" : "Manager";

                var (success, message) = await _orderService.CancelOrderAsync(orderId, username, userId);

                if (success)
                {
                    var order = await _orderService.GetOrderDetailsAsync(orderId);
                    if (order != null)
                    {
                        await LoggingHelper.LogOrderStatusChangeAsync(
                            username  : username,
                            orderId   : order.Id,
                            orderCode : order.OrderCode,
                            oldStatus : "Trước",
                            newStatus : "Đã hủy",
                            userId    : userId,
                            userRole  : userRole);
                    }
                }

                return Json(new { success, message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi hủy đơn hàng Id: {Id}", orderId);
                return Json(new { success = false, message = "Lỗi khi hủy đơn hàng." });
            }
        }

        [HttpGet("export")]
        public async Task<IActionResult> Export(string searchTerm = "", int statusFilter = -1)
        {
            var orders = await _orderService.GetOrdersForExportAsync(searchTerm, statusFilter);
            var csv    = "Mã Đơn Hàng,Người Nhận,Số ĐT,Địa chỉ,Tổng Tiền,Phí Ship,Trạng Thái,Ngày Đặt\n";

            foreach (var o in orders)
                csv += $"\"{o.OrderCode}\",\"{o.RecipientName}\",\"{o.RecipientPhone}\"," +
                       $"\"{o.ShippingAddressLine}\",{o.TotalAmount},{o.ShippingFee}," +
                       $"\"{OrderAdminService.GetStatusName(o.Status)}\",{o.OrderDate:yyyy-MM-dd}\n";

            return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv",
                        $"orders_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
        }
    }
}
