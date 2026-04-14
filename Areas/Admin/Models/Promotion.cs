using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HDKTech.Areas.Admin.Models
{
    [Table("Promotion")]
    public class Promotion
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên chiến dịch không được để trống.")]
        [StringLength(200)]
        public string CampaignName { get; set; }

        // ── Computed aliases ──────────────────────────────────────────
        /// <summary>Alias cho CampaignName — dùng thống nhất trên toàn hệ thống.</summary>
        [NotMapped]
        public string Name
        {
            get => CampaignName;
            set => CampaignName = value;
        }

        /// <summary>Phần trăm giảm giá — chỉ có giá trị khi PromotionType == "Percentage".</summary>
        [NotMapped]
        public decimal DiscountPercent => PromotionType == "Percentage" ? Value : 0;

        /// <summary>True khi khuyến mãi đang chạy thực sự (IsActive + trong khoảng thời gian).</summary>
        [NotMapped]
        public bool IsCurrentlyActive => IsActive && DateTime.Now >= StartDate && DateTime.Now <= EndDate;

        [StringLength(500)]
        public string? Description { get; set; }

        [StringLength(100)]
        public string? ApplicableCategory { get; set; } // Laptop & Accessories, All Categories, Tier: Platinum+

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        [StringLength(20)]
        public string PromotionType { get; set; } = "Percentage"; // Percentage, FixedAmount, FreeShip

        [Column(TypeName = "decimal(18,2)")]
        public decimal Value { get; set; } // 20 (for 20% OFF), 1000000 (for fixed amount)

        [StringLength(50)]
        public string? PromoCode { get; set; } // BACK2024, BLACK50, etc

        public int UsageCount { get; set; } = 0;

        public int? MaxUsageCount { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }

        [StringLength(50)]
        public string? Status { get; set; } // Draft, Scheduled, Running, Ended, Archived
    }
}
