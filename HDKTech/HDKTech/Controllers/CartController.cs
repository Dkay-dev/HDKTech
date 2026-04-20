using HDKTech.Models;
using HDKTech.Repositories;
using HDKTech.Services;
using HDKTech.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace HDKTech.Controllers
{
    public class CartController : Controller
    {
        private readonly ICartService _cartService;
        private readonly ProductRepository _productRepo;
        private readonly ILogger<CartController> _logger;

        public CartController(
            ICartService cartService,
            ProductRepository productRepo,
            ILogger<CartController> logger)
        {
            _cartService = cartService;
            _productRepo = productRepo;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var cart = await _cartService.GetCartAsync();
            return View(cart);
        }

        // ─────────────────────────────────────────────────────────────
        // ADD (GET) — id = productId, variantId optional (dùng default)
        // ─────────────────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Add(int id, int? variantId = null, int quantity = 1)
        {
            if (id <= 0 || quantity <= 0)
                return BadRequest("Sản phẩm không hợp lệ");

            var product = await _productRepo.GetProductWithDetailsAsync(id);
            if (product == null) return NotFound("Sản phẩm không tồn tại");

            // Pick variant — user chọn hoặc fallback sang default
            var variant = PickVariant(product, variantId);
            if (variant == null)
            {
                TempData["Error"] = "Sản phẩm chưa có cấu hình khả dụng.";
                return RedirectToAction("Details", "Product", new { id });
            }

            var rawImageUrl = product.Images?.FirstOrDefault(h => h.IsDefault)?.ImageUrl
                              ?? product.Images?.FirstOrDefault()?.ImageUrl;
            var fullImageUrl = ImageHelper.GetImagePath(rawImageUrl, product.Category?.Name);

            var cartItem = new CartItem(
                productId:        product.Id,
                productVariantId: variant.Id,
                productName:      product.Name,
                price:            variant.Price,
                quantity:         quantity,
                skuSnapshot:      variant.Sku,
                specSnapshot:     BuildSpecSnapshot(variant),
                ImageUrl:         fullImageUrl,
                categoryName:     product.Category?.Name);

            try
            {
                await _cartService.AddItemAsync(cartItem);
                _logger.LogInformation("Thêm {Sku} vào giỏ", variant.Sku);
            }
            catch (InvalidOperationException ex)
            {
                // DbCartService throw khi không đủ tồn kho
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction("Index");
        }

        // ─────────────────────────────────────────────────────────────
        // ADD (POST / AJAX)
        // ─────────────────────────────────────────────────────────────
        [HttpPost]
        [EnableRateLimiting("add-to-cart")]
        public async Task<IActionResult> AddToCart([FromBody] AddToCartRequest request)
        {
            if (request == null || request.ProductId <= 0 || request.Quantity <= 0)
                return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ" });

            try
            {
                var product = await _productRepo.GetProductWithDetailsAsync(request.ProductId);
                if (product == null)
                    return NotFound(new { success = false, message = "Sản phẩm không tồn tại" });

                var variant = PickVariant(product, request.ProductVariantId);
                if (variant == null)
                    return BadRequest(new { success = false, message = "Sản phẩm chưa có cấu hình khả dụng." });

                var rawImageUrl = product.Images?.FirstOrDefault(h => h.IsDefault)?.ImageUrl
                                  ?? product.Images?.FirstOrDefault()?.ImageUrl;
                var fullImageUrl = ImageHelper.GetImagePath(rawImageUrl, product.Category?.Name);

                var cartItem = new CartItem(
                    productId:        product.Id,
                    productVariantId: variant.Id,
                    productName:      product.Name,
                    price:            variant.Price,
                    quantity:         request.Quantity,
                    skuSnapshot:      variant.Sku,
                    specSnapshot:     BuildSpecSnapshot(variant),
                    ImageUrl:         fullImageUrl,
                    categoryName:     product.Category?.Name);

                await _cartService.AddItemAsync(cartItem);
                var cart = await _cartService.GetCartAsync();

                return Ok(new
                {
                    success     = true,
                    message     = $"Đã thêm {product.Name} vào giỏ",
                    totalItems  = cart.TotalItems,
                    totalPrice  = cart.TotalPrice.ToString("C")
                });
            }
            catch (InvalidOperationException ex)
            {
                // DbCartService throw khi không đủ tồn kho
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi thêm sản phẩm vào giỏ");
                return StatusCode(500, new { success = false, message = "Lỗi server" });
            }
        }

        // ─────────────────────────────────────────────────────────────
        // REMOVE (POST) — cần ProductId + ProductVariantId
        // ─────────────────────────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> Remove(
            [FromBody] RemoveItemRequest? request = null,
            [FromForm] int productId = 0,
            [FromForm] int productVariantId = 0)
        {
            try
            {
                var pid = request?.ProductId        ?? productId;
                var vid = request?.ProductVariantId ?? productVariantId;

                if (pid <= 0 || vid <= 0)
                    return BadRequest(new { success = false, message = "ID không hợp lệ" });

                await _cartService.RemoveItemAsync(pid, vid);
                var cart = await _cartService.GetCartAsync();

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest" || request != null)
                    return Ok(new
                    {
                        success    = true,
                        message    = "Xoá sản phẩm thành công",
                        totalItems = cart.TotalItems,
                        totalPrice = cart.TotalPrice
                    });

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi xoá sản phẩm");
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest" || request != null)
                    return StatusCode(500, new { success = false, message = "Lỗi server" });
                return RedirectToAction("Index");
            }
        }

        // ─────────────────────────────────────────────────────────────
        // UPDATE QUANTITY (AJAX)
        // ─────────────────────────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> UpdateQuantity([FromBody] UpdateQuantityRequest request)
        {
            if (request == null || request.ProductId <= 0 || request.ProductVariantId <= 0 || request.Quantity <= 0)
                return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ" });

            try
            {
                await _cartService.UpdateQuantityAsync(request.ProductId, request.ProductVariantId, request.Quantity);
                var cart = await _cartService.GetCartAsync();

                return Ok(new
                {
                    success    = true,
                    totalItems = cart.TotalItems,
                    totalPrice = cart.TotalPrice,
                    itemTotal  = cart.Items.FirstOrDefault(x =>
                                    x.ProductId == request.ProductId &&
                                    x.ProductVariantId == request.ProductVariantId)?.TotalPrice ?? 0
                });
            }
            catch (InvalidOperationException ex)
            {
                // DbCartService throw khi không đủ tồn kho
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi cập nhật số lượng");
                return StatusCode(500, new { success = false, message = "Lỗi server" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Clear()
        {
            await _cartService.ClearCartAsync();
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> GetCartInfo()
        {
            var cart = await _cartService.GetCartAsync();
            return Ok(new
            {
                totalItems = cart.TotalItems,
                totalPrice = cart.TotalPrice.ToString("C")
            });
        }

        // ─────────────────────────────────────────────────────────────
        // Helpers
        // ─────────────────────────────────────────────────────────────
        private static ProductVariant? PickVariant(Product product, int? variantId)
        {
            if (product.Variants == null || !product.Variants.Any()) return null;

            if (variantId.HasValue && variantId > 0)
            {
                var picked = product.Variants.FirstOrDefault(v => v.Id == variantId.Value && v.IsActive);
                if (picked != null) return picked;
            }

            return product.DefaultVariant
                ?? product.Variants.FirstOrDefault(v => v.IsActive);
        }

        private static string BuildSpecSnapshot(ProductVariant v)
        {
            var parts = new[] { v.Cpu, v.Ram, v.Storage, v.Gpu }
                .Where(s => !string.IsNullOrWhiteSpace(s));
            return string.Join(" / ", parts);
        }
    }

    public class AddToCartRequest
    {
        public int ProductId        { get; set; }
        public int ProductVariantId { get; set; }
        public int Quantity         { get; set; } = 1;
    }

    public class UpdateQuantityRequest
    {
        public int ProductId        { get; set; }
        public int ProductVariantId { get; set; }
        public int Quantity         { get; set; }
    }

    public class RemoveItemRequest
    {
        public int ProductId        { get; set; }
        public int ProductVariantId { get; set; }
    }
}
