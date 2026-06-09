namespace LicensingAPI.Models.Licenses
{
    public class CreateLicenseRequest
    {
        public int Company { get; set; }
        public string Policy { get; set; } = string.Empty;
        public string Product { get; set; } = string.Empty;
        public DateTime? Expiry { get; set; }
        public int MaxUsers { get; set; }
        public int MaxMachines { get; set; }
        public string? Name { get; set; } = string.Empty;
        //public string? UserEmail { get; set; }
       // public string? accountId { get; set; }
        //public int PolicyId { get; set; }
        //public int ProductId { get; set; }
        //public DateTime? Expiry { get; set; }
      //  public bool IsTrial { get; set; } = true;
    }

    public class UpdateLicenseRequest
    {
        public string? LicenseKey { get; set; }
        public DateTime? Expiry { get; set; }
        public int MaxUsers { get; set; }
        public int MaxMachines { get; set; }
    }

    public class DeleteLicenseRequest
    {
        public string? LicenseId { get; set; }
        public string AccountId { get; set; }
    }

    public class MapUserLicenseRequest
    {
        public string UserId { get; set; } = string.Empty;
        public int LicenseId { get; set; }
    }

    public class UserLicenseDTO
    {
        public int RecordId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string? UserEmail { get; set; }
        public int LicenseId { get; set; }
        public string? LicenseKey { get; set; }
        public string? ProductName { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
