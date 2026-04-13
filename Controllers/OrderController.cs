using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using HDKTech.Models;
using HDKTech.Repositories.Interfaces;

using HDKTech.Areas.Admin.Repositories;

namespace HDKTech.Controllers
{
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

        // GET: /Order/MyOrders
        [HttpGet]
        public async Task<IActionResult> MyOrders()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                TempData["Error"] = "Phiên đăng nhập hết hạn. Vui lòng đăng nhập lại.";
                return RedirectToAction("Login", "Account");
            }

            var userOrders = await _orderRepository.GetUserOrdersAsync(user.Id);
            
            return View(userOrders);
        }

        // GET: /Order/Details/{maOrder}
        [HttpGet("Order/Details/{maOrder}")]
        public async Task<IActionResult> Details(string maOrder)
        {
            if (string.IsNullOrEmpty(maOrder))
            {
                TempData["Error"] = "Mã đơn hàng không hợp lệ.";
                return RedirectToAction("MyOrders");
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                TempData["Error"] = "Phiên đăng nhập hết hạn. Vui lòng đăng nhập lại.";
                return RedirectToAction("Login", "Account");
            }

            var Order = await _orderRepository.GetOrderByMaDonHangAsync(maOrder);
            if (Order == null)
            {
                TempData["Error"] = "Không tìm thấy đơn hàng.";
                return RedirectToAction("MyOrders");
            }

            // ✅ Security Check: Verify user owns this order
            if (Order.UserId != user.Id)
            {
                _logger.LogWarning($"Unauthorized access attempt to order {maOrder} by user {user.Id}");
                TempData["Error"] = "Bạn không có quyền xem đơn hàng này.";
                return RedirectToAction("MyOrders");
            }

            return View(Order);
        }
    }
}


