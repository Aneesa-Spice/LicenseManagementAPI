using LicensingAPI.Models.Users;
using System.ComponentModel.DataAnnotations;

namespace LicensingAPI.Data
{
    public class LicenseEvent
    {
        [Key]
        public int LicenseEventId { get; set; }
        public int? UserLicenseId { get; set; }
        public string? UserId { get; set; }
        public string EventType { get; set; } = null!;
        public string? MachineFingerprint { get; set; }
        public string? IpAddress { get; set; }
        public string? AppVersion { get; set; }
        public string? ProductCode { get; set; }
        public string? Metadata { get; set; }               // JSON string
        public DateTime CreatedAt { get; set; }

        public virtual UserLicense? UserLicense { get; set; }
        public virtual ApplicationUser User { get; set; } = null!;
    }
}
