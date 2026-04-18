using System.Security.Claims;
using HDKTech.Areas.Admin.Constants;
using HDKTech.ChucNangPhanQuyen;
using Microsoft.AspNetCore.Identity;

namespace HDKTech.Data
{
    /// <summary>
    /// Seed duy nhất cho hệ phân quyền ASP.NET Identity.
    ///
    /// - 4 Role chuẩn: Admin, Manager, Staff, Customer → AspNetRoles.
    /// - Permission của từng role được lưu dưới dạng RoleClaim:
    ///     Type  = "permission"
    ///     Value = "Module.Action" (vd: "Product.Read").
    ///
    /// Các policy granular trong Program.cs (vd <c>Policy = "Order.Read"</c>) sẽ
    /// được xử lý bởi <see cref="PermissionHandler"/> — handler đọc thẳng các
    /// RoleClaim này để quyết định Succeed/Fail.
    /// </summary>
    public static class IdentityRoleSeed
    {
        // ── Bộ permission chuẩn hợp nhất từ AllSystemPermissions + PermissionSeed ──
        // Dùng chung cho cả Program.cs (đăng ký policy) lẫn IdentityRoleSeed (gán claim).
        public static readonly IReadOnlyList<string> AllPermissions = new[]
        {
            "Dashboard.View",
            "Product.Read",    "Product.Create",   "Product.Update",   "Product.Delete",
            "Category.Read",   "Category.Create",  "Category.Update",  "Category.Delete",
            "Brand.Read",      "Brand.Create",     "Brand.Update",     "Brand.Delete",
            "Inventory.Read",  "Inventory.Update",
            "Order.Read",      "Order.Update",     "Order.Delete",
            "Promotion.Read",  "Promotion.Create", "Promotion.Update", "Promotion.Delete",
            "Banner.Read",     "Banner.Create",    "Banner.Update",    "Banner.Delete",
            "Role.Read",       "Role.Update",
            "Report.Export",
            "SystemLog.Read",
            "User.Read",       "User.Update",      "User.Delete",
        };

        // ── Phân bổ permission theo role ──────────────────────────────────────────
        // Customer: KHÔNG có permission nào (chỉ truy cập public endpoint).
        // Admin   : toàn bộ.
        // Manager : toàn bộ TRỪ các mảng chỉ dành cho Admin (User.*, Role.*, SystemLog.*).
        // Staff   : Dashboard.View, Product.Read, Inventory.Read, Order.Read, Order.Update.
        private static readonly HashSet<string> AdminOnlyPrefixes =
            new(StringComparer.OrdinalIgnoreCase) { "User.", "Role.", "SystemLog." };

        private static readonly HashSet<string> StaffPermissions =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "Dashboard.View",
                "Product.Read",
                "Inventory.Read",
                "Order.Read",
                "Order.Update",
            };

        public static async Task SeedAsync(IServiceProvider services)
        {
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

            await EnsureRoleWithClaimsAsync(
                roleManager,
                AdminConstants.AdminRole,
                AllPermissions);

            await EnsureRoleWithClaimsAsync(
                roleManager,
                AdminConstants.ManagerRole,
                AllPermissions.Where(p => !AdminOnlyPrefixes.Any(pref =>
                    p.StartsWith(pref, StringComparison.OrdinalIgnoreCase))));

            await EnsureRoleWithClaimsAsync(
                roleManager,
                "Staff",
                StaffPermissions);

            await EnsureRoleWithClaimsAsync(
                roleManager,
                "Customer",
                Enumerable.Empty<string>());
        }

        /// <summary>
        /// Đảm bảo role tồn tại; đồng bộ claims với <paramref name="desiredPermissions"/>:
        ///   - thêm claim còn thiếu
        ///   - giữ nguyên claim đã đúng
        ///   - xoá claim thừa (nằm trong <see cref="AllPermissions"/> nhưng không còn được cấp cho role này)
        ///
        /// Không đụng đến claim có <c>Type != "permission"</c> (nếu dev khác đã seed thêm).
        /// </summary>
        private static async Task EnsureRoleWithClaimsAsync(
            RoleManager<IdentityRole>    roleManager,
            string                       roleName,
            IEnumerable<string>          desiredPermissions)
        {
            var role = await roleManager.FindByNameAsync(roleName);
            if (role == null)
            {
                role = new IdentityRole(roleName) { NormalizedName = roleName.ToUpperInvariant() };
                var createResult = await roleManager.CreateAsync(role);
                if (!createResult.Succeeded)
                {
                    var errors = string.Join("; ", createResult.Errors.Select(e => e.Description));
                    throw new InvalidOperationException(
                        $"IdentityRoleSeed: không tạo được role '{roleName}'. Lỗi: {errors}");
                }
            }

            var desired = new HashSet<string>(desiredPermissions, StringComparer.OrdinalIgnoreCase);
            var existingPermissionClaims = (await roleManager.GetClaimsAsync(role))
                .Where(c => c.Type == PermissionHandler.PermissionClaimType)
                .ToList();

            // Xoá claim thừa
            foreach (var c in existingPermissionClaims)
            {
                if (!desired.Contains(c.Value))
                    await roleManager.RemoveClaimAsync(role, c);
            }

            var already = new HashSet<string>(
                existingPermissionClaims.Select(c => c.Value),
                StringComparer.OrdinalIgnoreCase);

            // Thêm claim còn thiếu
            foreach (var perm in desired)
            {
                if (already.Contains(perm)) continue;
                await roleManager.AddClaimAsync(role,
                    new Claim(PermissionHandler.PermissionClaimType, perm));
            }
        }
    }
}
