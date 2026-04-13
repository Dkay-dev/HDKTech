using HDKTech.Data;
using HDKTech.Models;
using HDKTech.Models.Momo;
using HDKTech.Models.Vnpay;
using HDKTech.Repositories.Interfaces;
using HDKTech.Services;
using HDKTech.Services.Momo;
using HDKTech.Services.Vnpay;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

using HDKTech.Areas.Admin.Repositories;

namespace HDKTech.Controllers
{
    [Authorize]
    public class CheckoutController : Controller
    {
        private readonly IOrderRepository _orderRepository;
        private readonly ICartService _cartService;
        private readonly UserManager<AppUser> _userManager;
        private readonly ILogger<CheckoutController> _logger;
        private readonly IVnPayService _vnPayService;
        private readonly IMomoService _momoService;   // ✅ THÊM MỚI
        private readonly HDKTechContext _context;

        public CheckoutController(
            IOrderRepository orderRepository,
            ICartService cartService,
            UserManager<AppUser> userManager,
            ILogger<CheckoutController> logger,
            IVnPayService vnPayService,
            IMomoService momoService,
            HDKTechContext context)
        {
            _orderRepository = orderRepository;
            _cartService = cartService;
            _userManager = userManager;
            _logger = logger;
            _vnPayService = vnPayService;
            _momoService = momoService;
            _context = context;
        }

        // GET: /Checkout
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var cart = await _cartService.GetCartAsync();

            if (cart == null || !cart.Items.Any())
            {
                TempData["Error"] = "Giỏ hàng trống. Vui lòng thêm sản phẩm trước.";
                return RedirectToAction("Index", "Cart");
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Index", "Checkout") });

            var viewModel = new CheckoutViewModel
            {
                RecipientName = user.FullName ?? "",
                Email = user.Email ?? "",
                SoDienThoai = user.PhoneNumber ?? "",
                ShippingAddress = "", // Không có DiaChi trong AppUser
                Items = cart.Items,
                TotalAmount = (decimal)cart.Items.Sum(x => x.Price * x.Quantity),
                SoProduct = cart.Items.Count,
                ShippingFee = 0 // Có thể tính động dựa trên địa chỉ
            };

            return View(viewModel);
        }

        // POST: /Checkout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(CheckoutViewModel model)
        {
            var cart = await _cartService.GetCartAsync();
            if (cart == null || !cart.Items.Any())
            {
                TempData["Error"] = "Giỏ hàng trống.";
                return RedirectToAction("Index", "Cart");
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                TempData["Error"] = "Phiên đăng nhập hết hạn. Vui lòng đăng nhập lại.";
                return RedirectToAction("Login", "Account");
            }

            if (!ModelState.IsValid)
            {
                model.Items = cart.Items;
                model.TotalAmount = (decimal)cart.Items.Sum(x => x.Price * x.Quantity);
                model.SoProduct = cart.Items.Count;
                return View(model);
            }

            try
            {
                // ✅ Fix #2: Wrap toàn bộ luồng tạo đơn hàng trong try-catch đầy đủ
                // Bước 1: Tạo đơn hàng và lưu vào database
                var donHang = await _orderRepository.CreateOrderAsync(
                    userId: user.Id,
                    RecipientName: model.RecipientName,
                    soDienThoai: model.SoDienThoai,
                    ShippingAddress: model.ShippingAddress,
                    items: cart.Items,
                    ShippingFee: model.ShippingFee
                );

                // Bước 2: Kiểm tra đơn hàng đã được tạo thành công chưa
                // (SaveChangesAsync đã chạy bên trong CreateOrderAsync, nếu lỗi sẽ throw exception)
                if (donHang == null || donHang.Id <= 0)
                {
                    throw new InvalidOperationException("Tạo đơn hàng thất bại - không nhận được Id từ database.");
                }

                // Bước 3: Cập nhật thông tin profile user (không bắt buộc, không throw nếu lỗi)
                try
                {
                    user.FullName    = model.RecipientName;
                    user.PhoneNumber = model.SoDienThoai;
                    await _userManager.UpdateAsync(user);
                }
                catch (Exception exProfile)
                {
                    // Lỗi cập nhật profile không ảnh hưởng đến đơn hàng
                    _logger.LogWarning(exProfile, "Không thể cập nhật profile user {UserId}", user.Id);
                }

                // Bước 4: Xoá giỏ hàng
                await _cartService.ClearCartAsync();

                // ✅ Sinh mã đơn hàng hiển thị dạng HDK-xxxxx từ OrderCode thực tế
                // OrderCode trong DB: "HDK20260413123456_1234" → hiển thị: "HDK-1234"
                var maHienThi = donHang.OrderCode; // OrderCode đã tự sinh trong CreateOrderAsync

                _logger.LogInformation(
                    "✅ Đơn hàng #{MaDonHang} (Id={Id}) tạo thành công bởi user {UserId}",
                    maHienThi, donHang.Id, user.Id);

                // Bước 5: Redirect sang trang thành công, truyền mã đơn hàng
                return RedirectToAction("Success", new { maOrder = donHang.OrderCode });
            }
            catch (Exception ex)
            {
                // ✅ Fix #2: Log chi tiết lỗi để debug dễ hơn
                _logger.LogError(ex, "❌ Lỗi khi tạo đơn hàng cho user {UserId}: {Message}", user.Id, ex.Message);

                // Hiển thị thông báo lỗi thân thiện cho người dùng (không lộ technical details)
                TempData["Error"] = "Đặt hàng thất bại. Vui lòng thử lại hoặc liên hệ hỗ trợ.";

                // Khôi phục dữ liệu view
                model.Items       = cart.Items;
                model.TotalAmount = (decimal)cart.Items.Sum(x => x.Price * x.Quantity);
                model.SoProduct   = cart.Items.Count;
                return View(model);
            }
        }

