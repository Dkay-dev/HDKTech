using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HDKTech.Areas.Admin.Models
{
    [Table("Promotion")]
    public class Promotion
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Campaign name is required")]
        [StringLength(200)]
        public string CampaignName { get; set; }

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
