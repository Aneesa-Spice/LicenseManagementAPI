using LicensingAPI.Models.Users;
using System.ComponentModel.DataAnnotations;

namespace LicensingAPI.Data
{
    public class AuditLog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string EntityName { get; set; } = string.Empty; // e.g., "License", "Product"

        public string? EntityId { get; set; } // The ID of the affected entity

        [Required]
        public string Action { get; set; } = string.Empty; // e.g., "CREATE", "UPDATE", "DELETE", "ACTIVATE"

        public string? Changes { get; set; } // JSON representation of changes or action details

        [Required]
        public string PerformedBy { get; set; } = string.Empty; // User email or system

        public string? UserId { get; set; } // The ID of the user who performed the action

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [MaxLength(50)]
        public string? IpAddress { get; set; }

        // Navigation property
        public virtual ApplicationUser? User { get; set; }
    }
}