        // GET: /Checkout/Success
        public async Task<IActionResult> Success(string maOrder)
        {
            if (string.IsNullOrEmpty(maOrder))
            {
                return RedirectToAction("Index", "Home");
            }

            var Order = await _orderRepository.GetOrderByMaDonHangAsync(maOrder);
            if (Order == null)
            {
                _logger.LogError($"Lỗi tạo đơn sau VNPay callback");
                TempData["Error"] = "Thanh toán thành công nhưng tạo đơn hàng thất bại. Vui lòng liên hệ hỗ trợ.";
                return RedirectToAction("Index", "Home");
            }

            return View(Order);
        }

        // GET: /Checkout/PaymentCallBack  ← MoMo redirect về đây
        [HttpGet]
        public async Task<IActionResult> PaymentCallBack()
        {
            var response = _momoService.PaymentExecute(Request.Query);

            // ✅ Kiểm tra chữ ký hợp lệ
            if (!_momoService.ValidateSignature(Request.Query))
            {
                _logger.LogWarning($"MoMo callback: xác thực chữ ký thất bại, orderId={response.OrderId}");
                TempData["Error"] = "Xác thực thanh toán MoMo thất bại. Vui lòng liên hệ hỗ trợ.";
                return RedirectToAction("Index");
            }

            // ✅ Kiểm tra resultCode = 0 là thành công
            var resultCode = Request.Query["resultCode"].ToString();
            if (resultCode != "0")
            {
                _logger.LogWarning($"MoMo callback thất bại: resultCode={resultCode}, orderId={response.OrderId}");
                TempData["Error"] = $"Thanh toán MoMo thất bại (mã: {resultCode}). Vui lòng thử lại.";
                return RedirectToAction("Index");
            }

            // ✅ Security Check: Verify user owns this order
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                _logger.LogWarning($"Unauthorized access: null user");
                TempData["Error"] = "Bạn phải đăng nhập để xem đơn hàng."; 
                return RedirectToAction("Index", "Home");
            }

            // Get order by order ID from response
            var order = await _orderRepository.GetOrderByMaDonHangAsync(response.OrderId);
            if (order == null || order.UserId != user.Id)
            {
                _logger.LogWarning($"Unauthorized access attempt to order {response.OrderId} by user {user?.Id}");
                TempData["Error"] = "Bạn không có quyền xem đơn hàng này.";
                return RedirectToAction("Index", "Home");
            }

            return View(order);
        }
    }
}


