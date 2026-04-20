// ✅ Fix #3: ViewComponent "CartSummary" để hiển thị số lượng sản phẩm trong giỏ hàng
// Lấy dữ liệu từ Session thông qua ICartService (đọc server-side)
// Gọi vào _Layout.cshtml bằng: @await Component.InvokeAsync("CartSummary")
// Số lượng sẽ tự động đúng mỗi lần render (realtime khi page load)

using HDKTech.Services;
using Microsoft.AspNetCore.Mvc;

namespace HDKTech.Component
{
    /// <summary>
    /// ViewComponent hiển thị tóm tắt giỏ hàng trên Header
    /// Render số lượng sản phẩm từ Session (server-side) để luôn chính xác
    /// </summary>
    public class CartSummaryViewComponent : ViewComponent
    {
        private readonly ICartService _cartService;
        private readonly ILogger<CartSummaryViewComponent> _logger;

        public CartSummaryViewComponent(ICartService cartService, ILogger<CartSummaryViewComponent> logger)
        {
            _cartService = cartService;
            _logger      = logger;
        }

        /// <summary>
        /// Lấy tổng số sản phẩm trong giỏ hàng từ Session và truyền vào View
        /// </summary>
        public async Task<IViewComponentResult> InvokeAsync()
        {
            try
            {
                var cart     = await _cartService.GetCartAsync();
                int soLuong  = cart?.TotalItems ?? 0;
                return View(soLuong);
            }
            catch (Exception ex)
            {
                // Không throw - trả về 0 để header không crash khi session lỗi
                _logger.LogWarning(ex, "CartSummaryViewComponent: không lấy được giỏ hàng");
                return View(0);
            }
        }
    }
}
