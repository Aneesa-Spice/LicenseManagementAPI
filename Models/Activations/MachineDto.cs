using System;

namespace LicensingAPI.Models.Activations
{
    public class MachineDto
    {
        public string Id { get; set; } = string.Empty;
        public string? Name { get; set; }
        public string Fingerprint { get; set; } = string.Empty;
        public int Cores { get; set; }
        public string? Ip { get; set; }
        public string? Hostname { get; set; }
        public string? Platform { get; set; }
        public string? Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string? LicenseId { get; set; }
    }
}
