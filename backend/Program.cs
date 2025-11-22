using Microsoft.EntityFrameworkCore;
using MyHackathonAPI.Data;
using MyHackathonAPI.Endpoints;

var builder = WebApplication.CreateBuilder(args);

// 1. Add Services (The "Tools")
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options => {
    options.AddPolicy("AllowAll", policy => 
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

// Add Database (The "Brain")
builder.Services.AddDbContext<AppDb>(options =>
    options.UseSqlite("Data Source=hackathon.db"));

var app = builder.Build();

// 2. Configure Pipeline (The "Flow")
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");

// 3. Ensure Database & Seed Data (The "Starter Pack")
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDb>();
    db.Database.EnsureCreated(); // Create database if it doesn't exist
    DataSeeder.Seed(db);
}

// 4. Map Endpoints (The "Actions")
app.MapUserEndpoints();
app.MapFormEndpoints();
app.MapAuthEndpoints();
app.MapTenantEndpoints();

app.Run();
