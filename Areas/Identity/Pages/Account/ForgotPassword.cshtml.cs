// Areas/Identity/Pages/Account/ForgotPassword.cshtml.cs
using System.ComponentModel.DataAnnotations;
using HDKTech.Models;
using HDKTech.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HDKTech.Areas.Identity.Pages.Account
{
    public class ForgotPasswordModel : PageModel
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IEmailSender         _emailSender;
        private readonly IOtpService          _otpService;

        public ForgotPasswordModel(
            UserManager<AppUser> userManager,
            IEmailSender         emailSender,
            IOtpService          otpService)
        {
            _userManager = userManager;
            _emailSender = emailSender;
            _otpService  = otpService;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            [Required(ErrorMessage = "Vui lòng nhập email")]
            [EmailAddress(ErrorMessage = "Email không hợp lệ")]
            public string Email { get; set; } = string.Empty;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            var user = await _userManager.FindByEmailAsync(Input.Email);

            // Luôn redirect để không lộ email có tồn tại hay không
            if (user != null && user.EmailConfirmed)
            {
                var otp = _otpService.GenerateOtp(Input.Email, "pwd-reset");

                await _emailSender.SendEmailAsync(
                    Input.Email,
                    "🔑 [HDKTech] Mã đặt lại mật khẩu",
                    BuildOtpEmailHtml(user.FullName ?? "bạn", otp));
            }

            return RedirectToPage("./ForgotPasswordConfirmation",
                new { email = Input.Email });
        }

        private static string BuildOtpEmailHtml(string fullName, string otp) => $@"
<!DOCTYPE html>
<html lang='vi'>
<head><meta charset='UTF-8'/></head>
<body style='font-family:Arial,sans-serif;color:#333;max-width:600px;margin:auto;background:#f5f5f5;'>
  <div style='background:#2563eb;padding:28px 24px;text-align:center;border-radius:12px 12px 0 0;'>
    <h1 style='color:#fff;margin:0;font-size:26px;'>HDK<span style='font-weight:300'>Tech</span></h1>
    <p style='color:rgba(255,255,255,0.85);margin:6px 0 0;font-size:14px;'>Đặt lại mật khẩu</p>
  </div>

  <div style='background:#fff;padding:32px 24px;border-radius:0 0 12px 12px;box-shadow:0 4px 16px rgba(0,0,0,0.08);'>
    <p style='font-size:16px;'>Xin chào <strong>{fullName}</strong>,</p>
    <p style='color:#555;line-height:1.7;'>
      Chúng tôi nhận được yêu cầu đặt lại mật khẩu cho tài khoản HDKTech của bạn.<br/>
      Vui lòng nhập mã OTP bên dưới để tiếp tục.
    </p>

    <div style='text-align:center;margin:32px 0;'>
      <div style='display:inline-block;background:#eff6ff;border:2px dashed #2563eb;
                  border-radius:16px;padding:20px 48px;'>
        <div style='font-size:11px;font-weight:700;color:#2563eb;letter-spacing:2px;
                    text-transform:uppercase;margin-bottom:8px;'>Mã đặt lại mật khẩu</div>
        <div style='font-size:42px;font-weight:900;color:#2563eb;letter-spacing:10px;
                    font-family:monospace;'>{otp}</div>
      </div>
    </div>

    <p style='color:#888;font-size:13px;text-align:center;'>
      Mã có hiệu lực trong <strong>15 phút</strong>.<br/>
      Nếu bạn không yêu cầu đặt lại mật khẩu, hãy bỏ qua email này.
    </p>

    <hr style='border:none;border-top:1px solid #f0f0f0;margin:24px 0;'/>
    <p style='color:#aaa;font-size:12px;text-align:center;'>
      Không chia sẻ mã này với bất kỳ ai — HDKTech sẽ không bao giờ hỏi mã OTP của bạn.
    </p>
  </div>

  <p style='text-align:center;color:#bbb;font-size:11px;margin-top:16px;'>
    © 2026 HDKTech — Email này được gửi tự động, vui lòng không reply.
  </p>
</body>
</html>";
    }
}
