namespace LicensingAPI.Models.Users
{
    public class RoleDTO
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    public class RoleRequest
    {
        public string Name { get; set; } = string.Empty;
    }
}
