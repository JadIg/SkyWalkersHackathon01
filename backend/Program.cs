using Microsoft.EntityFrameworkCore; // 1. For Database

var builder = WebApplication.CreateBuilder(args);

// 2. ENABLE CORS (So frontend doesn't cry)
builder.Services.AddCors(o => o.AddDefaultPolicy(p => 
    p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

// 3. SETUP DATABASE (SQLite)
builder.Services.AddDbContext<AppDb>(o =>
{
    var connectionString = builder.Configuration.GetConnectionString("Default")
        ?? "Data Source=hackathon.db";
    o.UseSqlite(connectionString);
});

// 4. ADD SWAGGER (So you can test without frontend)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// 5. TURN ON THE PIPELINES
app.UseCors();
app.UseSwagger();
app.UseSwaggerUI();

// 6. AUTOMATICALLY CREATE DB (Hackathon hack)
using (var scope = app.Services.CreateScope()) {
    var db = scope.ServiceProvider.GetRequiredService<AppDb>();
    db.Database.EnsureCreated();
}

// --- YOUR ENDPOINTS GO HERE ---
app.MapGet("/users", async (AppDb db) => 
    await db.Users.ToListAsync());

app.MapGet("/users/{id}", async (int id, AppDb db) => 
    await db.Users.FindAsync(id)
        is User user
            ? Results.Ok(user)
            : Results.NotFound());
app.MapPost("/users", async (User user, AppDb db) => {
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

app.Run();

// --- YOUR DATABASE CLASSES GO HERE ---
class AppDb : DbContext {
    public AppDb(DbContextOptions<AppDb> options) : base(options) { }
    public DbSet<User> Users { get; set; } = null!; // Table Name
}

// The "Shape" of your data
class User {
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public int Age { get; set; }
}
