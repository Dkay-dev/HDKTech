// Areas/Identity/Pages/Account/ResendEmailConfirmation.cshtml.cs
using HDKTech.Models;
using HDKTech.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;

namespace HDKTech.Areas.Identity.Pages.Account
{
    public class ResendEmailConfirmationModel : PageModel
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IEmailSender _emailSender;

        public ResendEmailConfirmationModel(
            UserManager<AppUser> userManager,
            IEmailSender emailSender)
        {
            _userManager = userManager;
            _emailSender = emailSender;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        [TempData] public string? StatusMessage { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Vui lòng nhập email")]
            [EmailAddress(ErrorMessage = "Email không hợp lệ")]
            public string Email { get; set; } = string.Empty;
        }

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            var user = await _userManager.FindByEmailAsync(Input.Email);

            // Luôn hiển thị thành công dù email có tồn tại hay không (tránh user enumeration)
            if (user == null || await _userManager.IsEmailConfirmedAsync(user))
            {
                StatusMessage = "Email xác nhận đã được gửi. Vui lòng kiểm tra hộp thư.";
                return RedirectToPage();
            }

            var userId = await _userManager.GetUserIdAsync(user);
            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

            var callbackUrl = Url.Page(
                "/Account/ConfirmEmail",
                pageHandler: null,
                values: new { area = "Identity", userId, code },
                protocol: Request.Scheme)!;

            await _emailSender.SendEmailAsync(
                Input.Email,
                "✅ [HDKTech] Xác nhận địa chỉ email của bạn",
                BuildResendHtml(user.FullName, HtmlEncoder.Default.Encode(callbackUrl)));

            StatusMessage = "Email xác nhận đã được gửi. Vui lòng kiểm tra hộp thư (kể cả Spam).";
            return RedirectToPage();
        }

        private static string BuildResendHtml(string fullName, string confirmUrl) => $@"
<!DOCTYPE html>
<html lang='vi'>
<head><meta charset='UTF-8'/></head>
<body style='font-family:Arial,sans-serif;color:#333;max-width:600px;margin:auto;'>
  <div style='background:#ef4444;padding:28px 24px;text-align:center;border-radius:12px 12px 0 0;'>
    <h1 style='color:#fff;margin:0;font-size:24px;'>HDKTech</h1>
    <p style='color:rgba(255,255,255,0.85);margin:4px 0 0;'>Gửi lại email xác nhận</p>
  </div>
  <div style='background:#fff;padding:32px 24px;border-radius:0 0 12px 12px;'>
    <p>Xin chào <strong>{fullName}</strong>,</p>
    <p>Bạn đã yêu cầu gửi lại email xác nhận. Nhấn nút bên dưới để kích hoạt tài khoản.</p>
    <div style='text-align:center;margin:32px 0;'>
      <a href='{confirmUrl}'
         style='background:#ef4444;color:#fff;padding:14px 36px;border-radius:8px;
                text-decoration:none;font-weight:700;font-size:15px;display:inline-block;'>
        ✅ XÁC NHẬN EMAIL
      </a>
    </div>
    <p style='color:#888;font-size:13px;'>Link có hiệu lực trong 24 giờ.</p>
  </div>
</body>
</html>";
    }
}