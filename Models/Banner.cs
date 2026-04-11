using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HDKTech.Models
{
    /// <summary>
    /// Banner Model - Quản lý các banner quảng cáo trên website
    /// </summary>
    [Table("Banner")]
    public class Banner
    {
        /// <summary>
        /// Mã định danh banner (Primary Key)
        /// </summary>
        [Key]
        public int MaBanner { get; set; }

        /// <summary>
        /// Tên banner (bắt buộc)
        /// </summary>
        [Required(ErrorMessage = "Tên banner không được để trống")]
        [StringLength(200, ErrorMessage = "Tên banner không được vượt quá 200 ký tự")]
        public string TenBanner { get; set; }

        /// <summary>
        /// URL hình ảnh banner (bắt buộc)
        /// </summary>
        [Required(ErrorMessage = "URL hình ảnh không được để trống")]
        [StringLength(500, ErrorMessage = "URL hình ảnh không được vượt quá 500 ký tự")]
        public string ImageUrl { get; set; }

        /// <summary>
        /// Mô tả ngắn gọn về banner
        /// </summary>
        [StringLength(500, ErrorMessage = "Mô tả không được vượt quá 500 ký tự")]
        public string? MoTa { get; set; }

        /// <summary>
        /// Link khi click vào banner (nullable)
        /// </summary>
        [Url(ErrorMessage = "Link banner phải là URL hợp lệ")]
        [StringLength(500, ErrorMessage = "Link banner không được vượt quá 500 ký tự")]
        public string? LinkBanner { get; set; }

        /// <summary>
        /// Thứ tự hiển thị (sắp xếp từ nhỏ đến lớn)
        /// </summary>
        public int ThuTuHienThi { get; set; } = 0;

        /// <summary>
        /// Trạng thái kích hoạt/vô hiệu hóa
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Ngày tạo
        /// </summary>
        public DateTime NgayTao { get; set; } = DateTime.Now;

        /// <summary>
        /// Ngày cập nhật lần cuối
        /// </summary>
        public DateTime? NgayCapNhat { get; set; }

        /// <summary>
        /// Loại banner: "Main" (Trang chủ), "Side" (Sidebar), "Bottom" (Footer)
        /// </summary>
        [StringLength(50)]
        public string LoaiBanner { get; set; } = "Main";

        /// <summary>
        /// Ngày bắt đầu hiển thị (nullable) - Cho tính năng scheduling
        /// </summary>
        public DateTime? NgayBatDau { get; set; }

        /// <summary>
        /// Ngày kết thúc hiển thị (nullable) - Cho tính năng scheduling
        /// </summary>
        public DateTime? NgayKetThuc { get; set; }
    }
}
