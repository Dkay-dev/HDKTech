using HDKTech.Models.Momo;
using HDKTech.Models.Vnpay;
using HDKTech.Services.Momo;
using HDKTech.Services.Vnpay;
using Microsoft.AspNetCore.Mvc;

namespace HDKTech.Controllers
{
    /// <summary>
    /// PaymentController — dùng làm API endpoint nếu cần gọi trực tiếp.
    /// Luồng chính của ứng dụng đã được xử lý tại CheckoutController.
    /// </summary>
    public class PaymentController : Controller
    {
        private readonly IVnPayService _vnPayService;
        private readonly IMomoService _momoService;

        public PaymentController(IVnPayService vnPayService, IMomoService momoService)
        {
            _vnPayService = vnPayService;
            _momoService = momoService;
        }

        // ✅ Không cần dùng nữa — luồng MoMo đã chuyển sang CheckoutController.Index (POST)
        // Giữ lại phòng trường hợp gọi trực tiếp từ ngoài
        [HttpPost]
        [Route("Payment/CreatePaymentUrl")]
        public async Task<IActionResult> CreatePaymentUrl([FromBody] OrderInfoModel model)
        {
            if (model == null || model.Amount <= 0)
                return BadRequest("Thông tin đơn hàng không hợp lệ.");

            var response = await _momoService.CreatePaymentAsync(model);

            if (response == null || string.IsNullOrEmpty(response.PayUrl))
                return BadRequest($"Không thể tạo URL MoMo: {response?.Message}");

            return Ok(new { payUrl = response.PayUrl });
        }

        // ✅ Không cần dùng nữa — luồng VNPay đã chuyển sang CheckoutController.Index (POST)
        [HttpPost]
        [Route("Payment/CreatePaymentUrlVnpay")]
        public IActionResult CreatePaymentUrlVnpay(PaymentInformationModel model)
        {
            var url = _vnPayService.CreatePaymentUrl(model, HttpContext);
            return Redirect(url);
        }
    }
}