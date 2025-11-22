using MyHackathonAPI.Models;

namespace MyHackathonAPI.Data;

public static class DataSeeder {
    public static void Seed(AppDb db) {
        if (!db.Tenants.Any()) 
        {
            Console.WriteLine("🌱 Seeding Companies & Users...");
            
            // 1. Create Companies
            var tenants = new List<Tenant> {
                new Tenant { Name = "National Bank of Iraq" }, // ID 1
                new Tenant { Name = "Zain" },                  // ID 2
                new Tenant { Name = "CBI" }                    // ID 3
            };
            db.Tenants.AddRange(tenants);
            db.SaveChanges(); 

            // 2. Create Login Users (The "Sophisticated" part)
            var users = new List<User> {
                new User { Name = "Ali (Bank Admin)", Email = "ali@nb.com", Password = "password", Role = "Admin", TenantId = 1 },
                new User { Name = "Sarah (Zain Admin)", Email = "sarah@zain.com", Password = "password", Role = "Admin", TenantId = 2 },
                new User { Name = "Ahmed (CBI Admin)", Email = "ahmed@cbi.com", Password = "password", Role = "Admin", TenantId = 3 }
            };
            db.Users.AddRange(users);
            db.SaveChanges();

            // 3. Create Demo Form (For Bank)
            var form = new Form {
                Title = "Hackathon Feedback Survey",
                Description = "Tell us about your experience!",
                TenantId = 1, 
                IsPublished = true, 
                IsPublic = true, 
                Version = 1,
                Questions = new List<Question> {
                    new Question { Label = "How was the food?", Type = "Rating", Options = "1-5" },
                    new Question { Label = "Which track are you in?", Type = "Dropdown", Options = "Backend,Frontend,Design" }
                }
            };
            db.Forms.Add(form);
            db.SaveChanges();
            
            Console.WriteLine("✅ Login Accounts Created:");
            Console.WriteLine("   - ali@nb.com  -> Tenant 1");
            Console.WriteLine("   - sarah@zain.com -> Tenant 2");
        }
    }
}
