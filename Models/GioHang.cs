using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Drawing;

namespace HDKTech.Models
{
    [Table("GioHang")]
    public class GioHang
    {
        [Key]
        public int MaGioHang { get; set; }
        public string MaNguoiDung { get; set; }
        [ForeignKey("MaNguoiDung")]
        public virtual NguoiDung NguoiDung { get; set; }
        public virtual ICollection<ChiTietGioHang> ChiTietGioHangs { get; set; }
    }
}
