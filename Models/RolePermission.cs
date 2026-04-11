using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HDKTech.Models
{
    [Table("RolePermissions")]
    public class RolePermission
    {
        [Key]
        public int RolePermissionId { get; set; }

        [ForeignKey("Role")]
        public int RoleId { get; set; }

        [ForeignKey("Permission")]
        public int PermissionId { get; set; }

        public DateTime AssignedAt { get; set; } = DateTime.Now;

        public virtual Role Role { get; set; }
        public virtual Permission Permission { get; set; }
    }
}
