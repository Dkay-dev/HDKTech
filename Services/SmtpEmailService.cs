// Services/SmtpEmailService.cs — Module D
using HDKTech.Models;
using System.Net;
using System.Net.Mail;

namespace HDKTech.Services
{
    /// <summary>
    /// SMTP email service dùng System.Net.Mail.SmtpClient.
    /// Config đọc từ appsettings.json section "Email":
    ///   SmtpHost, SmtpPort, SmtpUser, SmtpPass, FromEmail, FromName.
    ///
    /// Khi SmtpHost không được cấu hình (placeholder), service log warning
    /// và skip gửi thay vì throw exception (graceful degradation).
    /// </summary>
    public class SmtpEmailService : IEmailService
    {
        private readonly IConfiguration         _config;
        private readonly ILogger<SmtpEmailService> _logger;

        public SmtpEmailService(
            IConfiguration            config,
            ILogger<SmtpEmailService> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task SendOrderConfirmationAsync(Order order, string toEmail)
        {
            if (string.IsNullOrWhiteSpace(toEmail))
            {
                _logger.LogWarning("[Email] Không gửi được — toEmail trống. OrderId={OrderId}", order.Id);
                return;
            }

            var section  = _config.GetSection("Email");
            var smtpHost = section["SmtpHost"] ?? "";
            var smtpPort = int.TryParse(section["SmtpPort"], out var p) ? p : 587;
            var smtpUser = section["SmtpUser"] ?? "";
            var smtpPass = section["SmtpPass"] ?? "";
            var fromEmail = section["FromEmail"] ?? smtpUser;
            var fromName  = section["FromName"]  ?? "HDKTech Shop";

            // Graceful degradation: nếu SMTP chưa cấu hình, log và bỏ qua
            if (string.IsNullOrWhiteSpace(smtpHost) || smtpHost == "smtp.example.com")
            {
                _logger.LogWarning(
                    "[Email] SMTP chưa cấu hình — bỏ qua gửi xác nhận đơn #{Code}",
                    order.OrderCode);
                return;
            }

            try
            {
                var subject = $"[HDKTech] Xác nhận đơn hàng #{order.OrderCode}";
                var body    = BuildOrderConfirmationHtml(order);

                using var client = new SmtpClient(smtpHost, smtpPort)
                {
                    EnableSsl   = true,
                    Credentials = new NetworkCredential(smtpUser, smtpPass),
                    Timeout     = 10_000 // 10 giây
                };

                using var message = new MailMessage
                {
                    From       = new MailAddress(fromEmail, fromName),
                    Subject    = subject,
                    Body       = body,
                    IsBodyHtml = true
                };
                message.To.Add(toEmail);

                await client.SendMailAsync(message);

                _logger.LogInformation(
                    "[Email] Đã gửi xác nhận đơn #{Code} → {Email}",
                    order.OrderCode, MaskEmail(toEmail));
            }
            catch (SmtpException ex)
            {
                _logger.LogError(ex,
                    "[Email] SMTP lỗi khi gửi xác nhận đơn #{Code} → {Email}",
                    order.OrderCode, MaskEmail(toEmail));
                // Không re-throw — email lỗi không nên block luồng chính
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[Email] Lỗi không xác định khi gửi xác nhận đơn #{Code}",
                    order.OrderCode);
            }
        }

