using HDKTech.Models;
using HDKTech.Repositories;
using HDKTech.Repositories.Interfaces;
using HDKTech.Services;
using HDKTech.Utils;
using Microsoft.AspNetCore.Mvc;

using HDKTech.Areas.Admin.Repositories;

namespace HDKTech.Controllers
{
    public class CartController : Controller
    {
        private readonly ICartService _cartService;
        private readonly ProductRepository _productRepo;
        private readonly ILogger<CartController> _logger;

        public CartController(ICartService cartService, ProductRepository productRepo, ILogger<CartController> logger)
        {
            _cartService = cartService;
            _productRepo = productRepo;
            _logger = logger;
        }

        /// <summary>
        /// Hiển thị giỏ hàng
        /// </summary>
        public async Task<IActionResult> Index()
        {
            var cart = await _cartService.GetCartAsync();
            return View(cart);
        }

        /// <summary>
        /// Thêm sản phẩm vào giỏ (GET - từ link)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Add(int id, int quantity = 1)
        {
            if (id <= 0 || quantity <= 0)
                return BadRequest("Sản phẩm không hợp lệ");

            // Lấy sản phẩm từ database (với Category)
            var product = await _productRepo.GetProductWithDetailsAsync(id);
            if (product == null)
                return NotFound("Sản phẩm không tồn tại");

            // Lấy ảnh đầu tiên (nếu có)
            var rawImageUrl = product.Images?.FirstOrDefault(h => h.IsDefault)?.ImageUrl 
                           ?? product.Images?.FirstOrDefault()?.ImageUrl;

            // Chuẩn hoá đường dẫn ảnh bằng shared helper
            var fullImageUrl = ImageHelper.GetImagePath(rawImageUrl, product.Category?.Name);

            // Tạo CartItem từ Product với đường dẫn ảnh đầy đủ
            var cartItem = new CartItem(
                productId: product.Id,
                productName: product.Name,
                price: product.Price,
                quantity: quantity,
                ImageUrl: fullImageUrl,
                categoryName: product.Category?.Name
            );

            // Thêm vào giỏ
            await _cartService.AddItemAsync(cartItem);

            _logger.LogInformation($"Thêm sản phẩm {product.Name} vào giỏ");

            // Redirect về trang giỏ hàng
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Thêm sản phẩm vào giỏ (POST - AJAX)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> AddToCart([FromBody] AddToCartRequest request)
        {
            if (request?.ProductId <= 0 || request.Quantity <= 0)
                return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ" });

            try
            {
                var product = await _productRepo.GetProductWithDetailsAsync(request.ProductId);
                if (product == null)
                    return NotFound(new { success = false, message = "Sản phẩm không tồn tại" });

                var rawImageUrl = product.Images?.FirstOrDefault(h => h.IsDefault)?.ImageUrl 
                               ?? product.Images?.FirstOrDefault()?.ImageUrl;

                // Chuẩn hoá đường dẫn ảnh bằng shared helper
                var fullImageUrl = ImageHelper.GetImagePath(rawImageUrl, product.Category?.Name);

                var cartItem = new CartItem(
                    productId: product.Id,
                    productName: product.Name,
                    price: product.Price,
                    quantity: request.Quantity,
                    ImageUrl: fullImageUrl,
                    categoryName: product.Category?.Name
                );

                await _cartService.AddItemAsync(cartItem);

                var cart = await _cartService.GetCartAsync();

                return Ok(new
                {
                    success = true,
                    message = $"Đã thêm {product.Name} vào giỏ",
                    totalItems = cart.TotalItems,
                    totalPrice = cart.TotalPrice.ToString("C")
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi thêm sản phẩm vào giỏ");
                return StatusCode(500, new { success = false, message = "Lỗi server" });
            }
        }

        /// <summary>
        /// Xóa sản phẩm khỏi giỏ (hỗ trợ AJAX và Form)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Remove([FromBody] RemoveItemRequest? request = null, [FromForm] int productId = 0)
        {
            try
            {
                // Support both AJAX (FromBody) and Form submission
                var idToRemove = request?.ProductId ?? productId;

                if (idToRemove <= 0)
                    return BadRequest(new { success = false, message = "ID sản phẩm không hợp lệ" });

                await _cartService.RemoveItemAsync(idToRemove);
                var cart = await _cartService.GetCartAsync();

                // Check if request is AJAX
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest" || request != null)
                {
                    return Ok(new
                    {
                        success = true,
                        message = "Xoá sản phẩm thành công",
                        totalItems = cart.TotalItems,
                        totalPrice = cart.TotalPrice
                    });
                }

                // Otherwise redirect (for form submission)
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi xoá sản phẩm");
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest" || request != null)
                {
                    return StatusCode(500, new { success = false, message = "Lỗi server" });
                }
                return RedirectToAction("Index");
            }
        }

        /// <summary>
        /// Cập nhật số lượng sản phẩm (AJAX)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> UpdateQuantity([FromBody] UpdateQuantityRequest request)
        {
            if (request?.ProductId <= 0 || request.Quantity <= 0)
                return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ" });

            try
            {
                await _cartService.UpdateQuantityAsync(request.ProductId, request.Quantity);
                var cart = await _cartService.GetCartAsync();
                var item = cart.Items.FirstOrDefault(x => x.ProductId == request.ProductId);

                return Ok(new
                {
                    success = true,
                    totalItems = cart.TotalItems,
                    totalPrice = cart.TotalPrice,
                    itemTotal = item?.TotalPrice ?? 0,
                    unitPrice = item?.Price ?? 0
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi cập nhật số lượng");
                return StatusCode(500, new { success = false, message = "Lỗi server" });
            }
        }

        /// <summary>
        /// Xóa toàn bộ giỏ hàng
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Clear()
        {
            await _cartService.ClearCartAsync();
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Lấy giỏ hàng (AJAX - để hiển thị số lượng trên navbar)
        /// </summary>
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
    }

    /// <summary>
    /// Request model để thêm sản phẩm vào giỏ (AJAX)
    /// </summary>
    public class AddToCartRequest
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; } = 1;
    }

    /// <summary>
    /// Request model để cập nhật số lượng (AJAX)
    /// </summary>
    public class UpdateQuantityRequest
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }

    /// <summary>
    /// Request model để xoá sản phẩm (AJAX)
    /// </summary>
    public class RemoveItemRequest
    {
        public int ProductId { get; set; }
    }
}


