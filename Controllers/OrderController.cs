using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HDKTech.Models;
using HDKTech.Data;
using HDKTech.Repositories.Interfaces;

namespace HDKTech.Controllers
{
    [Authorize]
    public class OrderController : Controller
    {
        private readonly IOrderRepository _orderRepository;
        private readonly UserManager<AppUser> _userManager;
        private readonly ILogger<OrderController> _logger;
        private readonly HDKTechContext _context;

        public OrderController(
            IOrderRepository orderRepository,
            UserManager<AppUser> userManager,
            ILogger<OrderController> logger,
            HDKTechContext context)
        {
            _orderRepository = orderRepository;
            _userManager = userManager;
            _logger = logger;
            _context = context;
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

        // GET: /Order/Details/{id}
        [HttpGet("Order/Details/{id}")]
        public async Task<IActionResult> Details(int id)
        {
            if (id <= 0)
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

            var order = await _context.Orders
                .Include(x => x.Items)
                    .ThenInclude(i => i.Product)
                        .ThenInclude(p => p!.Images)
                .Include(x => x.Items)
                    .ThenInclude(i => i.Product)
                        .ThenInclude(p => p!.Category)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (order == null)
            {
                TempData["Error"] = "Không tìm thấy đơn hàng.";
                return RedirectToAction("MyOrders");
            }

            // ✅ Security Check: Verify user owns this order
            if (order.UserId != user.Id)
            {
                _logger.LogWarning($"Unauthorized access attempt to order {id} by user {user.Id}");
                TempData["Error"] = "Bạn không có quyền xem đơn hàng này.";
                return RedirectToAction("MyOrders");
            }

            return View(order);
        }
    }
}


