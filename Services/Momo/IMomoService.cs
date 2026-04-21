using HDKTech.Models.Momo;

namespace HDKTech.Services.Momo
{
    public interface IMomoService
    {
        Task<MomoCreatePaymentResponseModel> CreatePaymentAsync(OrderInfoModel model);
        MomoExecuteResponseModel PaymentExecute(IQueryCollection query);
        /// <summary>
        /// Xác thực chữ ký từ MoMo callback. Nhận thẳng IQueryCollection để lấy đủ các tham số.
        /// </summary>
        bool ValidateSignature(IQueryCollection query);  // ✅ THAY ĐỔI signature
    }
}