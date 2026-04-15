using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HDKTech.Areas.Admin.Models
{
    /// <summary>
    /// Banner Model - Manages advertising banners displayed on the website
    /// </summary>
    [Table("Banners")]
    public class Banner
    {
        /// <summary>
        /// Banner identifier (Primary Key)
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Banner title (required)
        /// </summary>
        [Required(ErrorMessage = "Banner title is required")]
        [StringLength(200, ErrorMessage = "Banner title must not exceed 200 characters")]
        public string Title { get; set; }

        /// <summary>
        /// Banner image URL (required)
        /// </summary>
        [StringLength(500, ErrorMessage = "Image URL must not exceed 500 characters")]
        public string? ImageUrl { get; set; }

        /// <summary>
        /// Short description of the banner
        /// </summary>
        [StringLength(500, ErrorMessage = "Description must not exceed 500 characters")]
        public string? Description { get; set; }

        /// <summary>
        /// Link navigated to when the banner is clicked (nullable)
        /// </summary>
        [StringLength(500, ErrorMessage = "Link URL must not exceed 500 characters")]
        public string? LinkUrl { get; set; }

        /// <summary>
        /// Display order (sorted ascending)
        /// </summary>
        public int DisplayOrder { get; set; } = 0;

        /// <summary>
        /// Active / inactive status
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Creation date
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// Last updated date
        /// </summary>
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Banner type: "Main" (Homepage), "Side" (Sidebar), "Bottom" (Footer)
        /// </summary>
        [StringLength(50)]
        public string BannerType { get; set; } = "Main";

        /// <summary>
        /// Display start date (nullable) - for scheduling feature
        /// </summary>
        public DateTime? StartDate { get; set; }

        /// <summary>
        /// Display end date (nullable) - for scheduling feature
        /// </summary>
        public DateTime? EndDate { get; set; }
    }
}