        // ── HTML template ──────────────────────────────────────────────────
        private static string BuildOrderConfirmationHtml(Order order)
        {
            var items = order.Items != null
                ? string.Join("", order.Items.Select(i => $@"
                <tr>
                    <td style='padding:8px;border-bottom:1px solid #eee;'>{(string.IsNullOrWhiteSpace(i.ProductNameSnapshot) ? $"SP#{i.ProductId}" : i.ProductNameSnapshot)}</td>
                    <td style='padding:8px;border-bottom:1px solid #eee;text-align:center;'>{i.Quantity}</td>
                    <td style='padding:8px;border-bottom:1px solid #eee;text-align:right;'>{i.UnitPrice:N0}đ</td>
                    <td style='padding:8px;border-bottom:1px solid #eee;text-align:right;'>{i.UnitPrice * i.Quantity:N0}đ</td>
                </tr>"))
                : "<tr><td colspan='4' style='padding:8px;text-align:center;color:#999;'>Không có thông tin sản phẩm</td></tr>";

            return $@"
<!DOCTYPE html>
<html lang='vi'>
<head><meta charset='UTF-8'><title>Xác nhận đơn hàng</title></head>
<body style='font-family:Arial,sans-serif;color:#333;max-width:600px;margin:auto;'>

  <div style='background:#1a73e8;padding:24px;text-align:center;'>
    <h1 style='color:#fff;margin:0;font-size:24px;'>HDKTech</h1>
    <p style='color:#dce8fd;margin:4px 0 0;'>Đặt hàng thành công!</p>
  </div>

  <div style='padding:24px;'>
    <p>Xin chào <strong>{order.RecipientName ?? "Quý khách"}</strong>,</p>
    <p>Đơn hàng <strong>#{order.OrderCode}</strong> của bạn đã được đặt thành công vào lúc
       <strong>{order.OrderDate:dd/MM/yyyy HH:mm}</strong>.</p>

    <h3 style='border-bottom:2px solid #1a73e8;padding-bottom:8px;'>Thông tin giao hàng</h3>
    <p>
      Người nhận: <strong>{order.RecipientName}</strong><br/>
      Điện thoại: <strong>{order.RecipientPhone}</strong><br/>
      Địa chỉ: <strong>{order.ShippingAddressFull ?? order.ShippingAddressLine}</strong><br/>
      Phương thức thanh toán: <strong>{order.PaymentMethod}</strong>
    </p>

    <h3 style='border-bottom:2px solid #1a73e8;padding-bottom:8px;'>Chi tiết đơn hàng</h3>
    <table style='width:100%;border-collapse:collapse;font-size:14px;'>
      <thead>
        <tr style='background:#f5f5f5;'>
          <th style='padding:8px;text-align:left;'>Sản phẩm</th>
          <th style='padding:8px;text-align:center;'>SL</th>
          <th style='padding:8px;text-align:right;'>Đơn giá</th>
          <th style='padding:8px;text-align:right;'>Thành tiền</th>
        </tr>
      </thead>
      <tbody>
        {items}
      </tbody>
      <tfoot>
        <tr>
          <td colspan='3' style='padding:8px;text-align:right;'>Phí vận chuyển:</td>
          <td style='padding:8px;text-align:right;'>{order.ShippingFee:N0}đ</td>
        </tr>
        {(order.DiscountAmount > 0 ? $@"
        <tr>
          <td colspan='3' style='padding:8px;text-align:right;color:#e53935;'>Giảm giá:</td>
          <td style='padding:8px;text-align:right;color:#e53935;'>-{order.DiscountAmount:N0}đ</td>
        </tr>" : "")}
        <tr style='background:#e8f0fe;font-weight:bold;'>
          <td colspan='3' style='padding:10px;text-align:right;'>TỔNG CỘNG:</td>
          <td style='padding:10px;text-align:right;color:#1a73e8;font-size:16px;'>{order.TotalAmount:N0}đ</td>
        </tr>
      </tfoot>
    </table>

    <p style='margin-top:24px;color:#666;font-size:13px;'>
      Chúng tôi sẽ liên hệ xác nhận và giao hàng trong vòng 1–3 ngày làm việc.<br/>
      Mọi thắc mắc vui lòng liên hệ hotline hoặc trả lời email này.
    </p>
  </div>

  <div style='background:#f5f5f5;padding:16px;text-align:center;font-size:12px;color:#999;'>
    HDKTech — Chuyên laptop chính hãng<br/>
    Email này được gửi tự động, vui lòng không reply trực tiếp.
  </div>

</body>
</html>";
        }

        /// <summary>Che bớt địa chỉ email trong log để tránh PII leak.</summary>
        private static string MaskEmail(string email)
        {
            var at = email.IndexOf('@');
            if (at <= 1) return "***";
            return email[0] + "***" + email[at..];
        }
    }
}
