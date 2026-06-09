namespace LicensingAPI.Models.Auth
{
    public class UserInfo
    {
        public string Id { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public bool IsActive { get; set; }
        public string Role { get; set; }
        public string? KeygenId { get; set; } = string.Empty;
    }
}
