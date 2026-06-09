using System.ComponentModel.DataAnnotations;

namespace LicensingAPI.Data
{
    public class Policy
    {
        [Key]
        public int PolicyId { get; set; }
        public string ProviderPolicyId { get; set; } = null!;
        public string? PolicyName { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public string? DurationValue { get; set; } 
        public long? Duration { get; set; }
        public string? Scheme { get; set; }
        //// Non-floating policies are considered "node-locked"
        public bool Floating { get; set; } = false;

        public int? MaxMachines { get; set; }

        public int? MaxProcesses { get; set; }

        public int? MaxCPUCores { get; set; }

        public int? MaxMemory { get; set; }
        public int? MaxDisc { get; set; }
        public int? MaxUsers { get; set; }
        public int? MaxUses { get; set; }
        // Foreign key to Product
        public int? ProductId { get; set; }
        public string? ProviderProductId { get; set; }
        public virtual Product? Product { get; set; } 
        // Navigation property back to Licenses
        public virtual ICollection<License> Licenses { get; set; } = new List<License>();
        //public virtual ICollection<UserLicense> UserLicenses { get; set; } = new List<UserLicense>();

    }
}
