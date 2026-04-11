using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HDKTech.Models
{
    [Table("KhuyenMai")]
    public class KhuyenMai
    {
        [Key]
        public int MaKhuyenMai { get; set; }

        [Required(ErrorMessage = "Tên chiến dịch không được để trống")]
        [StringLength(200)]
        public string TenChienDich { get; set; }

        [StringLength(500)]
        public string? MoTa { get; set; }

        [StringLength(100)]
        public string? DanhMucAp { get; set; } // Laptop & Accessories, All Categories, Tier: Platinum+

        public DateTime NgayBatDau { get; set; }

        public DateTime NgayKetThuc { get; set; }

        [StringLength(20)]
        public string LoaiKhuyenMai { get; set; } = "Percentage"; // Percentage, FixedAmount, FreeShip

        [Column(TypeName = "decimal(18,2)")]
        public decimal GiaTri { get; set; } // 20 (for 20% OFF), 1000000 (for fixed amount)

        [StringLength(50)]
        public string? MaKhuyenMai_Code { get; set; } // BACK2024, BLACK50, etc

        public int SoLuongSuDung { get; set; } = 0;

        public int? SoLuongToiDa { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime NgayTao { get; set; } = DateTime.Now;

        public DateTime? NgayCapNhat { get; set; }

        [StringLength(50)]
        public string? TrangThai { get; set; } // Draft, Scheduled, Running, Ended, Archived
    }
}
