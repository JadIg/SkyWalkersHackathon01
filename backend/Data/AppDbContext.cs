using Microsoft.EntityFrameworkCore;
using MyHackathonAPI.Models;

namespace MyHackathonAPI.Data;

public class AppDb : DbContext {
    public AppDb(DbContextOptions<AppDb> options) : base(options) { }
    
    public DbSet<Tenant> Tenants { get; set; } = null!; 
    
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Form> Forms { get; set; } = null!;
    public DbSet<Question> Questions { get; set; } = null!;
    public DbSet<Submission> Submissions { get; set; } = null!;
    public DbSet<Answer> Answers { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        // Mark Form.Version as concurrency token (optional EF check)
        modelBuilder.Entity<Form>().Property(f => f.Version).IsConcurrencyToken();
    }
}
