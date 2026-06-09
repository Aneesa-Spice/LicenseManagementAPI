using System.ComponentModel.DataAnnotations;

namespace LicensingAPI.Data
{
    public class TrialSession
    {
        [Key]
        public int Id { get; set; }
        public string MachineFingerprint { get; set; } = null!;
        public int ProductId { get; set; }
        public string TrialLicenseKey { get; set; } = null!;
        public string KeygenLicenseId { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool Converted { get; set; } = false;

        public Product Product { get; set; } = null!;
        public virtual ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
