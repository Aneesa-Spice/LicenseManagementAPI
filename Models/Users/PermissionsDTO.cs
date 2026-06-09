using System.Collections.Generic;

namespace LicensingAPI.Models.Users
{
    public class RolePermissionsDTO
    {
        public string RoleId { get; set; } = string.Empty;
        public List<string> Permissions { get; set; } = new List<string>();
    }

    public class UpdateRolePermissionsRequest
    {
        public string RoleId { get; set; } = string.Empty;
        public List<string> Permissions { get; set; } = new List<string>();
    }
}
