using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HDKTech.Models
{
    [Table("KhoHang")]
    public class KhoHang
    {

        [Key]
        public int MaSanPham { get; set; }

        public int SoLuong { get; set; }
        public DateTime NgayCapNhat { get; set; } = DateTime.Now;
        [ForeignKey("MaSanPham")]
        public virtual SanPham SanPham { get; set; }
    }
}
