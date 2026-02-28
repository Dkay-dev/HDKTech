using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HDKTech.Models
{
    [Table("DonHang")]
    public class DonHang
    {
        [Key]
        public int MaDonHang { get; set; }
        [Required]
        public string MaDonHangChuoi { get; set; }
        public string MaNguoiDung { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TongTien { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal PhiVanChuyen { get; set; }

        public string TenNguoiNhan { get; set; }

        public string SoDienThoaiNhan { get; set; }

        public string DiaChiGiaoHang { get; set; }

        public int TrangThaiDonHang { get; set; }
        public DateTime NgayDatHang { get; set; } = DateTime.Now;


        [ForeignKey("MaNguoiDung")]
        public virtual NguoiDung NguoiDung { get; set; }


        public virtual ICollection<ChiTietDonHang> ChiTietDonHangs { get; set; }

        public virtual HoaDon HoaDon { get; set; }



    }
}
