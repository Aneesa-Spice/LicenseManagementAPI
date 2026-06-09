using System.ComponentModel.DataAnnotations;

namespace LicensingAPI.Models.Companies
{
    public class CreateCompanyRequest
    {
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Address { get; set; }

        [MaxLength(100)]
        public string? ContactEmail { get; set; }

        [MaxLength(50)]
        public string? ContactPhone { get; set; }

        public int LicenseQuantity { get; set; } = 0;

        //[MaxLength(50)]
        public bool Status { get; set; } = true;
        [Required]
        public string UserId { get; set; } = string.Empty;
        public string? UserEmail { get; set; }
    }

    public class UpdateCompanyRequest
    {
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Address { get; set; }

        [MaxLength(100)]
        public string? ContactEmail { get; set; }

        [MaxLength(50)]
        public string? ContactPhone { get; set; }

        public int LicenseQuantity { get; set; } = 0;

        [MaxLength(50)]
        public string? Status { get; set; }
        [Required]
        public string UserId { get; set; } = string.Empty;
        public string? UserEmail { get; set; }
    }

    public class CompanyDTO
    {
        public int CompanyId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? ContactEmail { get; set; }
        public string? ContactPhone { get; set; }
        public int LicenseQuantity { get; set; }
        public bool Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string? UserEmail { get; set; } = string.Empty;
    }

    public class AddCompanyUserRequest
    {
        [Required]
        public string UserEmail { get; set; } = string.Empty;
        public int CompanyId { get; set; }
        public string Role { get; set; } = "member";
    }

    public class CompanyUserDTO
    {
        public int UserLicenseId { get; set; }
        public int CompanyId { get; set; }
        public int LicenseId { get; set; } 
        //public string UserId { get; set; } = string.Empty;
        //public string? FullName { get; set; }
        public string? Email { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
