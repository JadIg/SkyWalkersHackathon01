namespace MyHackathonAPI.Models;

public record LoginRequest(string Email, string Password);
public record RegisterRequest(string Name, string Email, string Password);
