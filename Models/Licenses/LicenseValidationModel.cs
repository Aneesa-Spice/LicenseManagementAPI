using System;

namespace LicensingAPI.Models.Licenses
{
    public class LicenseValidationRequest
    {
        public string Email { get; set; } = string.Empty;
        public string LicenseKey { get; set; } = string.Empty;
    }

    public class LicenseValidationResult
    {
        public bool IsValid { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime? ExpiryDate { get; set; }
        public bool IsTrial { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
