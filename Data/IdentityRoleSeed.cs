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
            "RevenueAnalytics.Read", "RevenueAnalytics.Export",
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
        /// Đảm bảo role tồn tại và chỉ seed permission MẶC ĐỊNH khi role
        /// được tạo mới ở lần chạy đầu tiên.
        ///
        /// Quan trọng: nếu role đã tồn tại, KHÔNG chạm vào RoleClaims nữa —
        /// để giữ nguyên mọi thay đổi mà admin đã lưu qua Permission Matrix
        /// ở UI (/admin/role/manage-permissions/{roleCode}).
        ///
        /// Trước đây hàm này đồng bộ cưỡng bức: thiếu thì thêm, thừa thì xoá.
        /// Điều đó khiến mọi lần app khởi động lại đều reset permission về
        /// danh sách hardcode, ghi đè thao tác của admin.
        /// </summary>
        private static async Task EnsureRoleWithClaimsAsync(
            RoleManager<IdentityRole>    roleManager,
            string                       roleName,
            IEnumerable<string>          desiredPermissions)
        {
            var role = await roleManager.FindByNameAsync(roleName);
            var isNewRole = false;

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
                isNewRole = true;
            }

            // Chỉ seed permission khi role VỪA được tạo mới.
            // Role đã có → tôn trọng cấu hình admin đã chỉnh qua UI, không đụng vào.
            if (!isNewRole) return;

            var desired = new HashSet<string>(desiredPermissions, StringComparer.OrdinalIgnoreCase);
            foreach (var perm in desired)
            {
                await roleManager.AddClaimAsync(role,
                    new Claim(PermissionHandler.PermissionClaimType, perm));
            }
        }
    }
}
