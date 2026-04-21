using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HDKTech.Models
{
    /// <summary>
    /// User của hệ thống HDKTech. Kế thừa IdentityUser để tận dụng đầy đủ hạ tầng
    /// login / password hashing / email confirm / lockout của ASP.NET Identity.
    ///
    /// Phân quyền dùng duy nhất ASP.NET Identity:
    ///   - Role     → AspNetRoles / AspNetUserRoles
    ///   - Quyền    → AspNetRoleClaims (Type="permission", Value="Product.Read"...)
    ///
    /// KHÔNG còn cột RoleId/Role custom. Xem Data/IdentityRoleSeed.cs + PermissionHandler.cs.
    /// </summary>
    [Table("Users")]
    public class AppUser : IdentityUser
    {
        [Required]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [StringLength(300)]
        public string? AvatarUrl { get; set; }

        public DateTime? DateOfBirth { get; set; }

        /// <summary>0 = Unknown, 1 = Male, 2 = Female, 3 = Other.</summary>
        public int Gender { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? LastLoginAt { get; set; }

        // ── Navigation collections ────────────────────────────────
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
        public virtual ICollection<UserAddress> Addresses { get; set; } = new List<UserAddress>();
    }
}
