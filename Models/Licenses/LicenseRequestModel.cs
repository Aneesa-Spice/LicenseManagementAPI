using System.ComponentModel.DataAnnotations;

namespace LicensingAPI.Models.Licenses
{
    public class LicenseRequestModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string FullName { get; set; } = string.Empty;

        [Required]
        public string ProductCode { get; set; } = string.Empty;

        // A simple shared secret to verify the request comes from your trusted DLL
        public string? ClientSecret { get; set; }
    }
}
