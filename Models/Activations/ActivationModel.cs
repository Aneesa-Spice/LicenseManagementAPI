using System.ComponentModel.DataAnnotations;

namespace LicensingAPI.Models.Activations
{
    public class ActivateRequest
    {
        [Required]
        public string LicenseKey { get; set; } = string.Empty;

        [Required]
        public string MachineFingerprint { get; set; } = string.Empty;

        public string? MachineName { get; set; }
        public string? MachinePlatform { get; set; }
    }

    public class HeartbeatRequest
    {
        [Required]
        public string MachineId { get; set; } = string.Empty;
    }

    public class DeactivateRequest
    {
        [Required]
        public string MachineId { get; set; } = string.Empty;
    }

    public class ValidateLicenseRequest
    {
        [Required]
        public string LicenseKey { get; set; } = string.Empty;

        public string? MachineFingerprint { get; set; }
    }
}
