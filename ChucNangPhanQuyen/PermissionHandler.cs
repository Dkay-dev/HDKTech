using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using HDKTech.Data;
using HDKTech.Models;
using System.Security.Claims;

namespace HDKTech.ChucNangPhanQuyen
{
    public class PermissionHandler : AuthorizationHandler<PermissionRequirement>
    {
        private readonly HDKTechContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly UserManager<NguoiDung> _userManager;

        public PermissionHandler(HDKTechContext context, IHttpContextAccessor httpContextAccessor, UserManager<NguoiDung> userManager)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _userManager = userManager;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
        {
            var userPrincipal = _httpContextAccessor.HttpContext?.User;
            if (userPrincipal == null || !userPrincipal.Identity.IsAuthenticated)
            {
                context.Fail();
                return;
            }

            // If user is in Admin role (identity role), succeed (site admin has full access)
            if (userPrincipal.IsInRole("Admin"))
            {
                context.Succeed(requirement);
                return;
            }

            var userId = userPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                context.Fail();
                return;
            }

            // Get user entity and their identity role names
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                context.Fail();
                return;
            }

            var identityRoleNames = await _userManager.GetRolesAsync(user);
            if (identityRoleNames == null || identityRoleNames.Count == 0)
            {
                context.Fail();
                return;
            }

            // If any identity role is Admin, succeed
            if (identityRoleNames.Contains("Admin"))
            {
                context.Succeed(requirement);
                return;
            }

            // Map identity role names to custom Role table (Role.RoleName)
            var customRoleIds = await _context.Roles
                .AsNoTracking()
                .Where(r => identityRoleNames.Contains(r.RoleName))
                .Select(r => r.RoleId)
                .ToListAsync();

            if (!customRoleIds.Any())
            {
                context.Fail();
                return;
            }

            // Get permissions for those custom roles
            var permissions = await _context.RolePermissions
                .AsNoTracking()
                .Include(rp => rp.Permission)
                .Where(rp => customRoleIds.Contains(rp.RoleId))
                .Select(rp => rp.Permission)
                .ToListAsync();

            if (permissions.Any(p => p.Module == requirement.Module && p.Action == requirement.Action))
            {
                context.Succeed(requirement);
                return;
            }

            context.Fail();
        }
    }
}
