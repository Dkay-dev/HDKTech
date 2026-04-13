using HDKTech.Data;
using HDKTech.Models;
using HDKTech.Models.Vnpay;
using HDKTech.Repositories.Interfaces;
using HDKTech.Services;
using HDKTech.Services.Vnpay;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

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
        private readonly HDKTechContext _context;

        public CheckoutController(
            IOrderRepository orderRepository,
            ICartService cartService,
            UserManager<AppUser> userManager,
            ILogger<CheckoutController> logger,
            IVnPayService vnPayService,
            HDKTechContext context)
        {
            _orderRepository = orderRepository;
            _cartService = cartService;
            _userManager = userManager;
            _logger = logger;
            _vnPayService = vnPayService;
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
                ShippingAddress = "",
                Items = cart.Items,
                TotalAmount = (decimal)cart.Items.Sum(x => x.Price * x.Quantity),
                SoProduct = cart.Items.Count,
                ShippingFee = 0
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

            // ✅ Chọn VNPay → redirect sang VNPay
            if (model.PaymentMethod == "VNPAY")
            {
                try
                {
                    TempData["RecipientName"] = model.RecipientName;
                    TempData["SoDienThoai"] = model.SoDienThoai;
                    TempData["ShippingAddress"] = model.ShippingAddress;
                    TempData["GhiChu"] = model.GhiChu;
                    TempData["ShippingFee"] = model.ShippingFee.ToString();

                    var totalAmount = model.TotalAmount + model.ShippingFee;

                    var paymentModel = new PaymentInformationModel
                    {
                        OrderType = "other",
                        Amount = (double)totalAmount,
                        OrderDescription = "Thanh toan don hang HDKTech",
                        Name = model.RecipientName
                    };

                    var paymentUrl = _vnPayService.CreatePaymentUrl(paymentModel, HttpContext);
                    _logger.LogInformation($"Redirect VNPay: user {user.Id}, amount {totalAmount}");

                    return Redirect(paymentUrl);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Lỗi tạo URL VNPay: {ex.Message}");
                    TempData["Error"] = "Không thể kết nối VNPay. Vui lòng thử lại.";
                    model.Items = cart.Items;
                    model.TotalAmount = (decimal)cart.Items.Sum(x => x.Price * x.Quantity);
                    model.SoProduct = cart.Items.Count;
                    return View(model);
                }
            }

            // ✅ COD → tạo đơn hàng luôn
            try
            {
                var order = await _orderRepository.CreateOrderAsync(
                    userId: user.Id,
                    RecipientName: model.RecipientName,
                    soDienThoai: model.SoDienThoai,
                    ShippingAddress: model.ShippingAddress,
                    items: cart.Items,
                    ShippingFee: model.ShippingFee,
                    paymentMethod: "COD",
                    paymentStatus: "Unpaid"
                );

                user.FullName = model.RecipientName;
                user.PhoneNumber = model.SoDienThoai;
                await _userManager.UpdateAsync(user);
                await _cartService.ClearCartAsync();

                _logger.LogInformation($"Đơn hàng COD #{order.OrderCode} tạo thành công");
                return RedirectToAction("Success", new { maOrder = order.OrderCode });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Lỗi khi tạo đơn hàng: {ex.Message}");
                TempData["Error"] = "Lỗi khi đặt hàng. Vui lòng thử lại.";
                model.Items = cart.Items;
                model.TotalAmount = (decimal)cart.Items.Sum(x => x.Price * x.Quantity);
                model.SoProduct = cart.Items.Count;
                return View(model);
            }
        }

        // GET: /Checkout/Success
        public async Task<IActionResult> Success(string maOrder)
        {
            if (string.IsNullOrEmpty(maOrder))
                return RedirectToAction("Index", "Home");

            var order = await _orderRepository.GetOrderByMaDonHangAsync(maOrder);
            if (order == null)
            {
                TempData["Error"] = "Không tìm thấy đơn hàng.";
                return RedirectToAction("Index", "Home");
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null || order.UserId != user.Id)
            {
                TempData["Error"] = "Bạn không có quyền xem đơn hàng này.";
                return RedirectToAction("Index", "Home");
            }

            return View(order);
        }

        // GET: /Checkout/PaymentCallbackVnpay
        [HttpGet]
        public async Task<IActionResult> PaymentCallbackVnpay()
        {
            var response = _vnPayService.PaymentExecute(Request.Query);

            // ✅ Luôn lưu log giao dịch VNPay vào DB dù thành công hay thất bại
            var vnpayLog = new VNPAYModel
            {
                OrderId = response.OrderId ?? "",
                PaymentMethod = "VNPay",
                OrderDescription = response.OrderDescription ?? "",
                TransactionId = response.TransactionId ?? "",
                PaymentId = response.PaymentId ?? "",
                Success = response.Success,
                VnPayResponseCode = response.VnPayResponseCode ?? "",
                CreatedDate = DateTime.Now
            };
            _context.VNPAYModels.Add(vnpayLog);
            await _context.SaveChangesAsync();

            // Thanh toán thất bại hoặc bị huỷ
            if (!response.Success || response.VnPayResponseCode != "00")
            {
                _logger.LogWarning($"VNPay callback thất bại: ResponseCode={response.VnPayResponseCode}");
                TempData["Error"] = $"Thanh toán VNPay thất bại (mã: {response.VnPayResponseCode}). Vui lòng thử lại.";
                return RedirectToAction("Index");
            }

            // ✅ Thanh toán thành công → tạo đơn hàng
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    TempData["Error"] = "Phiên đăng nhập hết hạn.";
                    return RedirectToAction("Login", "Account");
                }

                var cart = await _cartService.GetCartAsync();
                if (cart == null || !cart.Items.Any())
                {
                    TempData["Error"] = "Giỏ hàng đã hết. Đơn hàng có thể đã được tạo trước đó.";
                    return RedirectToAction("Index", "Home");
                }

                var recipientName = TempData["RecipientName"]?.ToString() ?? user.FullName ?? "";
                var soDienThoai = TempData["SoDienThoai"]?.ToString() ?? user.PhoneNumber ?? "";
                var shippingAddress = TempData["ShippingAddress"]?.ToString() ?? "";
                var shippingFee = decimal.TryParse(TempData["ShippingFee"]?.ToString(), out var fee) ? fee : 0;

                var order = await _orderRepository.CreateOrderAsync(
                    userId: user.Id,
                    RecipientName: recipientName,
                    soDienThoai: soDienThoai,
                    ShippingAddress: shippingAddress,
                    items: cart.Items,
                    ShippingFee: shippingFee,
                    paymentMethod: "VNPay",
                    paymentStatus: "Paid"
                );

                user.FullName = recipientName;
                user.PhoneNumber = soDienThoai;
                await _userManager.UpdateAsync(user);
                await _cartService.ClearCartAsync();

                _logger.LogInformation($"Đơn hàng VNPay #{order.OrderCode} tạo thành công, TransactionId={response.TransactionId}");

                return RedirectToAction("Success", new { maOrder = order.OrderCode });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Lỗi tạo đơn sau VNPay callback: {ex.Message}");
                TempData["Error"] = "Thanh toán thành công nhưng tạo đơn hàng thất bại. Vui lòng liên hệ hỗ trợ.";
                return RedirectToAction("Index", "Home");
            }
        }
    }
}