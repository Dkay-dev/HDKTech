using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HDKTech.Models
{
    /// <summary>
    /// Sổ địa chỉ của khách hàng — một user có thể lưu nhiều địa chỉ nhận hàng
    /// (nhà riêng, công ty, văn phòng phụ …) và chọn 1 địa chỉ mặc định.
    ///
    /// Khi checkout, dữ liệu từ UserAddress được COPY (snapshot) sang Order
    /// để lịch sử đơn không bị ảnh hưởng nếu sau này user sửa / xoá địa chỉ.
    /// </summary>
    [Table("UserAddresses")]
    public class UserAddress
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(450)]              // = IdentityUser.Id
        public string UserId { get; set; } = string.Empty;

        [StringLength(50)]
        public string? Label { get; set; }     // "Nhà riêng", "Công ty"

        [Required, StringLength(100)]
        public string RecipientName { get; set; } = string.Empty;

        [Required, StringLength(20)]
        public string RecipientPhone { get; set; } = string.Empty;

        [Required, StringLength(300)]
        public string AddressLine { get; set; } = string.Empty;  // số nhà, đường

        [Required, StringLength(100)]
        public string Ward { get; set; } = string.Empty;

        [Required, StringLength(100)]
        public string District { get; set; } = string.Empty;

        [Required, StringLength(100)]
        public string City { get; set; } = string.Empty;

        [StringLength(20)]
        public string? PostalCode { get; set; }

        public bool IsDefault { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }

        [ForeignKey(nameof(UserId))]
        public virtual AppUser? User { get; set; }

        // ── Helpers ──────────────────────────────────────────────
        /// <summary>Địa chỉ gộp để hiển thị 1 dòng.</summary>
        [NotMapped]
        public string FullAddress => $"{AddressLine}, {Ward}, {District}, {City}";
    }
}
