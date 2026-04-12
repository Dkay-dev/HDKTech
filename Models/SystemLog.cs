using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HDKTech.Models
{
    [Table("SystemLogs")]
    public class SystemLog
    {
        [Key]
        public int Id { get; set; }

        // Mapped column in DB is 'Timestamp' - expose as CreatedAt for existing code
        [Column("Timestamp")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Backwards-compatible alias
        [NotMapped]
        public DateTime Timestamp { get => CreatedAt; set => CreatedAt = value; }

        [Column("Username")]
        [StringLength(255)]
        public string? Username { get; set; }

        // In DB the column is 'ActionType' - expose as Action for legacy code
        [Column("ActionType")]
        [StringLength(50)]
        public string? Action { get; set; }

        // Alias for newer code
        [NotMapped]
        public string? ActionType { get => Action; set => Action = value; }

        // In DB the column is 'Module' - expose as LogLevel for legacy code
        [Column("Module")]
        [StringLength(100)]
        public string? LogLevel { get; set; }

        // Alias for newer code
        [NotMapped]
        public string? Module { get => LogLevel; set => LogLevel = value; }

        public string? Description { get; set; }

        public string? EntityId { get; set; }

        [StringLength(500)]
        public string? EntityName { get; set; }

        public string? OldValue { get; set; }
        public string? NewValue { get; set; }

        [StringLength(50)]
        public string? IpAddress { get; set; }

        public string? UserAgent { get; set; }

        [StringLength(50)]
        public string? Status { get; set; }

        public string? ErrorMessage { get; set; }

        [StringLength(100)]
        public string? UserRole { get; set; }

        public string? UserId { get; set; }
    }
}
