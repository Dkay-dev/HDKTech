// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using HDKTech.Models;
using HDKTech.Services;

namespace HDKTech.Areas.Identity.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<AppUser> _signInManager;
        private readonly ILogger<LoginModel>    _logger;
        // ── Giai đoạn 4: Audit log cho sự kiện đăng nhập / bị khoá ──────
        private readonly ISystemLogService      _logService;

        public LoginModel(
            SignInManager<AppUser> signInManager,
            ILogger<LoginModel>    logger,
            ISystemLogService      logService)
        {
            _signInManager = signInManager;
            _logger        = logger;
            _logService    = logService;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        public string ReturnImageUrl { get; set; }

        [TempData]
        public string ErrorMessage { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Vui lòng nhập Email.")]
            [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
            public string Email { get; set; }

            [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
            [DataType(DataType.Password)]
            public string Password { get; set; }

            [Display(Name = "Ghi nhớ đăng nhập?")]
            public bool RememberMe { get; set; }
        }

        public async Task OnGetAsync(string returnImageUrl = null)
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }

            returnImageUrl ??= Url.Content("~/");

            // Xóa cookie bên ngoài hiện có để đảm bảo quá trình đăng nhập sạch sẽ
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            ReturnImageUrl = returnImageUrl;
        }

        public async Task<IActionResult> OnPostAsync(string returnImageUrl = null)
        {
            returnImageUrl ??= Url.Content("~/");

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            if (ModelState.IsValid)
            {
                // ── Giai đoạn 4: Brute-force — lockoutOnFailure: true ────────
                // Sau 5 lần sai liên tiếp, tài khoản bị khoá 15 phút (cấu hình ở Program.cs).
                var result = await _signInManager.PasswordSignInAsync(
                    Input.Email, Input.Password, Input.RememberMe,
                    lockoutOnFailure: true);

                if (result.Succeeded)
                {
                    _logger.LogInformation("Người dùng {Email} đã đăng nhập thành công.", Input.Email);

                    // Ghi Audit Log đăng nhập thành công
                    var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
                    var ua = HttpContext.Request.Headers["User-Agent"].ToString();
                    await _logService.LogLoginAsync(Input.Email, ip, ua);

                    // ── Giai đoạn 4: Redirect đến Admin Dashboard sau khi login ──
                    // Chuyển hướng đến Dashboard controller trong Admin area
                    return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
                }
                if (result.RequiresTwoFactor)
                {
                    return RedirectToPage("./LoginWith2fa",
                        new { ReturnImageUrl = returnImageUrl, RememberMe = Input.RememberMe });
                }
                if (result.IsLockedOut)
                {
                    _logger.LogWarning("Tài khoản {Email} đã bị khoá do nhập sai nhiều lần.", Input.Email);

                    // ── Giai đoạn 4: Audit Log — ghi nhận sự kiện Lockout ────
                    await _logService.LogActionAsync(
                        username:    Input.Email,
                        actionType:  "Lockout",
                        module:      "Security",
                        description: $"Tài khoản '{Input.Email}' bị khoá tạm thời 15 phút do nhập sai mật khẩu quá 5 lần.",
                        userRole:    "Unknown"
                    );

                    return RedirectToPage("./Lockout");
                }
                else
                {
                    // Ghi log đăng nhập thất bại (không đủ nghiêm trọng để ghi audit)
                    _logger.LogWarning("Đăng nhập thất bại cho {Email}.", Input.Email);
                    ModelState.AddModelError(string.Empty, "Thông tin đăng nhập không hợp lệ.");
                    return Page();
                }
            }

            // Nếu dữ liệu không hợp lệ, hiển thị lại form
            return Page();
        }
    }
}

