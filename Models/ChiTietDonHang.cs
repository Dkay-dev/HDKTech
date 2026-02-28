
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HDKTech.Models
{
    [Table("ChiTietDonHang")]
    public class ChiTietDonHang
    {
        [Key]
        public int MaChiTietDonHang { get; set; }
        public int MaDonHang { get; set; }

        public int MaSanPham { get; set; }
        public int SoLuong { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal GiaBanLucMua { get; set; }
        [ForeignKey("MaDonHang")]
        public virtual DonHang DonHang { get; set; }

        [ForeignKey("MaSanPham")]
        public virtual SanPham SanPham { get; set; }
    }
}
