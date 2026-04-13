// ✅ Fix #1: Override trang Login của Identity
// Thêm logic redirect theo Role sau khi đăng nhập thành công:
//   - Admin / Manager / WarehouseStaff → /Admin/Dashboard
//   - Customer (mặc định)             → returnUrl hoặc trang chủ

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using HDKTech.Models;

namespace HDKTech.Areas.Identity.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<AppUser> _signInManager;
        private readonly UserManager<AppUser>   _userManager;
        private readonly ILogger<LoginModel>    _logger;

        public LoginModel(
            SignInManager<AppUser> signInManager,
            UserManager<AppUser>   userManager,
            ILogger<LoginModel>    logger)
        {
            _signInManager = signInManager;
            _userManager   = userManager;
            _logger        = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public IList<AuthenticationScheme> ExternalLogins { get; set; } = new List<AuthenticationScheme>();
        public string? ReturnUrl { get; set; }
        public string? ErrorMessage { get; set; }

        // ── Danh sách Role được điều hướng vào Admin Area ─────────────────────
        private static readonly string[] AdminRoles = { "Admin", "Manager", "WarehouseStaff" };

        public class InputModel
        {
            [Required(ErrorMessage = "Vui lòng nhập email.")]
            [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
            public string Email { get; set; } = string.Empty;

            [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
            [DataType(DataType.Password)]
            public string Password { get; set; } = string.Empty;

            [Display(Name = "Ghi nhớ đăng nhập?")]
            public bool RememberMe { get; set; }
        }

        // ── GET: Hiển thị form đăng nhập ──────────────────────────────────────
        public async Task OnGetAsync(string? returnUrl = null)
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
                ModelState.AddModelError(string.Empty, ErrorMessage);

            returnUrl ??= Url.Content("~/");

            // Xoá cookie xác thực ngoại vi còn sót lại
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
            ReturnUrl      = returnUrl;
        }

        // ── POST: Xử lý đăng nhập & redirect theo Role ───────────────────────
        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            if (!ModelState.IsValid)
                return Page();

            // Thử đăng nhập bằng Password
            var result = await _signInManager.PasswordSignInAsync(
                Input.Email,
                Input.Password,
                Input.RememberMe,
                lockoutOnFailure: true   // Bật lockout brute-force theo cấu hình Program.cs
            );

            if (result.Succeeded)
            {
                _logger.LogInformation("Người dùng {Email} đăng nhập thành công.", Input.Email);

                // ✅ Lấy thông tin user để kiểm tra Role
                var user = await _userManager.FindByEmailAsync(Input.Email);

                if (user != null)
                {
                    var roles = await _userManager.GetRolesAsync(user);

                    // ✅ Fix #1: Admin / Manager / WarehouseStaff → Admin Dashboard
                    if (roles.Any(r => AdminRoles.Contains(r)))
                    {
                        _logger.LogInformation("Admin user {Email} → redirect /Admin/Dashboard", Input.Email);
                        return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
                    }
                }

                // Customer / User thường → returnUrl hoặc trang chủ
                // LocalRedirect bảo vệ chống Open Redirect
                return LocalRedirect(returnUrl);
            }

            if (result.RequiresTwoFactor)
            {
                return RedirectToPage("./LoginWith2fa", new
                {
                    ReturnUrl    = returnUrl,
                    RememberMe   = Input.RememberMe
                });
            }

            if (result.IsLockedOut)
            {
                _logger.LogWarning("Tài khoản {Email} bị khoá tạm thời.", Input.Email);
                return RedirectToPage("./Lockout");
            }

            // Đăng nhập thất bại
            ModelState.AddModelError(string.Empty, "Email hoặc mật khẩu không đúng. Vui lòng thử lại.");
            return Page();
        }
    }
}
