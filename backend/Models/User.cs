using Microsoft.EntityFrameworkCore;
using MyHackathonAPI.Data;

namespace MyHackathonAPI.Models;

public class User {
    public int Id { get; private set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
    public string PhoneNumber { get; set; } = "";
    public int Age { get; set; }
    public string Role { get; set; } = "Editor"; // "Admin" or "Editor"
    public int TenantId { get; set; } // The "Organization" ID

    public static Task<User> CreateAsync(AppDb db, string name, string email, string password, int tenantId, string role = "Editor") =>
        Task.FromResult(new User {
            Name = name,
            Email = email,
            Password = password,
            TenantId = tenantId,
            Role = role
        });
}
