using System;
using System.ComponentModel.DataAnnotations;

namespace LicensingAPI.Data
{
    public class Product
    {
        [Key]
        public int ProductId { get; set; }
        [Required]
        public string ProviderProductId { get; set; } = null!;

        [Required]
        [MaxLength(200)]
        public string ProductName { get; set; } = string.Empty;
     
        [Required]
        [MaxLength(100)]
        public string Code { get; set; } = string.Empty;
        public string? Url { get; set; }
        public int Version { get; set; } = 0;

        [MaxLength(500)]
        public string? Description { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; } = DateTime.UtcNow;
        public string? Remarks { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;

        // Navigation property: one product can have many licenses
        public virtual ICollection<License> Licenses { get; set; } = new List<License>();  
        public virtual ICollection<Policy> Policies { get; set; } = new List<Policy>();
        //public virtual ICollection<UserLicense> UserLicenses { get; set; } = new List<UserLicense>();
    }
}
