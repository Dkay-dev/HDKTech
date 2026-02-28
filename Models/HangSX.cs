using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HDKTech.Models
{
    [Table("HangSanXuat")]
    public class HangSX
    {
        [Key]
        public int MaHangSX { get; set; }
        [Required(ErrorMessage = "Ten Hang SX khong the null")]
        [StringLength(100)]
        public string TenHangSX { get; set; }
        [StringLength(500)]
        public string MoTa { get; set; }
        public virtual ICollection<SanPham> SanPhams { get; set; }
    }
}
