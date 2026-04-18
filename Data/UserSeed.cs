using HDKTech.Areas.Admin.Constants;
using HDKTech.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HDKTech.Data.Seeds
{
    /// <summary>
    /// Seed user dùng UserManager để Identity tự hash password chuẩn (PBKDF2)
    /// và UserManager.AddToRoleAsync để gán role qua AspNetUserRoles.
    ///
    /// Phải gọi SAU IdentityRoleSeed (cần AspNetRoles đã có "Admin", "Manager",
    /// "Staff", "Customer").
    /// </summary>
    public static class UserSeed
    {
        /// <summary>Mật khẩu mặc định cho tất cả demo user. Đổi ngay trong production.</summary>
        private const string DefaultPassword = "HDKTech@2024";

        public static async Task SeedAsync(
            UserManager<AppUser> userManager,
            HDKTechContext context)
        {
            // Admin
            await EnsureUserAsync(userManager,
                id: SeedConstants.AdminUserId,
                email: "admin@hdktech.vn",
                fullName: "Admin HDKTech",
                phone: "0900000001",
                roleName: AdminConstants.AdminRole);

            // Manager
            await EnsureUserAsync(userManager,
                id: SeedConstants.ManagerUserId,
                email: "manager@hdktech.vn",
                fullName: "Manager HDKTech",
                phone: "0900000002",
                roleName: AdminConstants.ManagerRole);

            // Customers
            await EnsureUserAsync(userManager, SeedConstants.User1Id,
                "nguyen.van.an@gmail.com", "Nguyễn Văn An", "0905123456", "Customer");
            await EnsureUserAsync(userManager, SeedConstants.User2Id,
                "tran.thi.bich@gmail.com", "Trần Thị Bích", "0936789012", "Customer");
            await EnsureUserAsync(userManager, SeedConstants.User3Id,
                "le.quoc.hung@gmail.com", "Lê Quốc Hùng", "0914567890", "Customer");
            await EnsureUserAsync(userManager, SeedConstants.User4Id,
                "pham.minh.duc@gmail.com", "Phạm Minh Đức", "0977234567", "Customer");
            await EnsureUserAsync(userManager, SeedConstants.User5Id,
                "hoang.thi.lan@gmail.com", "Hoàng Thị Lan", "0967890123", "Customer");

            // Default addresses cho customer tại Đà Nẵng
            await EnsureAddressAsync(context, SeedConstants.User1Id, "Nhà riêng",
                "Nguyễn Văn An", "0905123456", "123 Hoàng Diệu", "Phước Ninh", "Hải Châu", "Đà Nẵng");
            await EnsureAddressAsync(context, SeedConstants.User2Id, "Nhà riêng",
                "Trần Thị Bích", "0936789012", "45 Lê Lợi", "Thạch Thang", "Thanh Khê", "Đà Nẵng");
            await EnsureAddressAsync(context, SeedConstants.User3Id, "Nhà riêng",
                "Lê Quốc Hùng", "0914567890", "78 Nguyễn Tri Phương", "Hòa Khánh Bắc", "Liên Chiểu", "Đà Nẵng");
            await EnsureAddressAsync(context, SeedConstants.User4Id, "Nhà riêng",
                "Phạm Minh Đức", "0977234567", "12 Tôn Đức Thắng", "Hòa Hiệp Nam", "Liên Chiểu", "Đà Nẵng");
            await EnsureAddressAsync(context, SeedConstants.User5Id, "Nhà riêng",
                "Hoàng Thị Lan", "0967890123", "56 Điện Biên Phủ", "Chính Gián", "Thanh Khê", "Đà Nẵng");
        }

        // ── Helpers ────────────────────────────────────────────────

        private static async Task EnsureUserAsync(
            UserManager<AppUser> userManager,
            string id, string email, string fullName, string phone, string roleName)
        {
            var existing = await userManager.FindByIdAsync(id);
            if (existing != null)
            {
                await SyncRoleAsync(userManager, existing, roleName);
                return;
            }

            var user = new AppUser
            {
                Id                 = id,
                UserName           = email,
                NormalizedUserName = email.ToUpperInvariant(),
                Email              = email,
                NormalizedEmail    = email.ToUpperInvariant(),
                FullName           = fullName,
                PhoneNumber        = phone,
                EmailConfirmed     = true,
                IsActive           = true,
                CreatedAt          = DateTime.Now
            };

            // UserManager.CreateAsync tự hash password qua IPasswordHasher<AppUser>
            var result = await userManager.CreateAsync(user, DefaultPassword);
            if (!result.Succeeded)
            {
                var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                throw new InvalidOperationException(
                    $"UserSeed: không tạo được user {email}. Lỗi: {errors}");
            }

            var addToRole = await userManager.AddToRoleAsync(user, roleName);
            if (!addToRole.Succeeded)
            {
                var errors = string.Join("; ", addToRole.Errors.Select(e => e.Description));
                throw new InvalidOperationException(
                    $"UserSeed: không gán được role '{roleName}' cho user {email}. Lỗi: {errors}");
            }
        }

        /// <summary>
        /// Đồng bộ role duy nhất cho user: gỡ role thừa, thêm role đang cần.
        /// </summary>
        private static async Task SyncRoleAsync(
            UserManager<AppUser> userManager,
            AppUser user,
            string desiredRole)
        {
            var current = await userManager.GetRolesAsync(user);
            if (current.Count == 1 &&
                string.Equals(current[0], desiredRole, StringComparison.OrdinalIgnoreCase))
                return;

            if (current.Count > 0)
                await userManager.RemoveFromRolesAsync(user, current);

            await userManager.AddToRoleAsync(user, desiredRole);
        }

        private static async Task EnsureAddressAsync(
            HDKTechContext context,
            string userId, string label, string recipientName, string recipientPhone,
            string line, string ward, string district, string city)
        {
            var exists = await context.UserAddresses
                .AnyAsync(a => a.UserId == userId && a.IsDefault);
            if (exists) return;

            context.UserAddresses.Add(new UserAddress
            {
                UserId         = userId,
                Label          = label,
                RecipientName  = recipientName,
                RecipientPhone = recipientPhone,
                AddressLine    = line,
                Ward           = ward,
                District       = district,
                City           = city,
                IsDefault      = true,
                CreatedAt      = DateTime.Now
            });
            await context.SaveChangesAsync();
        }
    }
}
