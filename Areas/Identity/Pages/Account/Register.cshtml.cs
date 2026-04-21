// Areas/Identity/Pages/Account/Register.cshtml.cs
using System.ComponentModel.DataAnnotations;
using HDKTech.Models;
using HDKTech.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HDKTech.Areas.Identity.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<AppUser>   _signInManager;
        private readonly UserManager<AppUser>     _userManager;
        private readonly IUserStore<AppUser>      _userStore;
        private readonly IUserEmailStore<AppUser> _emailStore;
        private readonly ILogger<RegisterModel>   _logger;
        private readonly IEmailSender             _emailSender;
        private readonly IOtpService              _otpService;

        public RegisterModel(
            UserManager<AppUser>   userManager,
            IUserStore<AppUser>    userStore,
            SignInManager<AppUser> signInManager,
            ILogger<RegisterModel> logger,
            IEmailSender           emailSender,
            IOtpService            otpService)
        {
            _userManager   = userManager;
            _userStore     = userStore;
            _emailStore    = (IUserEmailStore<AppUser>)userStore;
            _signInManager = signInManager;
            _logger        = logger;
            _emailSender   = emailSender;
            _otpService    = otpService;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string? ReturnUrl { get; set; }

        public IList<AuthenticationScheme> ExternalLogins { get; set; } = new List<AuthenticationScheme>();

        public class InputModel
        {
            [Required(ErrorMessage = "Vui lòng nhập họ tên")]
            [StringLength(100, MinimumLength = 2, ErrorMessage = "Họ tên từ 2–100 ký tự")]
            [Display(Name = "Họ tên")]
            public string FullName { get; set; } = string.Empty;

            [Required(ErrorMessage = "Vui lòng nhập email")]
            [EmailAddress(ErrorMessage = "Email không hợp lệ")]
            [Display(Name = "Email")]
            public string Email { get; set; } = string.Empty;

            [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
            [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu tối thiểu 6 ký tự")]
            [DataType(DataType.Password)]
            [Display(Name = "Mật khẩu")]
            public string Password { get; set; } = string.Empty;

            [DataType(DataType.Password)]
            [Display(Name = "Xác nhận mật khẩu")]
            [Compare("Password", ErrorMessage = "Mật khẩu không khớp")]
            public string ConfirmPassword { get; set; } = string.Empty;

            [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
            [Display(Name = "Số điện thoại")]
            public string? PhoneNumber { get; set; }
        }

        public async Task OnGetAsync(string? returnUrl = null)
        {
            ReturnUrl      = returnUrl;
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            returnUrl    ??= Url.Content("~/");
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            if (!ModelState.IsValid)
                return Page();

            var user = new AppUser
            {
                FullName    = Input.FullName.Trim(),
                PhoneNumber = Input.PhoneNumber?.Trim(),
                CreatedAt   = DateTime.Now,
                IsActive    = true
            };

            await _userStore.SetUserNameAsync(user, Input.Email, CancellationToken.None);
            await _emailStore.SetEmailAsync(user, Input.Email, CancellationToken.None);

            var result = await _userManager.CreateAsync(user, Input.Password);

            if (result.Succeeded)
            {
                _logger.LogInformation("User {Email} đăng ký tài khoản mới.", Input.Email);
                await _userManager.AddToRoleAsync(user, "Customer");

                // Sinh OTP 6 số và gửi email
                var otp = _otpService.GenerateOtp(Input.Email, "email-confirm");

                await _emailSender.SendEmailAsync(
                    Input.Email,
                    "🔐 [HDKTech] Mã xác nhận đăng ký tài khoản",
                    BuildOtpEmailHtml(Input.FullName, otp));

                return RedirectToPage("RegisterConfirmation",
                    new { email = Input.Email, returnUrl });
            }

            foreach (var error in result.Errors)
            {
                var msg = error.Code switch
                {
                    "DuplicateUserName" => $"Email '{Input.Email}' đã được sử dụng.",
                    "DuplicateEmail"    => $"Email '{Input.Email}' đã được sử dụng.",
                    "InvalidEmail"      => "Email không hợp lệ.",
                    "PasswordTooShort"  => "Mật khẩu tối thiểu 6 ký tự.",
                    _                   => error.Description
                };
                ModelState.AddModelError(string.Empty, msg);
            }

            return Page();
        }

        private static string BuildOtpEmailHtml(string fullName, string otp) => $@"
<!DOCTYPE html>
<html lang='vi'>
<head><meta charset='UTF-8'/></head>
<body style='font-family:Arial,sans-serif;color:#333;max-width:600px;margin:auto;background:#f5f5f5;'>
  <div style='background:#ef4444;padding:28px 24px;text-align:center;border-radius:12px 12px 0 0;'>
    <h1 style='color:#fff;margin:0;font-size:26px;letter-spacing:-0.5px;'>HDK<span style='font-weight:300'>Tech</span></h1>
    <p style='color:rgba(255,255,255,0.85);margin:6px 0 0;font-size:14px;'>Xác nhận đăng ký tài khoản</p>
  </div>

  <div style='background:#fff;padding:32px 24px;border-radius:0 0 12px 12px;box-shadow:0 4px 16px rgba(0,0,0,0.08);'>
    <p style='font-size:16px;'>Xin chào <strong>{fullName}</strong>,</p>
    <p style='color:#555;line-height:1.7;'>
      Cảm ơn bạn đã đăng ký tài khoản tại <strong>HDKTech</strong>!<br/>
      Vui lòng nhập mã OTP bên dưới để xác nhận email và kích hoạt tài khoản.
    </p>

    <div style='text-align:center;margin:32px 0;'>
      <div style='display:inline-block;background:#fef2f2;border:2px dashed #ef4444;
                  border-radius:16px;padding:20px 48px;'>
        <div style='font-size:11px;font-weight:700;color:#ef4444;letter-spacing:2px;
                    text-transform:uppercase;margin-bottom:8px;'>Mã xác nhận</div>
        <div style='font-size:42px;font-weight:900;color:#ef4444;letter-spacing:10px;
                    font-family:monospace;'>{otp}</div>
      </div>
    </div>

    <p style='color:#888;font-size:13px;text-align:center;'>
      Mã có hiệu lực trong <strong>15 phút</strong>.<br/>
      Nếu bạn không đăng ký tài khoản, hãy bỏ qua email này.
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
