using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HDKTech.Models
{
    [Table("NguoiDung")]
    public class NguoiDung : IdentityUser
    {
        [Required]
        [StringLength(100)]
        public string HoTen { get; set; }
        public DateTime NgayTao { get; set; } = DateTime.Now;
        public virtual ICollection<DonHang> DonHangs { get; set; }

    }
}
