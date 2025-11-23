using Microsoft.EntityFrameworkCore;
using MyHackathonAPI.Data;
using MyHackathonAPI.Models;

namespace MyHackathonAPI.Endpoints;

public static class UserEndpoints {
    public static void MapUserEndpoints(this IEndpointRouteBuilder app) {
        
        app.MapGet("/users", async (AppDb db) => 
            await db.Users.ToListAsync());

        app.MapGet("/users/{id}", async (int id, AppDb db) => 
            await db.Users.FindAsync(id)
                is User user
                    ? Results.Ok(user)
                    : Results.NotFound());
        
        app.MapPost("/users", async (User input, AppDb db) => {
            var user = await User.CreateAsync(db, input.Name, input.Email, input.Password, input.TenantId, input.Role);
            db.Users.Add(user);
            await db.SaveChangesAsync();
            return Results.Created($"/users/{user.Id}", user);
        });
        
        app.MapPut("/users/{id}", async (int id, User inputUser, AppDb db) => {
            var user = await db.Users.FindAsync(id);
            if (user is null) return Results.NotFound();

            // Update fields
            user.Name = inputUser.Name;
            user.Email = inputUser.Email;
            user.PhoneNumber = inputUser.PhoneNumber;
            user.Age = inputUser.Age;

            await db.SaveChangesAsync();
            return Results.NoContent();
        });
        
        app.MapDelete("/users/{id}", async (int id, AppDb db) => {
            if (await db.Users.FindAsync(id) is User user) {
                db.Users.Remove(user);
                await db.SaveChangesAsync();
                return Results.Ok(user);
            }
            return Results.NotFound();
        });
    }
}
