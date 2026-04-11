using System;

namespace HDKTech.Models
{
    /// <summary>
    /// Theo dõi mỗi lần click vào banner
    /// </summary>
    public class BannerClickEvent
    {
        public int Id { get; set; }

        // Foreign key
        public int BannerId { get; set; }
        public Banner Banner { get; set; }

        // Click information
        public DateTime ClickedAt { get; set; } = DateTime.Now;
        public string UserIpAddress { get; set; }
        public string UserAgent { get; set; }
        public string Referer { get; set; }

        // Analytics
        public int? UserId { get; set; } // If user is logged in
        public string BannerName { get; set; } // Cached for analytics
        public string BannerType { get; set; } // Cached: Main, Side, Bottom
    }
}
