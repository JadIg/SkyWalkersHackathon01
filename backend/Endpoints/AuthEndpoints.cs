using Microsoft.EntityFrameworkCore;
using MyHackathonAPI.Data;
using MyHackathonAPI.Models;

namespace MyHackathonAPI.Endpoints;

public static class AuthEndpoints {
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app) {
        
        // POST /auth/login
        app.MapPost("/auth/login", async (LoginRequest req, AppDb db) => {
            var user = await db.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == req.Email.ToLower());

            if (user is null || user.Password != req.Password) 
                return Results.Unauthorized();

            // Fetch the Tenant Name for the UI
            var tenant = await db.Tenants.FindAsync(user.TenantId);

            return Results.Ok(new {
                UserId = user.Id,
                Name = user.Name,
                Role = user.Role,
                TenantId = user.TenantId,
                TenantName = tenant?.Name ?? "Unknown"
            });
        });

        // POST /auth/register
        app.MapPost("/auth/register", async (RegisterRequest req, AppDb db) => {
            // 1. Check if email exists
            if (await db.Users.AnyAsync(u => u.Email.ToLower() == req.Email.ToLower()))
                return Results.Conflict("Email already registered.");

            // 2. Create a unique tenant for the new user
            var tenant = new Tenant { Name = $"{req.Name}'s Organization" };
            db.Tenants.Add(tenant);
            await db.SaveChangesAsync();

            // 3. Create User
            var user = new User {
                Name = req.Name,
                Email = req.Email,
                Password = req.Password,
                TenantId = tenant.Id,
                Role = "Editor" // Default
            };

            db.Users.Add(user);
            await db.SaveChangesAsync();

            // 4. Auto-Login
            return Results.Ok(new {
                UserId = user.Id,
                Name = user.Name,
                Role = user.Role,
                TenantId = user.TenantId,
                TenantName = tenant.Name
            });
        });
    }
}
