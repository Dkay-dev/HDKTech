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
            IMomoService momoService,               // ✅ THÊM MỚI
            HDKTechContext context)
        {
            _orderRepository = orderRepository;
            _cartService = cartService;
            _userManager = userManager;
            _logger = logger;
            _vnPayService = vnPayService;
            _momoService = momoService;             // ✅ THÊM MỚI
            _context = context;
        }

        // GET: /Checkout
        [HttpGet]
        public async Task<IActionResult> Index(string? selectedItems = null)
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

            List<CartItem> selectedCartItems = cart.Items.ToList();
            List<int> selectedProductIds = new List<int>();

            // Nếu có selectedItems, lọc chỉ lấy các sản phẩm đã chọn
            if (!string.IsNullOrEmpty(selectedItems))
            {
                try
                {
                    selectedProductIds = System.Text.Json.JsonSerializer.Deserialize<List<int>>(selectedItems) ?? new List<int>();
                    selectedCartItems = cart.Items.Where(x => selectedProductIds.Contains(x.ProductId)).ToList();
                }
                catch
                {
                    selectedCartItems = cart.Items.ToList();
                }
            }

            if (!selectedCartItems.Any())
            {
                TempData["Error"] = "Vui lòng chọn ít nhất một sản phẩm để mua.";
                return RedirectToAction("Index", "Cart");
            }

            var viewModel = new CheckoutViewModel
            {
                RecipientName = user.FullName ?? "",
                Email = user.Email ?? "",
                SoDienThoai = user.PhoneNumber ?? "",
                ShippingAddress = "",
                Items = selectedCartItems,
                TotalAmount = (decimal)selectedCartItems.Sum(x => x.Price * x.Quantity),
                SoProduct = selectedCartItems.Count,
                ShippingFee = 0
            };

            // Lưu danh sách sản phẩm đã chọn vào TempData để dùng khi POST
            if (selectedProductIds.Any())
            {
                TempData["SelectedProductIds"] = System.Text.Json.JsonSerializer.Serialize(selectedProductIds);
            }

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

            // Lấy danh sách sản phẩm đã chọn từ TempData
            List<CartItem> itemsToOrder = cart.Items.ToList();
            List<int> selectedProductIds = new List<int>();

            var selectedItemsJson = TempData["SelectedProductIds"]?.ToString();
            if (!string.IsNullOrEmpty(selectedItemsJson))
            {
                try
                {
                    selectedProductIds = System.Text.Json.JsonSerializer.Deserialize<List<int>>(selectedItemsJson) ?? new List<int>();
                    itemsToOrder = cart.Items.Where(x => selectedProductIds.Contains(x.ProductId)).ToList();
                }
                catch
                {
                    itemsToOrder = cart.Items.ToList();
                }
            }

            if (!itemsToOrder.Any())
            {
                TempData["Error"] = "Vui lòng chọn ít nhất một sản phẩm để mua.";
                return RedirectToAction("Index", "Cart");
            }

            if (!ModelState.IsValid)
            {
                model.Items = itemsToOrder;
                model.TotalAmount = (decimal)itemsToOrder.Sum(x => x.Price * x.Quantity);
                model.SoProduct = itemsToOrder.Count;
                return View(model);
            }

            var totalAmount = model.TotalAmount + model.ShippingFee;

            // Lưu thông tin giao hàng vào TempData (dùng cho cả VNPay và MoMo callback)
            TempData["RecipientName"] = model.RecipientName;
            TempData["SoDienThoai"] = model.SoDienThoai;
            TempData["ShippingAddress"] = model.ShippingAddress;
            TempData["GhiChu"] = model.GhiChu;
            TempData["ShippingFee"] = model.ShippingFee.ToString();
            TempData["SelectedProductIds"] = System.Text.Json.JsonSerializer.Serialize(selectedProductIds);

            // ✅ VNPay
            if (model.PaymentMethod == "VNPAY")
            {
                try
                {
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
                    model.Items = itemsToOrder;
                    model.TotalAmount = (decimal)itemsToOrder.Sum(x => x.Price * x.Quantity);
                    model.SoProduct = itemsToOrder.Count;
                    return View(model);
                }
            }

            // ✅ MoMo — xử lý trực tiếp tại đây, redirect sang cổng thanh toán
            if (model.PaymentMethod == "Momo")
            {
                try
                {
                    var orderInfoModel = new OrderInfoModel
                    {
                        FullName = model.RecipientName,
                        Amount = (long)totalAmount,
                        OrderInfo = "Thanh toan don hang HDKTech"
                    };

                    var momoResponse = await _momoService.CreatePaymentAsync(orderInfoModel);

                    if (momoResponse == null || string.IsNullOrEmpty(momoResponse.PayUrl))
                    {
                        _logger.LogError($"MoMo trả về lỗi: {momoResponse?.Message}");
                        TempData["Error"] = $"Không thể kết nối MoMo: {momoResponse?.Message ?? "Lỗi không xác định"}";
                        model.Items = itemsToOrder;
                        model.TotalAmount = (decimal)itemsToOrder.Sum(x => x.Price * x.Quantity);
                        model.SoProduct = itemsToOrder.Count;
                        return View(model);
                    }

                    // Lưu MoMo OrderId để dùng khi callback
                    TempData["MomoOrderId"] = momoResponse.OrderId;
                    _logger.LogInformation($"Redirect MoMo: user {user.Id}, amount {totalAmount}, orderId={momoResponse.OrderId}");

                    return Redirect(momoResponse.PayUrl);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Lỗi tạo URL MoMo: {ex.Message}");
                    TempData["Error"] = "Không thể kết nối MoMo. Vui lòng thử lại.";
                    model.Items = itemsToOrder;
                    model.TotalAmount = (decimal)itemsToOrder.Sum(x => x.Price * x.Quantity);
                    model.SoProduct = itemsToOrder.Count;
                    return View(model);
                }
            }

            // ✅ COD
            try
            {
                var order = await _orderRepository.CreateOrderAsync(
                    userId: user.Id,
                    RecipientName: model.RecipientName,
                    soDienThoai: model.SoDienThoai,
                    ShippingAddress: model.ShippingAddress,
                    items: itemsToOrder,
                    ShippingFee: model.ShippingFee,
                    paymentMethod: "COD",
                    paymentStatus: "Unpaid"
                );

                user.FullName = model.RecipientName;
                user.PhoneNumber = model.SoDienThoai;
                await _userManager.UpdateAsync(user);

                // Xóa các sản phẩm đã đặt khỏi giỏ hàng
                foreach (var item in itemsToOrder)
                {
                    await _cartService.RemoveItemAsync(item.ProductId);
                }

                _logger.LogInformation($"Đơn hàng COD #{order.OrderCode} tạo thành công");
                return RedirectToAction("Success", new { maOrder = order.OrderCode });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Lỗi khi tạo đơn hàng: {ex.Message}");
                TempData["Error"] = "Lỗi khi đặt hàng. Vui lòng thử lại.";
                model.Items = itemsToOrder;
                model.TotalAmount = (decimal)itemsToOrder.Sum(x => x.Price * x.Quantity);
                model.SoProduct = itemsToOrder.Count;
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

            // ✅ Lưu log giao dịch VNPay vào DB
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

            if (!response.Success || response.VnPayResponseCode != "00")
            {
                _logger.LogWarning($"VNPay callback thất bại: ResponseCode={response.VnPayResponseCode}");
                TempData["Error"] = $"Thanh toán VNPay thất bại (mã: {response.VnPayResponseCode}). Vui lòng thử lại.";
                return RedirectToAction("Index");
            }

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
                    paymentMethod: "MoMo",
                    paymentStatus: "Paid"
                );

                user.FullName = recipientName;
                user.PhoneNumber = soDienThoai;
                await _userManager.UpdateAsync(user);
                await _cartService.ClearCartAsync();

                _logger.LogInformation($"Đơn hàng MoMo #{order.OrderCode} tạo thành công, orderId={response.OrderId}");
                return RedirectToAction("Success", new { maOrder = order.OrderCode });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Lỗi tạo đơn sau MoMo callback: {ex.Message}");
                TempData["Error"] = "Thanh toán thành công nhưng tạo đơn hàng thất bại. Vui lòng liên hệ hỗ trợ.";
                return RedirectToAction("Index", "Home");
            }
        }

        // POST: /Checkout/MomoNotify  ← MoMo IPN (server-to-server)
        [HttpPost]
        [AllowAnonymous]
        [IgnoreAntiforgeryToken]
        public IActionResult MomoNotify()
        {
            // MoMo gọi IPN để xác nhận server-side — luôn trả 200 OK
            _logger.LogInformation("MoMo IPN nhận thành công");
            return Ok(new { status = 0, message = "Nhận thông báo thành công" });
        }
    }
}