using HDKTech.Models;
using Microsoft.AspNetCore.Identity;

namespace HDKTech.Data.Seeds
{
    public static class UserSeed
    {
        public static async Task SeedAsync(
            UserManager<AppUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            // Tạo roles Identity
            string[] roles = { "Admin", "Manager", "User" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));
            }

            // Admin
            await EnsureUser(userManager,
                id: SeedConstants.AdminUserId,
                email: "admin@hdktech.vn",
                fullName: "Admin HDKTech",
                role: "Admin");

            // Demo customers tại Đà Nẵng
            await EnsureUser(userManager,
                id: SeedConstants.User1Id,
                email: "nguyen.van.an@gmail.com",
                fullName: "Nguyễn Văn An",
                phone: "0905123456",
                role: "User");

            await EnsureUser(userManager,
                id: SeedConstants.User2Id,
                email: "tran.thi.bich@gmail.com",
                fullName: "Trần Thị Bích",
                phone: "0936789012",
                role: "User");

            await EnsureUser(userManager,
                id: SeedConstants.User3Id,
                email: "le.quoc.hung@gmail.com",
                fullName: "Lê Quốc Hùng",
                phone: "0914567890",
                role: "User");

            await EnsureUser(userManager,
                id: SeedConstants.User4Id,
                email: "pham.minh.duc@gmail.com",
                fullName: "Phạm Minh Đức",
                phone: "0977234567",
                role: "User");

            await EnsureUser(userManager,
                id: SeedConstants.User5Id,
                email: "hoang.thi.lan@gmail.com",
                fullName: "Hoàng Thị Lan",
                phone: "0967890123",
                role: "User");
        }

        private static async Task EnsureUser(
            UserManager<AppUser> userManager,
            string id, string email, string fullName,
            string phone = null, string role = "User")
        {
            if (await userManager.FindByIdAsync(id) != null) return;

            var user = new AppUser
            {
                Id = id,
                UserName = email,
                Email = email,
                FullName = fullName,
                PhoneNumber = phone,
                EmailConfirmed = true,
                CreatedAt = DateTime.Now
            };

            var result = await userManager.CreateAsync(user, "HDKTech@2024");
            if (result.Succeeded)
                await userManager.AddToRoleAsync(user, role);
        }
    }
}