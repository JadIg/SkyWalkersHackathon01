namespace MyHackathonAPI.Models;

public class User {
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
    public string PhoneNumber { get; set; } = "";
    public int Age { get; set; }
    public string Role { get; set; } = "Editor"; // "Admin" or "Editor"
    public int TenantId { get; set; } // The "Organization" ID
}
