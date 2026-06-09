namespace LicensingAPI.Models.Users;

public class CreateUserRequest
{
    public required string FullName { get; set; }
    public required string Email { get; set; }
    public required string Password { get; set; }
   // public string? SerialKey { get; set; }
    public string? Role { get; set; }
}

public class UpdateUserRequest
{
    public required string FullName { get; set; }
    public required string Email { get; set; }
   // public string? SerialKey { get; set; }
}
