using LicensingAPI.Models.Users;
using System.ComponentModel.DataAnnotations;

namespace LicensingAPI.Data
{
    public class Activation
    {
        [Key]
        public int ActivationId { get; set; }

        public int UserLicenseId { get; set; }
        public int LicenseId { get; set; }

        // ✅ just email, no FK
        public string UserEmail { get; set; } = string.Empty;

        public string ProviderMachineId { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? MachineFingerprint { get; set; }

        [MaxLength(200)]
        public string? MachineName { get; set; }

        public string Status { get; set; } = "Active";
        public bool IsValid { get; set; } = true;
        public bool IsDeleted { get; set; } = false;

        public DateTime ActivatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ExpiryDate { get; set; }
        public DateTime? RevokedAt { get; set; }
        public DateTime? LastSeenAt { get; set; }

        // Navigation
        public virtual UserLicense UserLicense { get; set; } = null!;
        public virtual License License { get; set; } = null!;
    }
}
