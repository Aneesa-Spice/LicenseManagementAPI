using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LicensingAPI.Data
{
    public class License
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int LicenseId { get; set; }
        [Required]
        public string ProviderLicenseId { get; set; } = null!;

        [Required]
        public string LicenseKey { get; set; } = null!;
        [MaxLength(20)]
        public string Status { get; set; } = "ACTIVE"; // ACTIVE, EXPIRED, SUSPENDED, etc.
        public string? Name { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string? Owner { get; set; }
        public int? MaxMachines { get; set; }
        public int? Uses { get; set; }      // current usage count
        public int? MaxUsers { get; set; }   // license-level override (optional)

        public bool IsTrial { get; set; } = false;
        public bool IsValid { get; set; } = true;
        public bool IsDeleted { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        //// Foreign key to Product
        public int? ProductId { get; set; }
        public string? ProviderProductId { get; set; }   // nullable string
        public virtual Product? Product { get; set; } 

        //// Foreign key to Policy (optional)
        public int? PolicyId { get; set; }
        public string? ProviderPolicyId { get; set; }
        public virtual Policy? Policy { get; set; }

        //// Foreign key to Company (optional)
        public int? CompanyId { get; set; }
        public virtual Company? Company { get; set; }

        // Navigation properties
        public virtual ICollection<UserLicense> UserLicenses { get; set; } = new List<UserLicense>();
        public virtual ICollection<Activation> Activations { get; set; } = new List<Activation>();
    }
}
