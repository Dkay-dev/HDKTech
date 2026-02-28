using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HDKTech.Models
{
    [Table("SanPham")]
    public class SanPham
    {
        [Key]
        public int MaSanPham { get; set; }
        [Required(ErrorMessage = "Ten san pham khong duoc null")]
        [StringLength(100)]
        public string TenSanPham { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal Gia { get; set; }
        public int MaDanhMuc { get; set; }
        public int MaHangSX { get; set; }
        public int TrangThaiSanPham { get; set; }
        public DateTime ThoiGianTaoSP { get; set; } = DateTime.Now;

        [ForeignKey("MaDanhMuc")]
        public virtual DanhMuc DanhMuc { get; set; }

        [ForeignKey("MaHangSX")]

        public virtual HangSX HangSX { get; set; }
        public virtual ICollection<ChiTietDonHang> ChiTietDonHangs { get; set; }
        public virtual ICollection<ChiTietGioHang> ChiTietGioHangs { get; set; }


    }
}
