// Areas/Identity/Pages/Account/ExternalLogin.cshtml.cs
using HDKTech.Models;
using HDKTech.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;

namespace HDKTech.Areas.Identity.Pages.Account
{
    public class ExternalLoginModel : PageModel
    {
        private readonly SignInManager<AppUser> _signInManager;
        private readonly UserManager<AppUser> _userManager;
        private readonly IUserStore<AppUser> _userStore;
        private readonly IUserEmailStore<AppUser> _emailStore;
        private readonly ILogger<ExternalLoginModel> _logger;
        private readonly IEmailSender _emailSender;

        public ExternalLoginModel(
            SignInManager<AppUser> signInManager,
            UserManager<AppUser> userManager,
            IUserStore<AppUser> userStore,
            ILogger<ExternalLoginModel> logger,
            IEmailSender emailSender)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _userStore = userStore;
            _emailStore = (IUserEmailStore<AppUser>)userStore;
            _logger = logger;
            _emailSender = emailSender;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string? ProviderDisplayName { get; set; }
        public string? ReturnUrl { get; set; }

        [TempData]
        public string? ErrorMessage { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Vui lòng nhập email")]
            [EmailAddress(ErrorMessage = "Email không hợp lệ")]
            [Display(Name = "Email")]
            public string Email { get; set; } = string.Empty;

            [Display(Name = "Họ tên")]
            public string? FullName { get; set; }
        }

        public IActionResult OnGet() => RedirectToPage("./Login");

        public IActionResult OnPost(string provider, string? returnUrl = null)
        {
            var redirectUrl = Url.Page("./ExternalLogin", pageHandler: "Callback",
                values: new { returnUrl });
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(
                provider, redirectUrl);
            return new ChallengeResult(provider, properties);
        }

        public async Task<IActionResult> OnGetCallbackAsync(
            string? returnUrl = null, string? remoteError = null)
        {
            returnUrl ??= Url.Content("~/");

            if (remoteError != null)
            {
                ErrorMessage = $"Lỗi từ Google: {remoteError}";
                return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
            }

            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                ErrorMessage = "Không thể lấy thông tin từ Google. Vui lòng thử lại.";
                return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
            }

            // Thử đăng nhập bằng external login đã có
            var result = await _signInManager.ExternalLoginSignInAsync(
                info.LoginProvider, info.ProviderKey,
                isPersistent: false, bypassTwoFactor: true);

            if (result.Succeeded)
            {
                _logger.LogInformation("{Name} đăng nhập bằng {Provider}.",
                    info.Principal.Identity?.Name, info.LoginProvider);

                // Cập nhật LastLoginAt
                var existUser = await _userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
                if (existUser != null)
                {
                    existUser.LastLoginAt = DateTime.Now;
                    await _userManager.UpdateAsync(existUser);
                }

                return LocalRedirect(returnUrl);
            }

            if (result.IsLockedOut)
                return RedirectToPage("./Lockout");

            // Lần đầu đăng nhập bằng Google → tự động tạo tài khoản
            ReturnUrl = returnUrl;
            ProviderDisplayName = info.ProviderDisplayName;

            var email = info.Principal.FindFirstValue(ClaimTypes.Email) ?? "";
            var fullName = info.Principal.FindFirstValue(ClaimTypes.Name) ?? "";

            // Thử tự động tạo tài khoản
            if (!string.IsNullOrEmpty(email))
            {
                var autoResult = await AutoCreateExternalUserAsync(email, fullName, info, returnUrl);
                if (autoResult != null) return autoResult;
            }

            // Fallback: hiển thị form nhập email
            Input.Email = email;
            Input.FullName = fullName;
            return Page();
        }

