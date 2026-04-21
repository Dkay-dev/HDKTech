// Areas/Identity/Pages/Account/ForgotPasswordConfirmation.cshtml.cs
using System.ComponentModel.DataAnnotations;
using HDKTech.Models;
using HDKTech.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HDKTech.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class ForgotPasswordConfirmation : PageModel
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IOtpService          _otpService;
        private readonly IEmailSender         _emailSender;

        public ForgotPasswordConfirmation(
            UserManager<AppUser> userManager,
            IOtpService          otpService,
            IEmailSender         emailSender)
        {
            _userManager = userManager;
            _otpService  = otpService;
            _emailSender = emailSender;
        }

        [BindProperty(SupportsGet = true)]
        public string Email { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Vui lòng nhập mã OTP")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "Mã OTP gồm 6 chữ số")]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "Mã OTP chỉ gồm chữ số")]
        public string Otp { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu tối thiểu 6 ký tự")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu mới")]
        public string Password { get; set; } = string.Empty;

        [BindProperty]
        [DataType(DataType.Password)]
        [Display(Name = "Xác nhận mật khẩu")]
        [Compare(nameof(Password), ErrorMessage = "Mật khẩu không khớp")]
        public string ConfirmPassword { get; set; } = string.Empty;

        public string? ErrorMessage { get; set; }
        public bool ResetSuccess { get; set; }
        public bool ResendSuccess { get; set; }

        public void OnGet(string? email = null)
        {
            Email = email ?? string.Empty;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            var user = await _userManager.FindByEmailAsync(Email);
            if (user == null)
            {
                // Không lộ thông tin
                ResetSuccess = true;
                return Page();
            }

            if (!_otpService.ValidateOtp(Email, "pwd-reset", Otp))
            {
                ErrorMessage = "Mã OTP không đúng hoặc đã hết hạn. Vui lòng thử lại.";
                return Page();
            }

            // Dùng token Identity để reset password
            var token  = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, Password);

            if (result.Succeeded)
            {
                ResetSuccess = true;
                return Page();
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            return Page();
        }

        public async Task<IActionResult> OnPostResendAsync()
        {
            if (string.IsNullOrEmpty(Email))
                return Page();

            var user = await _userManager.FindByEmailAsync(Email);
            if (user != null && user.EmailConfirmed)
            {
                var otp = _otpService.GenerateOtp(Email, "pwd-reset");
                await _emailSender.SendEmailAsync(
                    Email,
                    "🔑 [HDKTech] Mã đặt lại mật khẩu",
                    BuildOtpEmailHtml(user.FullName ?? "bạn", otp));
            }

            ResendSuccess = true;
            return Page();
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
    <p>Xin chào <strong>{fullName}</strong>, đây là mã OTP mới:</p>
    <div style='text-align:center;margin:28px 0;'>
      <div style='display:inline-block;background:#eff6ff;border:2px dashed #2563eb;border-radius:16px;padding:20px 48px;'>
        <div style='font-size:11px;font-weight:700;color:#2563eb;letter-spacing:2px;text-transform:uppercase;margin-bottom:8px;'>Mã đặt lại mật khẩu</div>
        <div style='font-size:42px;font-weight:900;color:#2563eb;letter-spacing:10px;font-family:monospace;'>{otp}</div>
      </div>
    </div>
    <p style='color:#888;font-size:13px;text-align:center;'>Mã có hiệu lực trong <strong>15 phút</strong>.</p>
  </div>
</body>
</html>";
    }
}
