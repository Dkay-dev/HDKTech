using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using HDKTech.Models;
using HDKTech.Repositories.Interfaces;

namespace HDKTech.Controllers
{
    [Authorize]
    public class OrderController : Controller
    {
        private readonly IOrderRepository _orderRepository;
        private readonly UserManager<NguoiDung> _userManager;
        private readonly ILogger<OrderController> _logger;

        public OrderController(
            IOrderRepository orderRepository,
            UserManager<NguoiDung> userManager,
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

        // GET: /Order/Details/{maDonHang}
        [HttpGet("Order/Details/{maDonHang}")]
        public async Task<IActionResult> Details(string maDonHang)
        {
            if (string.IsNullOrEmpty(maDonHang))
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

            var donHang = await _orderRepository.GetOrderByMaDonHangAsync(maDonHang);
            if (donHang == null)
            {
                TempData["Error"] = "Không tìm thấy đơn hàng.";
                return RedirectToAction("MyOrders");
            }

            // ✅ Security Check: Verify user owns this order
            if (donHang.MaNguoiDung != user.Id)
            {
                _logger.LogWarning($"Unauthorized access attempt to order {maDonHang} by user {user.Id}");
                TempData["Error"] = "Bạn không có quyền xem đơn hàng này.";
                return RedirectToAction("MyOrders");
            }

            return View(donHang);
        }
    }
}
