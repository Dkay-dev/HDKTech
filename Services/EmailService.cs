// Services/EmailService.cs
// Thay thế/bổ sung SmtpEmailService — dùng MailKit thay System.Net.Mail
// để hỗ trợ STARTTLS/SSL tốt hơn và email xác thực OTP
using HDKTech.Models;
using HDKTech.Services.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace HDKTech.Services
{
    /// <summary>
    /// IEmailSender - Interface cho việc gửi email (dùng cho Identity xác thực)
    /// </summary>
    public interface IEmailSender
    {
        Task SendEmailAsync(string email, string subject, string htmlMessage);
    }

    /// <summary>
    /// MailKitEmailSender — Gửi email dùng MailKit (hỗ trợ STARTTLS/SSL đầy đủ).
    /// Dùng cho:
    ///   - Xác thực email khi đăng ký (OTP link)
    ///   - Đặt lại mật khẩu
    ///   - Confirm email Google login lần đầu
    ///
    /// Config trong appsettings.json section "Email":
    ///   SmtpHost, SmtpPort, SmtpUser, SmtpPass, FromEmail, FromName, UseSsl
    /// </summary>
    public class MailKitEmailSender : IEmailSender
    {
        private readonly IConfiguration _config;
        private readonly ILogger<MailKitEmailSender> _logger;

        public MailKitEmailSender(
            IConfiguration config,
            ILogger<MailKitEmailSender> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            var section = _config.GetSection("Email");
            var smtpHost = section["SmtpHost"] ?? "";
            var smtpPort = int.TryParse(section["SmtpPort"], out var p) ? p : 587;
            var smtpUser = section["SmtpUser"] ?? "";
            var smtpPass = section["SmtpPass"] ?? "";
            var fromEmail = section["FromEmail"] ?? smtpUser;
            var fromName = section["FromName"] ?? "HDKTech";
            var useSsl = bool.TryParse(section["UseSsl"], out var ssl) && ssl;

            if (string.IsNullOrWhiteSpace(smtpHost) || smtpHost == "smtp.example.com")
            {
                _logger.LogWarning("[Email] SMTP chưa cấu hình — bỏ qua gửi email tới {Email}", MaskEmail(email));
                return;
            }

            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(fromName, fromEmail));
                message.To.Add(new MailboxAddress("", email));
                message.Subject = subject;

                var bodyBuilder = new BodyBuilder { HtmlBody = htmlMessage };
                message.Body = bodyBuilder.ToMessageBody();

                using var client = new SmtpClient();

                // Chọn SecureSocketOptions phù hợp với cổng
                SecureSocketOptions socketOptions;
                if (smtpPort == 465)
                    socketOptions = SecureSocketOptions.SslOnConnect;
                else if (useSsl)
                    socketOptions = SecureSocketOptions.SslOnConnect;
                else
                    socketOptions = SecureSocketOptions.StartTls;

                await client.ConnectAsync(smtpHost, smtpPort, socketOptions);
                await client.AuthenticateAsync(smtpUser, smtpPass);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                _logger.LogInformation("[Email] Đã gửi '{Subject}' → {Email}", subject, MaskEmail(email));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Email] Lỗi gửi email '{Subject}' → {Email}", subject, MaskEmail(email));
                throw; // Ném lại để caller biết gửi thất bại (quan trọng với OTP)
            }
        }

        private static string MaskEmail(string email)
        {
            var at = email.IndexOf('@');
            if (at <= 1) return "***";
            return email[0] + "***" + email[at..];
        }
    }

    /// <summary>
    /// SmtpEmailService (giữ lại) — implement IEmailService cho xác nhận đơn hàng
    /// Dùng MailKitEmailSender nội bộ thay vì System.Net.Mail
    /// </summary>
    public class SmtpEmailService : IEmailService
    {
        private readonly IEmailSender _emailSender;
        private readonly ILogger<SmtpEmailService> _logger;

        public SmtpEmailService(
            IEmailSender emailSender,
            ILogger<SmtpEmailService> logger)
        {
            _emailSender = emailSender;
            _logger = logger;
        }

        public async Task SendOrderConfirmationAsync(Order order, string toEmail)
        {
            if (string.IsNullOrWhiteSpace(toEmail)) return;

            try
            {
                var subject = $"[HDKTech] Xác nhận đơn hàng #{order.OrderCode}";
                var body = BuildOrderConfirmationHtml(order);
                await _emailSender.SendEmailAsync(toEmail, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Email] Gửi xác nhận đơn #{Code} thất bại", order.OrderCode);
            }
        }

        private static string BuildOrderConfirmationHtml(Order order)
        {
            var items = order.Items != null
                ? string.Join("", order.Items.Select(i => $@"
                <tr>
                    <td style='padding:8px;border-bottom:1px solid #eee;'>{i.ProductNameSnapshot ?? $"SP#{i.ProductId}"}</td>
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
    <p>Đơn hàng <strong>#{order.OrderCode}</strong> đã được đặt thành công.</p>
    <table style='width:100%;border-collapse:collapse;font-size:14px;'>
      <thead><tr style='background:#f5f5f5;'>
        <th style='padding:8px;text-align:left;'>Sản phẩm</th>
        <th style='padding:8px;text-align:center;'>SL</th>
        <th style='padding:8px;text-align:right;'>Đơn giá</th>
        <th style='padding:8px;text-align:right;'>Thành tiền</th>
      </tr></thead>
      <tbody>{items}</tbody>
      <tfoot>
        <tr><td colspan='3' style='padding:8px;text-align:right;'>Phí vận chuyển:</td>
            <td style='padding:8px;text-align:right;'>{order.ShippingFee:N0}đ</td></tr>
        <tr style='background:#e8f0fe;font-weight:bold;'>
          <td colspan='3' style='padding:10px;text-align:right;'>TỔNG CỘNG:</td>
          <td style='padding:10px;text-align:right;color:#1a73e8;font-size:16px;'>{order.TotalAmount:N0}đ</td>
        </tr>
      </tfoot>
    </table>
  </div>
  <div style='background:#f5f5f5;padding:16px;text-align:center;font-size:12px;color:#999;'>
    HDKTech — Chuyên laptop chính hãng
  </div>
</body>
</html>";
        }
    }
}
