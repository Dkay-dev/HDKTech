using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HDKTech.Models
{
    [Table("ChiTietGioHang")]
   
        public class ChiTietGioHang
        {
            [Key]
            public int MaChiTietGioHang { get; set; }
            public int MaGioHang { get; set; }
            public int MaSanPham { get; set; }
            public int SoLuong { get; set; }
            [ForeignKey("MaGioHang")]
            public virtual GioHang GioHang { get; set; }
            [ForeignKey("MaSanPham")]
            public virtual SanPham SanPham { get; set; }
        }
    }
