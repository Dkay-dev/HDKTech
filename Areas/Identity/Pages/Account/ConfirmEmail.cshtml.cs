// Areas/Identity/Pages/Account/ConfirmEmail.cshtml.cs
using System.Text;
using HDKTech.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;

namespace HDKTech.Areas.Identity.Pages.Account
{
    public class ConfirmEmailModel : PageModel
    {
        private readonly UserManager<AppUser> _userManager;

        public ConfirmEmailModel(UserManager<AppUser> userManager)
        {
            _userManager = userManager;
        }

        [TempData]
        public string? StatusMessage { get; set; }

        public bool IsSuccess { get; set; }
        public string? UserEmail { get; set; }

        public async Task<IActionResult> OnGetAsync(string userId, string code)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(code))
                return RedirectToPage("/Index");

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound($"Không tìm thấy user ID '{userId}'.");

            UserEmail = user.Email;

            try
            {
                var decodedCode = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
                var result = await _userManager.ConfirmEmailAsync(user, decodedCode);

                IsSuccess = result.Succeeded;
                StatusMessage = result.Succeeded
                    ? "Email đã được xác nhận thành công!"
                    : "Lỗi xác nhận email. Link đã hết hạn hoặc không hợp lệ.";
            }
            catch
            {
                IsSuccess = false;
                StatusMessage = "Link xác nhận không hợp lệ.";
            }

            return Page();
        }
    }
}