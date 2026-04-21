// Services/IEmailService.cs — Module D

// Services/IEmailService.cs — Module D
using HDKTech.Models;

namespace HDKTech.Services.Interfaces
{
    /// <summary>
    /// Gửi email giao dịch. Implementation: SmtpEmailService (System.Net.Mail).
    /// Tất cả method đều nên được gọi fire-and-forget để không block HTTP response.
    /// </summary>
    public interface IEmailService
    {
        /// <summary>Gửi email xác nhận đơn hàng sau khi đặt thành công.</summary>
        Task SendOrderConfirmationAsync(Order order, string toEmail);
    }
}
