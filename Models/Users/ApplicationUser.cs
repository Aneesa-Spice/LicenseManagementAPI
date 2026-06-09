using Microsoft.AspNetCore.Identity;
using LicensingAPI.Data;

namespace LicensingAPI.Models.Users
{
    public class ApplicationUser : IdentityUser
    {
        //public int? CompanyId { get; set; }
        public Company? Company { get; set; } = null!;
        public string FullName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? Remarks { get; set; }
        //public string? SerialKey { get; set; }
        //public string? KeygenUserId { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsDeleted { get; set; } = false;
        // Navigation properties
        //public virtual ICollection<UserLicense> UserLicenses { get; set; } = new List<UserLicense>();
        public virtual ICollection<Activation> Activations { get; set; } = new List<Activation>();
        //public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
       // public virtual ICollection<CompanyUser> CompanyUsers { get; set; } = new List<CompanyUser>();
        //public virtual ICollection<LicenseEvent> LicenseEvents { get; set; } = new List<LicenseEvent>();
    }
}
