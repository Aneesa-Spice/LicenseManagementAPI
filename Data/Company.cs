using LicensingAPI.Models.Users;
using System.ComponentModel.DataAnnotations;

namespace LicensingAPI.Data
{
    public class Company
    {
        [Key]
        public int CompanyId { get; set; }

        [Required]
        [MaxLength(200)]
        public string CompanyName { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Address { get; set; }

        [MaxLength(100)]
        public string ContactEmail { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? ContactPhone { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public int LicenseQuantity { get; set; } = 0;
        public bool Status { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;

        // Navigation property
        public virtual ICollection<License> Licenses { get; set; } = new List<License>();
        public virtual ICollection<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();
        //public virtual ICollection<CompanyUser> CompanyUsers { get; set; } = new List<CompanyUser>();
        public virtual ICollection<UserLicense> UserLicenses { get; set; } = new List<UserLicense>();
    }
}