        /// <summary>
        /// Tự động tạo tài khoản từ thông tin Google.
        /// Email Google đã xác thực → bỏ qua bước confirm email.
        /// </summary>
        private async Task<IActionResult?> AutoCreateExternalUserAsync(
            string email, string fullName, ExternalLoginInfo info, string returnUrl)
        {
            // Kiểm tra email đã tồn tại chưa
            var existingUser = await _userManager.FindByEmailAsync(email);
            if (existingUser != null)
            {
                // Liên kết Google vào tài khoản hiện tại
                await _userManager.AddLoginAsync(existingUser, info);
                existingUser.EmailConfirmed = true;
                existingUser.LastLoginAt = DateTime.Now;
                await _userManager.UpdateAsync(existingUser);
                await _signInManager.SignInAsync(existingUser, isPersistent: false, info.LoginProvider);
                return LocalRedirect(returnUrl);
            }

            // Tạo user mới
            var user = new AppUser
            {
                FullName = string.IsNullOrWhiteSpace(fullName) ? email.Split('@')[0] : fullName,
                EmailConfirmed = true,   // Google đã xác thực email
                IsActive = true,
                CreatedAt = DateTime.Now,
                LastLoginAt = DateTime.Now
            };

            await _userStore.SetUserNameAsync(user, email, CancellationToken.None);
            await _emailStore.SetEmailAsync(user, email, CancellationToken.None);

            // Lấy avatar từ Google nếu có
            var pictureUrl = info.Principal.FindFirstValue("picture") ?? "";
            if (!string.IsNullOrEmpty(pictureUrl))
                user.AvatarUrl = pictureUrl;

            var createResult = await _userManager.CreateAsync(user);
            if (!createResult.Succeeded)
            {
                _logger.LogError("Tạo user {Email} từ Google thất bại: {Errors}",
                    email, string.Join(", ", createResult.Errors.Select(e => e.Description)));
                return null;
            }

            await _userManager.AddLoginAsync(user, info);
            await _userManager.AddToRoleAsync(user, "Customer");

            _logger.LogInformation("User {Email} được tạo tự động qua {Provider}.", email, info.LoginProvider);

            // Gửi email chào mừng (không cần xác thực vì Google đã xác thực)
            _ = Task.Run(async () =>
            {
                try
                {
                    await _emailSender.SendEmailAsync(email,
                        "🎉 [HDKTech] Chào mừng bạn đến với HDKTech!",
                        BuildWelcomeEmailHtml(user.FullName));
                }
                catch { /* không throw — email chào mừng không quan trọng */ }
            });

            await _signInManager.SignInAsync(user, isPersistent: false, info.LoginProvider);
            return LocalRedirect(returnUrl);
        }

        public async Task<IActionResult> OnPostConfirmationAsync(string? returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                ErrorMessage = "Lỗi xác thực. Vui lòng thử lại.";
                return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
            }

            if (!ModelState.IsValid)
            {
                ProviderDisplayName = info.ProviderDisplayName;
                ReturnUrl = returnUrl;
                return Page();
            }

            var user = new AppUser
            {
                FullName = (Input.FullName ?? Input.Email.Split('@')[0]).Trim(),
                EmailConfirmed = true,
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            await _userStore.SetUserNameAsync(user, Input.Email, CancellationToken.None);
            await _emailStore.SetEmailAsync(user, Input.Email, CancellationToken.None);

            var result = await _userManager.CreateAsync(user);
            if (result.Succeeded)
            {
                result = await _userManager.AddLoginAsync(user, info);
                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, "Customer");
                    await _signInManager.SignInAsync(user, isPersistent: false, info.LoginProvider);
                    return LocalRedirect(returnUrl);
                }
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            ProviderDisplayName = info.ProviderDisplayName;
            ReturnUrl = returnUrl;
            return Page();
        }

        private static string BuildWelcomeEmailHtml(string fullName) => $@"
<!DOCTYPE html>
<html lang='vi'>
<head><meta charset='UTF-8'/></head>
<body style='font-family:Arial,sans-serif;color:#333;max-width:600px;margin:auto;'>
  <div style='background:#ef4444;padding:28px;text-align:center;border-radius:12px 12px 0 0;'>
    <h1 style='color:#fff;margin:0;font-size:26px;'>🎉 Chào mừng đến HDKTech!</h1>
  </div>
  <div style='background:#fff;padding:32px;border-radius:0 0 12px 12px;'>
    <p>Xin chào <strong>{fullName}</strong>,</p>
    <p>Tài khoản của bạn đã được tạo thành công thông qua Google.</p>
    <p>Bạn có thể đăng nhập bất cứ lúc nào bằng tài khoản Google đã liên kết.</p>
    <div style='text-align:center;margin:24px 0;'>
      <a href='https://hdktech.vn'
         style='background:#ef4444;color:#fff;padding:12px 32px;border-radius:8px;
                text-decoration:none;font-weight:700;'>
        🛍 MUA SẮM NGAY
      </a>
    </div>
  </div>
</body>
</html>";
    }
}