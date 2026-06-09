using LicensingAPI.Models.Users;
using System.ComponentModel.DataAnnotations;

namespace LicensingAPI.Data
{
    public class UserLicense
    {
        [Key]
        public int UserLicenseId { get; set; }
        public int LicenseId { get; set; }
        public string ProviderLicenseId { get; set; } = string.Empty;
        public int CompanyId { get; set; }

        // ✅ no UserId FK — just email for license identification
        public string UserEmail { get; set; } = string.Empty;
        public bool IsDeleted { get; set; } = false;
        public string Status { get; set; } = "Active";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public virtual License License { get; set; } = null!;
        public virtual Company Company { get; set; } = null!;
        public virtual ICollection<Activation> Activations { get; set; } = new List<Activation>();

    }
}
