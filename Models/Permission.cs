using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HDKTech.Models
{
    [Table("Permissions")]
    public class Permission
    {
        [Key]
        public int PermissionId { get; set; }

        [Required(ErrorMessage = "Tên quyền không được để trống")]
        [StringLength(100)]
        public string PermissionName { get; set; }

        [StringLength(200)]
        public string Description { get; set; }

        [StringLength(50)]
        public string Module { get; set; } // Dashboard, Product, Category, Order, etc.

        [StringLength(50)]
        public string Action { get; set; } // Create, Read, Update, Delete

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public virtual ICollection<RolePermission> RolePermissions { get; set; }
    }
}
