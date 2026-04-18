using HDKTech.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace HDKTech.ChucNangPhanQuyen
{
    /// <summary>
    /// Policy-based Authorization Handler — đọc hoàn toàn từ ASP.NET Identity.
    ///
    /// Flow mới (sau khi hợp nhất vào Identity):
    ///   1. Lấy user hiện tại qua <see cref="UserManager{AppUser}"/>.
    ///   2. Lấy danh sách role của user (<see cref="UserManager{AppUser}.GetRolesAsync"/>).
    ///   3. Với mỗi role, lấy claim của role (<see cref="RoleManager{IdentityRole}.GetClaimsAsync"/>)
    ///      trong đó Claim.Type == "permission" và Claim.Value == "Module.Action".
    ///   4. Nếu có ít nhất 1 role chứa claim khớp requirement → Succeed.
    ///
    /// Admin luôn bypass (được gán đủ permission lúc seed, nhưng shortcut để tránh query thừa).
    /// </summary>
    public class PermissionHandler : AuthorizationHandler<PermissionRequirement>
    {
        public const string PermissionClaimType = "permission";

        private readonly UserManager<AppUser>      _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public PermissionHandler(
            UserManager<AppUser>      userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            PermissionRequirement requirement)
        {
            if (context.User?.Identity?.IsAuthenticated != true)
            {
                context.Fail();
                return;
            }

            var user = await _userManager.GetUserAsync(context.User);
            if (user == null)
            {
                context.Fail();
                return;
            }

            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Count == 0)
            {
                context.Fail();
                return;
            }

            // Admin bypass
            if (roles.Contains(Areas.Admin.Constants.AdminConstants.AdminRole))
            {
                context.Succeed(requirement);
                return;
            }

            var targetValue = $"{requirement.Module}.{requirement.Action}";

            foreach (var roleName in roles)
            {
                var role = await _roleManager.FindByNameAsync(roleName);
                if (role == null) continue;

                var claims = await _roleManager.GetClaimsAsync(role);
                if (claims.Any(c =>
                        c.Type == PermissionClaimType &&
                        string.Equals(c.Value, targetValue, StringComparison.OrdinalIgnoreCase)))
                {
                    context.Succeed(requirement);
                    return;
                }
            }

            context.Fail();
        }
    }
}
