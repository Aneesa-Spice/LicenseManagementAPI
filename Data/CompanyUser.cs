using LicensingAPI.Models.Users;
using System.ComponentModel.DataAnnotations;

namespace LicensingAPI.Data
{
    public class CompanyUser
    {
        [Key]
        public int CompanyUserId { get; set; }
        public int CompanyId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string Role { get; set; } = "member";
        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

        public virtual Company Company { get; set; } = null!;
        public virtual ApplicationUser User { get; set; } = null!;
    }
}
