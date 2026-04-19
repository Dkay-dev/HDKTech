using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using HDKTech.Models;
using HDKTech.Repositories.Interfaces;

namespace HDKTech.Controllers
{
    /// <summary>
    /// OrderController — MERGED từ commit Huy 3f92de9 vào schema mới.
    /// Tính năng giữ / thêm:
    ///   • Details(maOrder)  — load đầy đủ Items + Product.Images + Category
    ///                         (ThenInclude được xử lý trong OrderRepository.GetOrderByMaDonHangAsync)
    ///   • Cancel(maOrder)   — huỷ đơn (chỉ khi Status = Pending / Confirmed & chưa giao)
    ///                         Bảo mật: verify UserId của đơn == user đăng nhập.
    /// Khoa's schema:
    ///   - Order.Status là enum OrderStatus (Pending, Confirmed, Packing, Shipping,
    ///     Delivered, Cancelled, Returned).
    ///   - OrderCode là string duy nhất, dùng làm mã tra cứu.
    /// </summary>
    [Authorize]
    public class OrderController : Controller
    {
        private readonly IOrderRepository _orderRepository;
        private readonly UserManager<AppUser> _userManager;
        private readonly ILogger<OrderController> _logger;

        public OrderController(
            IOrderRepository orderRepository,
            UserManager<AppUser> userManager,
            ILogger<OrderController> logger)
        {
            _orderRepository = orderRepository;
            _userManager = userManager;
            _logger = logger;
        }

        // ── GET /Order/MyOrders ──────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> MyOrders()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                TempData["Error"] = "Phiên đăng nhập hết hạn. Vui lòng đăng nhập lại.";
                return RedirectToAction("Login", "Account");
            }

            var orders = await _orderRepository.GetUserOrdersAsync(user.Id);
            return View(orders);
        }

        // ── GET /Order/Details/{maOrder} ─────────────────────────────
        [HttpGet("Order/Details/{maOrder}")]
        public async Task<IActionResult> Details(string maOrder)
        {
            if (string.IsNullOrWhiteSpace(maOrder))
            {
                TempData["Error"] = "Mã đơn hàng không hợp lệ.";
                return RedirectToAction(nameof(MyOrders));
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                TempData["Error"] = "Phiên đăng nhập hết hạn. Vui lòng đăng nhập lại.";
                return RedirectToAction("Login", "Account");
            }

            var order = await _orderRepository.GetOrderByMaDonHangAsync(maOrder);
            if (order == null)
            {
                TempData["Error"] = "Không tìm thấy đơn hàng.";
                return RedirectToAction(nameof(MyOrders));
            }

            // ✅ Security: đơn phải thuộc user đang đăng nhập
            if (order.UserId != user.Id)
            {
                _logger.LogWarning(
                    "Unauthorized access to order {Code} by user {UserId}",
                    maOrder, user.Id);
                TempData["Error"] = "Bạn không có quyền xem đơn hàng này.";
                return RedirectToAction(nameof(MyOrders));
            }

            return View(order);
        }

        // ── POST /Order/Cancel/{maOrder} ─────────────────────────────
        //  Logic: "Hủy đơn hàng phải đi kèm xem chi tiết" — form hủy nằm
        //  ngay trong trang Details.cshtml, chỉ enable khi đơn ở trạng thái
        //  có thể huỷ (Pending / Confirmed). Sau khi huỷ, chuyển về Details
        //  để user thấy trạng thái đã cập nhật.
        [HttpPost("Order/Cancel/{maOrder}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(string maOrder, string? cancelReason)
        {
            if (string.IsNullOrWhiteSpace(maOrder))
            {
                TempData["Error"] = "Mã đơn hàng không hợp lệ.";
                return RedirectToAction(nameof(MyOrders));
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                TempData["Error"] = "Phiên đăng nhập hết hạn. Vui lòng đăng nhập lại.";
                return RedirectToAction("Login", "Account");
            }

            var order = await _orderRepository.GetOrderByMaDonHangAsync(maOrder);
            if (order == null)
            {
                TempData["Error"] = "Không tìm thấy đơn hàng.";
                return RedirectToAction(nameof(MyOrders));
            }

            if (order.UserId != user.Id)
            {
                _logger.LogWarning(
                    "Unauthorized cancel attempt on order {Code} by user {UserId}",
                    maOrder, user.Id);
                TempData["Error"] = "Bạn không có quyền huỷ đơn hàng này.";
                return RedirectToAction(nameof(MyOrders));
            }

            // Chỉ cho huỷ khi đơn CHƯA đi đóng gói / đi giao
            if (order.Status != OrderStatus.Pending &&
                order.Status != OrderStatus.Confirmed)
            {
                TempData["Error"] = "Đơn hàng đã được xử lý, không thể huỷ.";
                return RedirectToAction(nameof(Details), new { maOrder });
            }

            var (ok, err) = await _orderRepository.CancelOrderAsync(
                orderId: order.Id,
                userId: user.Id,
                cancelReason: cancelReason);

            if (!ok)
            {
                TempData["Error"] = err ?? "Không thể huỷ đơn hàng.";
                _logger.LogError("Cancel order {Code} failed: {Err}", maOrder, err);
                return RedirectToAction(nameof(Details), new { maOrder });
            }

            _logger.LogInformation(
                "Order {Code} cancelled by user {UserId}", maOrder, user.Id);
            TempData["Success"] = $"Đơn hàng #{maOrder} đã được huỷ.";
            return RedirectToAction(nameof(Details), new { maOrder });
        }
    }
}
