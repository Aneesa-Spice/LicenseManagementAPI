using System.ComponentModel.DataAnnotations;

namespace LicensingAPI.Data
{
    public class AppSetting
    {
        [Key]
        public int Id { get; set; }
        [Required]
        [MaxLength(100)]
        public string Key { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Value { get; set; } = string.Empty;
        public string? ProviderName { get; set; }
        public string? Description { get; set; }
        public bool IsEncrypted { get; set; } = false;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
