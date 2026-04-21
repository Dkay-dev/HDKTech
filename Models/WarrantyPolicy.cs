using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HDKTech.Models
{
    /// <summary>
    /// Chính sách bảo hành — được tái sử dụng cho nhiều Product.
    /// VD: "Bảo hành chính hãng 24T", "Bảo hành mở rộng 36T", "Bảo hành pin 12T".
    ///
    /// Một Product tham chiếu tới 1 WarrantyPolicy qua FK nullable
    /// (Product.WarrantyPolicyId). ProductVariant có thể override chính sách
    /// của Product (VariantWarrantyPolicyId) cho các dòng cao cấp.
    /// </summary>
    [Table("WarrantyPolicies")]
    public class WarrantyPolicy
    {
        [Key]
        public int Id { get; set; }

        [Required, StringLength(150)]
        public string Name { get; set; } = string.Empty;

        [StringLength(50)]
        public string? Code { get; set; }            // "HDK-STD-24", "HDK-VIP-36"

        [Range(0, 120)]
        public int DurationMonths { get; set; }      // 0 = không bảo hành

        /// <summary>Nơi bảo hành: "Tại hãng", "Tại HDKTech", "Hỗ trợ đổi mới"...</summary>
        [StringLength(100)]
        public string? Coverage { get; set; }

        public string? Terms { get; set; }           // điều khoản chi tiết
        public string? Exclusions { get; set; }      // các trường hợp không bảo hành

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }

        public virtual ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
