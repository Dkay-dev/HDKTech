using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using HDKTech.Models;

namespace HDKTech.ChucNangPhanQuyen
{
    /// <summary>
    /// Policy-based Authorization Handler — Sprint 1 Refactor.
    ///
    /// Thay vì đọc từ bảng custom RolePermissions, handler này truy vấn
    /// bảng chuẩn của ASP.NET Identity: AspNetRoleClaims.
    ///
    /// Flow:
    ///   User → AspNetUserRoles → IdentityRole → AspNetRoleClaims
    ///   Claim Type  = "Permission"
    ///   Claim Value = "Module.Action"  (vd: "Inventory.Update", "Product.Delete")
    ///
    /// Để gán quyền cho Role: dùng RoleController.SavePermissions()
    /// hoặc gọi trực tiếp: await _roleManager.AddClaimAsync(role, new Claim("Permission","Inventory.Update"))
    /// </summary>
    public class PermissionHandler : AuthorizationHandler<PermissionRequirement>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly UserManager<AppUser>   _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public PermissionHandler(
            IHttpContextAccessor       httpContextAccessor,
            UserManager<AppUser>       userManager,
            RoleManager<IdentityRole>  roleManager)
        {
            _httpContextAccessor = httpContextAccessor;
            _userManager         = userManager;
            _roleManager         = roleManager;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            PermissionRequirement       requirement)
        {
            var userPrincipal = _httpContextAccessor.HttpContext?.User;

            if (userPrincipal == null || !userPrincipal.Identity!.IsAuthenticated)
            {
                context.Fail();
                return;
            }

            // Admin role — bypass mọi kiểm tra quyền
            if (userPrincipal.IsInRole("Admin"))
            {
                context.Succeed(requirement);
                return;
            }

            var userId = userPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) { context.Fail(); return; }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) { context.Fail(); return; }

            var roleNames = await _userManager.GetRolesAsync(user);
            if (!roleNames.Any()) { context.Fail(); return; }

            // Giá trị cần tìm trong AspNetRoleClaims: "Module.Action"
            var requiredValue = $"{requirement.Module}.{requirement.Action}";

            foreach (var roleName in roleNames)
            {
                // Admin role — catch-all
                if (roleName.Equals("Admin", StringComparison.OrdinalIgnoreCase))
                {
                    context.Succeed(requirement);
                    return;
                }

                var identityRole = await _roleManager.FindByNameAsync(roleName);
                if (identityRole == null) continue;

                // Đọc claims từ bảng AspNetRoleClaims
                var claims = await _roleManager.GetClaimsAsync(identityRole);

                bool granted = claims.Any(c =>
                    c.Type  == "Permission" &&
                    c.Value.Equals(requiredValue, StringComparison.OrdinalIgnoreCase));

                if (granted)
                {
                    context.Succeed(requirement);
                    return;
                }
            }

            context.Fail();
        }
    }
}
