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
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace HDKTech.Controllers
{
    /// <summary>
    /// CheckoutController — MERGED Huy 3f92de9 → Khoa's schema.
    ///
    /// Tính năng mới giữ từ Huy:
    ///   • selectedItems (JSON array [{productId, productVariantId}]):
    ///     user có thể tick một số dòng ở Cart rồi chỉ checkout những dòng đó.
    ///     - Nếu không có selectedItems → fallback checkout toàn bộ cart (hành vi cũ).
    ///     - selectedItems được persist qua TempData giữa GET và POST của cùng lần checkout.
    ///     - Sau khi COD tạo đơn thành công → CHỈ xoá những item đã check ra khỏi cart,
    ///       KHÔNG ClearCartAsync (để user còn lại phần giỏ chưa chọn).
    ///     - Đối với VNPay / MoMo: sau callback thành công cũng chỉ xoá items đã check
    ///       (thông tin items được lưu trong PendingCheckout.CartSnapshot đầy đủ).
    ///
    /// Schema Khoa:
    ///   • CartItem key = (ProductId, ProductVariantId)
    ///   • OrderStatus enum, PendingCheckout idempotency, PaymentTransaction,
    ///     PromotionService, EmailService fire-and-forget.
    /// </summary>
    [Authorize]
    public class CheckoutController : Controller
    {
        private readonly IOrderRepository _orderRepository;
        private readonly ICartService _cartService;
        private readonly UserManager<AppUser> _userManager;
        private readonly ILogger<CheckoutController> _logger;
        private readonly IVnPayService _vnPayService;
        private readonly IMomoService _momoService;
        private readonly HDKTechContext _context;
        private readonly IPromotionService _promotionService;
        private readonly IEmailService _emailService;

        public CheckoutController(
            IOrderRepository orderRepository,
            ICartService cartService,
            UserManager<AppUser> userManager,
            ILogger<CheckoutController> logger,
            IVnPayService vnPayService,
            IMomoService momoService,
            HDKTechContext context,
            IPromotionService promotionService,
            IEmailService emailService)
        {
            _orderRepository = orderRepository;
            _cartService = cartService;
            _userManager = userManager;
            _logger = logger;
            _vnPayService = vnPayService;
            _momoService = momoService;
            _context = context;
            _promotionService = promotionService;
            _emailService = emailService;
        }

        // ════════════════════════════════════════════════════════════
        //  HELPER: Parse selectedItems JSON + lọc CartItem list
        // ════════════════════════════════════════════════════════════
        private const string SELECTED_ITEMS_KEY = "SelectedCheckoutItems";

        private record SelectedKey(int ProductId, int ProductVariantId);

        private static List<SelectedKey>? ParseSelectedItems(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            try
            {
                var arr = JsonSerializer.Deserialize<List<SelectedKey>>(
                    json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return (arr != null && arr.Count > 0) ? arr : null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Áp filter selectedItems lên cart.Items.
        /// Nếu selected == null hoặc rỗng → trả về toàn bộ items (hành vi cũ).
        /// </summary>
        private static List<CartItem> FilterCartItems(List<CartItem> allItems,
                                                      List<SelectedKey>? selected)
        {
            if (selected == null || selected.Count == 0) return allItems;
            var set = selected
                .Select(s => (s.ProductId, s.ProductVariantId))
                .ToHashSet();
            return allItems
                .Where(i => set.Contains((i.ProductId, i.ProductVariantId)))
                .ToList();
        }

        // ── GET /Checkout ────────────────────────────────────────────
        // selectedItems là JSON array kiểu: [{"productId":1,"productVariantId":2}, ...]
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
                return RedirectToAction("Login", "Account",
                    new { returnUrl = Url.Action("Index", "Checkout") });

            // Persist selectedItems qua POST — dùng TempData.Peek để không mất giá trị.
            if (!string.IsNullOrWhiteSpace(selectedItems))
            {
                TempData[SELECTED_ITEMS_KEY] = selectedItems;
            }

            var selected = ParseSelectedItems(selectedItems);
            var itemsToShow = FilterCartItems(cart.Items.ToList(), selected);

            if (itemsToShow.Count == 0)
            {
                // selectedItems có mặt nhưng không khớp với cart → fallback toàn bộ
                TempData["Warning"] = "Các sản phẩm đã chọn không còn trong giỏ hàng. Hiển thị toàn bộ giỏ.";
                itemsToShow = cart.Items.ToList();
                TempData.Remove(SELECTED_ITEMS_KEY);
            }

            decimal subTotal = itemsToShow.Sum(i => i.Price * i.Quantity);

            var viewModel = new CheckoutViewModel
            {
                RecipientName = user.FullName ?? "",
                Email = user.Email ?? "",
                SoDienThoai = user.PhoneNumber ?? "",
                ShippingAddress = "",
                Items = itemsToShow,
                TotalAmount = subTotal,
                SoProduct = itemsToShow.Count,
                ShippingFee = 0
            };

            return View(viewModel);
        }

        // ── POST /Checkout ───────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("checkout")]
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

            // ─── Áp filter selectedItems (nếu có trong TempData) ────────
            string? selectedItemsJson = TempData.Peek(SELECTED_ITEMS_KEY) as string;
            var selected = ParseSelectedItems(selectedItemsJson);
            var checkoutItems = FilterCartItems(cart.Items.ToList(), selected);

            if (checkoutItems.Count == 0)
            {
                TempData["Error"] = "Không có sản phẩm nào để thanh toán.";
                return RedirectToAction("Index", "Cart");
            }

            if (!ModelState.IsValid)
            {
                model.Items = checkoutItems;
                model.TotalAmount = checkoutItems.Sum(i => i.Price * i.Quantity);
                model.SoProduct = checkoutItems.Count;
                return View(model);
            }

            // ─── Server-side sanitize đầu vào ───────────────────────
            model.RecipientName = SanitizeText(model.RecipientName, 100);
            model.ShippingAddress = SanitizeText(model.ShippingAddress, 500);
            model.SoDienThoai = SanitizeText(model.SoDienThoai, 20);
            model.GhiChu = model.GhiChu != null ? SanitizeText(model.GhiChu, 500) : null;

            // ─── Re-fetch giá từ DB & tính shipping server-side ─────
            var variantIds = checkoutItems.Select(i => i.ProductVariantId).Distinct().ToList();
            var variants = await _context.ProductVariants.AsNoTracking()
                .Where(v => variantIds.Contains(v.Id))
                .ToDictionaryAsync(v => v.Id);

            decimal subTotal = checkoutItems.Sum(i =>
                variants.TryGetValue(i.ProductVariantId, out var v) ? v.Price * i.Quantity : 0);

            decimal shippingFee = CalculateShippingFee(subTotal);
            decimal discount = 0;

            // ─── Validate & áp mã giảm giá ──────────────────────────
            if (!string.IsNullOrWhiteSpace(model.PromoCode))
            {
                try
                {
                    var promoResult = await _promotionService.CalculateDiscountAsync(
                        promoCode: model.PromoCode.Trim(),
                        userId: user.Id,
                        subTotal: subTotal,
                        cartItems: checkoutItems,
                        originalShippingFee: shippingFee);

                    if (promoResult.IsValid)
                    {
                        discount = promoResult.DiscountAmount;
                        shippingFee = promoResult.AdjustedShippingFee;
                        _logger.LogInformation(
                            "PromoCode '{Code}' áp cho User {UserId}: -{Discount}đ",
                            model.PromoCode, user.Id, discount);
                    }
                    else
                    {
                        ModelState.AddModelError(nameof(model.PromoCode), promoResult.Message);
                        model.PromoMessage = promoResult.Message;
                        model.Items = checkoutItems;
                        model.TotalAmount = subTotal;
                        model.SoProduct = checkoutItems.Count;
                        model.ShippingFee = shippingFee;
                        // TempData[SELECTED_ITEMS_KEY] đã được Peek nên sẽ sống tiếp qua redirect
                        return View(model);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi validate promo code '{Code}'", model.PromoCode);
                }
            }

            // ─── Tạo CartSnapshot (chỉ chứa các item được chọn) ─────
            var snapshot = checkoutItems.Select(i => new CartItemSnapshot
            {
                ProductId = i.ProductId,
                ProductVariantId = i.ProductVariantId,
                ProductName = i.ProductName,
                SkuSnapshot = i.SkuSnapshot,
                SpecSnapshot = i.SpecSnapshot,
                ImageUrl = i.ImageUrl,
                UnitPrice = variants.TryGetValue(i.ProductVariantId, out var v) ? v.Price : i.Price,
                Quantity = i.Quantity
            }).ToList();

            // ─── Tạo PendingCheckout ────────────────────────────────
            var pending = new PendingCheckout
            {
                UserId = user.Id,
                RecipientName = model.RecipientName,
                RecipientPhone = model.SoDienThoai,
                ShippingAddress = model.ShippingAddress,
                Note = model.GhiChu,
                SubTotal = subTotal,
                ShippingFee = shippingFee,
                Discount = discount,
                TotalAmount = subTotal + shippingFee - discount,
                PaymentMethod = model.PaymentMethod,
                CartSnapshot = JsonSerializer.Serialize(snapshot)
            };

            _context.PendingCheckouts.Add(pending);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "PendingCheckout {Id} tạo cho user {UserId}, total={Total}, method={Method}, items={Count}",
                pending.Id, user.Id, pending.TotalAmount, model.PaymentMethod, checkoutItems.Count);

            // Lưu danh sách (ProductId, ProductVariantId) đã checkout vào Session
            // để sau khi callback VNPay/MoMo thành công → chỉ xoá đúng các item này
            HttpContext.Session.SetString(
                $"CheckoutSelected_{pending.Id}",
                JsonSerializer.Serialize(
                    checkoutItems.Select(i => new SelectedKey(i.ProductId, i.ProductVariantId))));

            // ─── VNPay ───────────────────────────────────────────────
            if (model.PaymentMethod == "VNPAY")
            {
                try
                {
                    var paymentModel = new PaymentInformationModel
                    {
                        OrderType = "other",
                        Amount = (double)pending.TotalAmount,
                        OrderDescription = $"HDKTech_{pending.Id}",
                        Name = model.RecipientName
                    };

                    var paymentUrl = _vnPayService.CreatePaymentUrl(paymentModel, HttpContext);
                    HttpContext.Session.SetString("PendingCheckoutId", pending.Id.ToString());

                    return Redirect(paymentUrl);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi tạo URL VNPay cho PendingCheckout {Id}", pending.Id);
                    pending.Status = CheckoutStatus.Failed;
                    await _context.SaveChangesAsync();
                    TempData["Error"] = "Không thể kết nối VNPay. Vui lòng thử lại.";
                    model.Items = checkoutItems;
                    model.TotalAmount = subTotal;
                    model.SoProduct = checkoutItems.Count;
                    return View(model);
                }
            }

            // ─── MoMo ────────────────────────────────────────────────
            if (model.PaymentMethod == "Momo")
            {
                try
                {
                    var orderInfoModel = new OrderInfoModel
                    {
                        FullName = model.RecipientName,
                        Amount = (long)pending.TotalAmount,
                        OrderInfo = "Thanh toan don hang HDKTech",
                        ExtraData = pending.Id.ToString()
                    };

                    var momoResponse = await _momoService.CreatePaymentAsync(orderInfoModel);

                    if (momoResponse == null || string.IsNullOrEmpty(momoResponse.PayUrl))
                    {
                        _logger.LogError("MoMo trả về lỗi: {Msg}", momoResponse?.Message);
                        pending.Status = CheckoutStatus.Failed;
                        await _context.SaveChangesAsync();
                        TempData["Error"] = $"Không thể kết nối MoMo: {momoResponse?.Message ?? "Lỗi không xác định"}";
                        model.Items = checkoutItems;
                        model.TotalAmount = subTotal;
                        model.SoProduct = checkoutItems.Count;
                        return View(model);
                    }

                    pending.GatewayOrderId = momoResponse.OrderId;
                    await _context.SaveChangesAsync();

                    _logger.LogInformation(
                        "Redirect MoMo: PendingCheckout={Id}, GatewayOrderId={GwId}",
                        pending.Id, momoResponse.OrderId);

                    return Redirect(momoResponse.PayUrl);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi tạo URL MoMo cho PendingCheckout {Id}", pending.Id);
                    pending.Status = CheckoutStatus.Failed;
                    await _context.SaveChangesAsync();
                    TempData["Error"] = "Không thể kết nối MoMo. Vui lòng thử lại.";
                    model.Items = checkoutItems;
                    model.TotalAmount = subTotal;
                    model.SoProduct = checkoutItems.Count;
                    return View(model);
                }
            }

            // ─── COD ─────────────────────────────────────────────────
            try
            {
                var order = await _orderRepository.CreateOrderAsync(
                    userId: user.Id,
                    RecipientName: model.RecipientName,
                    soDienThoai: model.SoDienThoai,
                    ShippingAddress: model.ShippingAddress,
                    items: checkoutItems,   // CHỈ những item đã chọn
                    ShippingFee: shippingFee,
                    paymentMethod: "COD",
                    paymentStatus: "Unpaid");

                pending.Status = CheckoutStatus.Paid;
                await _context.SaveChangesAsync();

                user.FullName = model.RecipientName;
                user.PhoneNumber = model.SoDienThoai;
                await _userManager.UpdateAsync(user);

                // CHỈ xoá những item đã checkout, KHÔNG ClearCartAsync
                await RemoveCheckedOutItemsAsync(checkoutItems);

                // Xoá TempData selected (đã xử lý xong)
                TempData.Remove(SELECTED_ITEMS_KEY);

                // Module D: Email xác nhận — fire-and-forget
                var emailAddr = user.Email ?? "";
                var orderSnap = order;
                var svcEmail = _emailService;
                var logger = _logger;
                _ = Task.Run(async () =>
                {
                    try { await svcEmail.SendOrderConfirmationAsync(orderSnap, emailAddr); }
                    catch (Exception ex) { logger.LogError(ex, "[Email] Gửi xác nhận COD thất bại, OrderId={Id}", orderSnap.Id); }
                });

                _logger.LogInformation("Đơn hàng COD #{Code} tạo thành công", order.OrderCode);
                return RedirectToAction("Success", new { maOrder = order.OrderCode });
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("tồn kho") || ex.Message.Contains("hết hàng"))
            {
                pending.Status = CheckoutStatus.Failed;
                await _context.SaveChangesAsync();
                TempData["Error"] = ex.Message;
                model.Items = checkoutItems;
                model.TotalAmount = subTotal;
                model.SoProduct = checkoutItems.Count;
                return View(model);
            }
            catch (Exception ex)
            {
                pending.Status = CheckoutStatus.Failed;
                await _context.SaveChangesAsync();
                _logger.LogError(ex, "Lỗi tạo đơn COD cho PendingCheckout {Id}", pending.Id);
                TempData["Error"] = "Lỗi khi đặt hàng. Vui lòng thử lại.";
                model.Items = checkoutItems;
                model.TotalAmount = subTotal;
                model.SoProduct = checkoutItems.Count;
                return View(model);
            }
        }

        // ── GET /Checkout/Success ────────────────────────────────────
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

        // ── GET /Checkout/PaymentFailed ──────────────────────────────
        [HttpGet]
        public async Task<IActionResult> PaymentFailed(Guid? checkoutId)
        {
            if (checkoutId == null)
            {
                TempData["Error"] = "Thanh toán thất bại. Vui lòng thử lại.";
                return RedirectToAction("Index");
            }

            var pending = await _context.PendingCheckouts
                .FirstOrDefaultAsync(p => p.Id == checkoutId.Value);

            var canRetry = pending != null
                && pending.Status == CheckoutStatus.Failed
                && pending.ExpiresAt > DateTime.UtcNow;

            ViewBag.CanRetry = canRetry;
            ViewBag.CheckoutId = checkoutId;
            return View();
        }

        // ── GET /Checkout/PaymentCallbackVnpay ──────────────────────
        [HttpGet]
        public async Task<IActionResult> PaymentCallbackVnpay()
        {
            var response = _vnPayService.PaymentExecute(Request.Query);

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
                _logger.LogWarning("VNPay callback thất bại: ResponseCode={Code}", response.VnPayResponseCode);

                var pendingIdStr = HttpContext.Session.GetString("PendingCheckoutId");
                if (Guid.TryParse(pendingIdStr, out var pendingId))
                {
                    var p = await _context.PendingCheckouts.FindAsync(pendingId);
                    if (p != null) { p.Status = CheckoutStatus.Failed; await _context.SaveChangesAsync(); }
                    return RedirectToAction("PaymentFailed", new { checkoutId = pendingId });
                }

                TempData["Error"] = $"Thanh toán VNPay thất bại (mã: {response.VnPayResponseCode}).";
                return RedirectToAction("Index");
            }

            // Idempotency check
            var existingTx = await _context.PaymentTransactions
                .FirstOrDefaultAsync(t =>
                    t.GatewayTransactionId == response.TransactionId &&
                    t.Gateway == PaymentGateway.VnPay);

            if (existingTx != null)
            {
                _logger.LogWarning("VNPay duplicate callback TransactionId={Id}", response.TransactionId);
                var existingOrder = await _context.Orders.FindAsync(existingTx.OrderId);
                if (existingOrder != null)
                    return RedirectToAction("Success", new { maOrder = existingOrder.OrderCode });
                return RedirectToAction("Index", "Home");
            }

            var pendingIdFromSession = HttpContext.Session.GetString("PendingCheckoutId");
            if (!Guid.TryParse(pendingIdFromSession, out var pid))
            {
                _logger.LogError("VNPay callback: không tìm thấy PendingCheckoutId trong session");
                TempData["Error"] = "Không tìm thấy thông tin thanh toán. Liên hệ hỗ trợ.";
                return RedirectToAction("Index", "Home");
            }

            var pending = await _context.PendingCheckouts.FindAsync(pid);
            if (pending == null)
            {
                TempData["Error"] = "Không tìm thấy thông tin đơn hàng.";
                return RedirectToAction("Index", "Home");
            }

            try
            {
                var user = await _userManager.GetUserAsync(User);

                var payTx = new PaymentTransaction
                {
                    PendingCheckoutId = pending.Id,
                    GatewayTransactionId = response.TransactionId ?? Guid.NewGuid().ToString(),
                    Gateway = PaymentGateway.VnPay,
                    Amount = pending.TotalAmount,
                    Status = TransactionStatus.Success,
                    RawResponse = System.Text.Json.JsonSerializer.Serialize(Request.Query.ToDictionary(q => q.Key, q => q.Value.ToString())),
                    ProcessedAt = DateTime.UtcNow
                };

                var (success, errMsg, order) = await _orderRepository.CreateFromPendingCheckoutAsync(pending);
                if (!success || order == null)
                {
                    payTx.Status = TransactionStatus.Failed;
                    _context.PaymentTransactions.Add(payTx);
                    await _context.SaveChangesAsync();
                    _logger.LogError("VNPay: tạo order thất bại. {Err}", errMsg);
                    TempData["Error"] = "Thanh toán thành công nhưng tạo đơn hàng thất bại. Liên hệ hỗ trợ.";
                    return RedirectToAction("PaymentFailed", new { checkoutId = pending.Id });
                }

                payTx.OrderId = order.Id;
                _context.PaymentTransactions.Add(payTx);
                await _context.SaveChangesAsync();

                if (user != null)
                {
                    user.FullName = pending.RecipientName;
                    user.PhoneNumber = pending.RecipientPhone;
                    await _userManager.UpdateAsync(user);
                }

                // CHỈ xoá các item đã checkout (Khoa's selectedItems support)
                await RemoveItemsBySessionKeyAsync(pending.Id);

                HttpContext.Session.Remove("PendingCheckoutId");

                // Email fire-and-forget
                var vnpEmail = user?.Email ?? "";
                var vnpOrder = order;
                var vnpSvc = _emailService;
                var vnpLogger = _logger;
                _ = Task.Run(async () =>
                {
                    try { await vnpSvc.SendOrderConfirmationAsync(vnpOrder, vnpEmail); }
                    catch (Exception ex) { vnpLogger.LogError(ex, "[Email] Gửi xác nhận VNPay thất bại, OrderId={Id}", vnpOrder.Id); }
                });

                _logger.LogInformation("Đơn hàng VNPay #{Code} tạo thành công", order.OrderCode);
                return RedirectToAction("Success", new { maOrder = order.OrderCode });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi tạo đơn sau VNPay callback");
                TempData["Error"] = "Thanh toán thành công nhưng tạo đơn thất bại. Liên hệ hỗ trợ.";
                return RedirectToAction("PaymentFailed", new { checkoutId = pending.Id });
            }
        }

        // ── GET /Checkout/PaymentCallBack (MoMo redirect) ───────────
        [HttpGet]
        public async Task<IActionResult> PaymentCallBack()
        {
            if (!_momoService.ValidateSignature(Request.Query))
            {
                _logger.LogWarning("MoMo redirect: chữ ký không hợp lệ");
                TempData["Error"] = "Xác thực thanh toán MoMo thất bại.";
                return RedirectToAction("Index");
            }

            var resultCode = Request.Query["resultCode"].ToString();
            var extraData = Request.Query["extraData"].ToString();
            var momoOrderId = Request.Query["orderId"].ToString();

            if (!Guid.TryParse(extraData, out var pendingId))
            {
                _logger.LogError("MoMo redirect: extraData không phải GUID hợp lệ: {Data}", extraData);
                TempData["Error"] = "Lỗi xác định phiên thanh toán.";
                return RedirectToAction("Index");
            }

            var pending = await _context.PendingCheckouts.FindAsync(pendingId);
            if (pending == null)
            {
                TempData["Error"] = "Không tìm thấy thông tin đơn hàng.";
                return RedirectToAction("Index", "Home");
            }

            if (resultCode != "0")
            {
                _logger.LogWarning("MoMo redirect thất bại: resultCode={Code}, orderId={Id}", resultCode, momoOrderId);
                pending.Status = CheckoutStatus.Failed;
                await _context.SaveChangesAsync();
                return RedirectToAction("PaymentFailed", new { checkoutId = pendingId });
            }

            // IPN đã xử lý trước?
            var reloaded = await _context.PendingCheckouts.FindAsync(pendingId);
            if (reloaded?.Status == CheckoutStatus.Paid)
            {
                var existingTx = await _context.PaymentTransactions
                    .FirstOrDefaultAsync(t => t.PendingCheckoutId == pendingId);
                if (existingTx?.OrderId != null)
                {
                    var existingOrder = await _context.Orders.FindAsync(existingTx.OrderId);
                    if (existingOrder != null)
                    {
                        await RemoveItemsBySessionKeyAsync(pendingId);
                        return RedirectToAction("Success", new { maOrder = existingOrder.OrderCode });
                    }
                }
            }

            try
            {
                var (success, errMsg, order) = await _orderRepository.CreateFromPendingCheckoutAsync(pending);
                if (!success || order == null)
                {
                    _logger.LogError("MoMo redirect: tạo order thất bại. {Err}", errMsg);
                    TempData["Error"] = "Thanh toán thành công nhưng tạo đơn hàng thất bại. Liên hệ hỗ trợ.";
                    return RedirectToAction("PaymentFailed", new { checkoutId = pendingId });
                }

                var payTx = new PaymentTransaction
                {
                    PendingCheckoutId = pendingId,
                    OrderId = order.Id,
                    GatewayTransactionId = momoOrderId,
                    Gateway = PaymentGateway.Momo,
                    Amount = pending.TotalAmount,
                    Status = TransactionStatus.Success,
                    RawResponse = JsonSerializer.Serialize(Request.Query.ToDictionary(q => q.Key, q => q.Value.ToString())),
                    ProcessedAt = DateTime.UtcNow
                };

                try
                {
                    _context.PaymentTransactions.Add(payTx);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    _logger.LogInformation("MoMo redirect: transaction đã được IPN insert trước.");
                }

                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    user.FullName = pending.RecipientName;
                    user.PhoneNumber = pending.RecipientPhone;
                    await _userManager.UpdateAsync(user);
                }

                // CHỈ xoá các item đã checkout
                await RemoveItemsBySessionKeyAsync(pendingId);

                // Email
                var momoEmail = user?.Email ?? "";
                var momoOrder = order;
                var momoSvc = _emailService;
                var momoLogger = _logger;
                _ = Task.Run(async () =>
                {
                    try { await momoSvc.SendOrderConfirmationAsync(momoOrder, momoEmail); }
                    catch (Exception ex) { momoLogger.LogError(ex, "[Email] Gửi xác nhận MoMo thất bại, OrderId={Id}", momoOrder.Id); }
                });

                _logger.LogInformation("Đơn hàng MoMo #{Code} tạo thành công", order.OrderCode);
                return RedirectToAction("Success", new { maOrder = order.OrderCode });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi tạo đơn sau MoMo redirect");
                TempData["Error"] = "Thanh toán thành công nhưng tạo đơn thất bại. Liên hệ hỗ trợ.";
                return RedirectToAction("PaymentFailed", new { checkoutId = pendingId });
            }
        }

        // ── POST /Checkout/MomoNotify (IPN server-to-server) ────────
        [HttpPost]
        [AllowAnonymous]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> MomoNotify()
        {
            string body;
            using (var reader = new System.IO.StreamReader(Request.Body))
                body = await reader.ReadToEndAsync();

            _logger.LogInformation("MoMo IPN received: {Body}", body);

            MomoIpnPayload? payload;
            try { payload = JsonSerializer.Deserialize<MomoIpnPayload>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }); }
            catch { return Ok(new { message = "Bad request", resultCode = 1 }); }

            if (payload == null)
                return Ok(new { message = "Empty payload", resultCode = 1 });

            if (!Guid.TryParse(payload.ExtraData, out var pendingId))
            {
                _logger.LogWarning("MoMo IPN: extraData không hợp lệ: {Data}", payload.ExtraData);
                return Ok(new { message = "OK", resultCode = 0 });
            }

            var existing = await _context.PaymentTransactions
                .FirstOrDefaultAsync(t =>
                    t.GatewayTransactionId == payload.OrderId &&
                    t.Gateway == PaymentGateway.Momo);

            if (existing != null)
            {
                _logger.LogInformation("MoMo IPN duplicate: OrderId={Id}", payload.OrderId);
                return Ok(new { message = "Already processed", resultCode = 0 });
            }

            var pending = await _context.PendingCheckouts.FindAsync(pendingId);
            if (pending == null)
            {
                _logger.LogWarning("MoMo IPN: PendingCheckout {Id} không tồn tại", pendingId);
                return Ok(new { message = "OK", resultCode = 0 });
            }

            bool isSuccess = payload.ResultCode == 0;

            var payTx = new PaymentTransaction
            {
                PendingCheckoutId = pendingId,
                GatewayTransactionId = payload.OrderId,
                Gateway = PaymentGateway.Momo,
                Amount = payload.Amount / 1m,
                Status = isSuccess ? TransactionStatus.Success : TransactionStatus.Failed,
                RawResponse = body,
                ProcessedAt = DateTime.UtcNow
            };

            if (isSuccess)
            {
                var (success, errMsg, order) = await _orderRepository.CreateFromPendingCheckoutAsync(pending);
                if (success && order != null)
                {
                    payTx.OrderId = order.Id;
                    _logger.LogInformation("MoMo IPN: Order #{Code} tạo thành công", order.OrderCode);
                }
                else
                {
                    payTx.Status = TransactionStatus.Failed;
                    _logger.LogError("MoMo IPN: tạo order thất bại. Err={Err}", errMsg);
                }
            }
            else
            {
                pending.Status = CheckoutStatus.Failed;
                _logger.LogWarning("MoMo IPN: payment thất bại. ResultCode={Code}", payload.ResultCode);
            }

            try
            {
                _context.PaymentTransactions.Add(payTx);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                _logger.LogInformation("MoMo IPN: transaction đã được redirect insert trước.");
            }

            return Ok(new { message = "Success", resultCode = 0 });
        }

        // ════════════════════════════════════════════════════════════
        //  HELPER: Xoá các item cụ thể ra khỏi cart sau khi đặt hàng
        // ════════════════════════════════════════════════════════════
        private async Task RemoveCheckedOutItemsAsync(List<CartItem> checkedOutItems)
        {
            // Nếu cart chỉ còn đúng các item đã checkout → ClearCartAsync cho nhanh
            var cart = await _cartService.GetCartAsync();
            if (cart == null) return;

            if (cart.Items.Count == checkedOutItems.Count &&
                cart.Items.All(ci => checkedOutItems.Any(co =>
                    co.ProductId == ci.ProductId &&
                    co.ProductVariantId == ci.ProductVariantId)))
            {
                await _cartService.ClearCartAsync();
                return;
            }

            foreach (var item in checkedOutItems)
            {
                try
                {
                    await _cartService.RemoveItemAsync(item.ProductId, item.ProductVariantId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Không thể xoá item (P={Pid}, V={Vid}) khỏi cart sau checkout",
                        item.ProductId, item.ProductVariantId);
                }
            }
        }

        /// <summary>
        /// Đọc danh sách (ProductId, ProductVariantId) đã lưu vào Session lúc POST,
        /// dùng sau callback VNPay/MoMo thành công để xoá đúng các item đã checkout.
        /// Nếu Session không còn → fallback ClearCartAsync (an toàn cũ).
        /// </summary>
        private async Task RemoveItemsBySessionKeyAsync(Guid pendingId)
        {
            var key = $"CheckoutSelected_{pendingId}";
            var json = HttpContext.Session.GetString(key);
            HttpContext.Session.Remove(key);

            if (string.IsNullOrWhiteSpace(json))
            {
                // Fallback — không biết user đã chọn gì, clear toàn bộ cart cho an toàn
                await _cartService.ClearCartAsync();
                return;
            }

            List<SelectedKey>? selected;
            try
            {
                selected = JsonSerializer.Deserialize<List<SelectedKey>>(json);
            }
            catch
            {
                await _cartService.ClearCartAsync();
                return;
            }

            if (selected == null || selected.Count == 0)
            {
                await _cartService.ClearCartAsync();
                return;
            }

            var cart = await _cartService.GetCartAsync();
            if (cart == null) return;

            // Nếu toàn bộ cart khớp → ClearCartAsync
            if (cart.Items.Count == selected.Count &&
                cart.Items.All(ci => selected.Any(s =>
                    s.ProductId == ci.ProductId &&
                    s.ProductVariantId == ci.ProductVariantId)))
            {
                await _cartService.ClearCartAsync();
                return;
            }

            foreach (var s in selected)
            {
                try
                {
                    await _cartService.RemoveItemAsync(s.ProductId, s.ProductVariantId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Không thể xoá item (P={Pid}, V={Vid}) khỏi cart sau callback",
                        s.ProductId, s.ProductVariantId);
                }
            }
        }

        // ── Helpers ──────────────────────────────────────────────────

        /// <summary>
        /// Tính phí ship server-side — miễn ship nếu đơn >= 2 triệu, ngược lại 30k.
        /// </summary>
        private static decimal CalculateShippingFee(decimal subTotal)
            => subTotal >= 2_000_000 ? 0 : 30_000;

        /// <summary>
        /// Sanitize text input: strip control characters, trim, truncate.
        /// </summary>
        private static string SanitizeText(string? input, int maxLength)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            var cleaned = Regex.Replace(input, @"[\p{Cc}&&[^\n\t]]", string.Empty);
            cleaned = cleaned.Trim();
            return cleaned.Length > maxLength ? cleaned[..maxLength] : cleaned;
        }
    }

    // ── DTO cho MoMo IPN payload ─────────────────────────────────────
    public class MomoIpnPayload
    {
        public string PartnerCode { get; set; } = string.Empty;
        public string OrderId { get; set; } = string.Empty;
        public string RequestId { get; set; } = string.Empty;
        public long Amount { get; set; }
        public string OrderInfo { get; set; } = string.Empty;
        public string OrderType { get; set; } = string.Empty;
        public string TransId { get; set; } = string.Empty;
        public int ResultCode { get; set; }
        public string Message { get; set; } = string.Empty;
        public string PayType { get; set; } = string.Empty;
        public long ResponseTime { get; set; }
        public string ExtraData { get; set; } = string.Empty;
        public string Signature { get; set; } = string.Empty;
    }
}
