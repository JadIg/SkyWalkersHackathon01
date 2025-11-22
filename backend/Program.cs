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

    // --- START SEEDER ---
    if (!db.Forms.Any()) 
    {
        Console.WriteLine("🌱 Seeding Fake Data...");
        
        // Create a "Demo Form" for National Bank (Tenant 1)
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

        // Create 20 Fake Submissions
        var random = new Random();
        for (int i = 0; i < 20; i++) 
        {
            db.Submissions.Add(new Submission {
                FormId = form.Id,
                SubmittedAt = DateTime.UtcNow.AddHours(-random.Next(1, 48)),
                Answers = new List<Answer> {
                    new Answer { QuestionId = 1, Value = random.Next(1, 6).ToString() }, // Random Rating
                    new Answer { QuestionId = 2, Value = new[]{"Backend", "Frontend", "Design"}[random.Next(3)] } // Random Track
                }
            });
        }
        db.SaveChanges();
        Console.WriteLine("✅ 20 Fake Submissions Created! ID: 1");
    }
    // --- END SEEDER ---
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

// --- FORM ENDPOINTS ---

// GET /forms?tenantId=1
app.MapGet("/forms", async (int tenantId, AppDb db) => 
    await db.Forms.Where(f => f.TenantId == tenantId).ToListAsync());

// GET /forms/{id}
app.MapGet("/forms/{id}", async (int id, AppDb db) => 
    await db.Forms.Include(f => f.Questions).FirstOrDefaultAsync(f => f.Id == id)
        is Form form
            ? Results.Ok(form)
            : Results.NotFound());

// POST /forms
app.MapPost("/forms", async (Form form, AppDb db) => {
    db.Forms.Add(form);
    await db.SaveChangesAsync();
    return Results.Created($"/forms/{form.Id}", form);
});

// PUT /forms/{id} - HANDLES VERSIONING
app.MapPut("/forms/{id}", async (int id, Form inputForm, AppDb db) => {
    var existingForm = await db.Forms.Include(f => f.Questions).FirstOrDefaultAsync(f => f.Id == id);
    if (existingForm is null) return Results.NotFound();

    // Check if form has submissions
    var hasSubmissions = await db.Submissions.AnyAsync(s => s.FormId == id);

    if (hasSubmissions) {
        // STRATEGY A: VERSION LOCKING
        // Create a NEW form version instead of editing the old one
        var newForm = new Form {
            Title = inputForm.Title,
            Description = inputForm.Description,
            IsPublished = inputForm.IsPublished,
            IsPublic = inputForm.IsPublic,
            StartDate = inputForm.StartDate,
            EndDate = inputForm.EndDate,
            OneSubmissionPerUser = inputForm.OneSubmissionPerUser,
            TenantId = existingForm.TenantId,
            Version = existingForm.Version + 1,
            ParentGroupId = existingForm.ParentGroupId ?? existingForm.Id, // Link to original
            Questions = inputForm.Questions.Select(q => new Question {
                Label = q.Label,
                Type = q.Type,
                IsRequired = q.IsRequired,
                Options = q.Options,
                HelpText = q.HelpText,
                Placeholder = q.Placeholder,
                DefaultValue = q.DefaultValue,
                ValidationRules = q.ValidationRules
            }).ToList()
        };
        
        db.Forms.Add(newForm);
        await db.SaveChangesAsync();
        return Results.Ok(newForm); // Return the NEW version
    } else {
        // No submissions, safe to edit in place
        existingForm.Title = inputForm.Title;
        existingForm.Description = inputForm.Description;
        existingForm.IsPublished = inputForm.IsPublished;
        existingForm.IsPublic = inputForm.IsPublic;
        existingForm.StartDate = inputForm.StartDate;
        existingForm.EndDate = inputForm.EndDate;
        existingForm.OneSubmissionPerUser = inputForm.OneSubmissionPerUser;
        
        // Replace questions (Simple approach: Delete all, Add new)
        db.Questions.RemoveRange(existingForm.Questions);
        existingForm.Questions = inputForm.Questions;
        
        await db.SaveChangesAsync();
        return Results.NoContent();
    }
});

// --- SUBMISSION ENDPOINTS ---

// POST /forms/{id}/submit
app.MapPost("/forms/{id}/submit", async (int id, Submission submission, AppDb db) => {
    var form = await db.Forms.FindAsync(id);
    if (form is null) return Results.NotFound();
    
    // 1. Check Submission Window 
    var now = DateTime.UtcNow;
    if (form.StartDate != null && now < form.StartDate) return Results.BadRequest("Form not open yet.");
    if (form.EndDate != null && now > form.EndDate) return Results.BadRequest("Form closed.");

    // 2. Check Access (Public vs Authenticated)
    if (!form.IsPublic && submission.UserId == null) {
        return Results.Unauthorized();
    }

    // 3. Check Single Submission Rule 
    if (form.OneSubmissionPerUser && submission.UserId != null) {
        var alreadySubmitted = await db.Submissions
            .AnyAsync(s => s.FormId == id && s.UserId == submission.UserId);
        
        if (alreadySubmitted) return Results.Conflict("You have already submitted this form.");
    }

    submission.FormId = id;
    submission.SubmittedAt = DateTime.UtcNow;
    
    db.Submissions.Add(submission);
    await db.SaveChangesAsync();
    return Results.Created($"/submissions/{submission.Id}", submission);
});

// GET /forms/{id}/submissions
app.MapGet("/forms/{id}/submissions", async (int id, AppDb db) => 
    await db.Submissions.Include(s => s.Answers).Where(s => s.FormId == id).ToListAsync());

// GET /forms/{id}/stats
app.MapGet("/forms/{id}/stats", async (int id, AppDb db) => {
    var form = await db.Forms.FindAsync(id);
    if (form is null) return Results.NotFound();

    var total = await db.Submissions.CountAsync(s => s.FormId == id);
    
    // Simple Analytics: Group answers by Value to show "Distribution"
    var distribution = await db.Submissions
        .Where(s => s.FormId == id)
        .SelectMany(s => s.Answers)
        .GroupBy(a => new { a.QuestionId, a.Value })
        .Select(g => new { 
            QuestionId = g.Key.QuestionId, 
            Answer = g.Key.Value, 
            Count = g.Count() 
        })
        .ToListAsync();

    return Results.Ok(new {
        FormTitle = form.Title,
        TotalSubmissions = total,
        Analytics = distribution
    });
});

app.Run();

// --- YOUR DATABASE CLASSES GO HERE ---
class AppDb : DbContext {
    public AppDb(DbContextOptions<AppDb> options) : base(options) { }
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Form> Forms { get; set; } = null!;
    public DbSet<Question> Questions { get; set; } = null!;
    public DbSet<Submission> Submissions { get; set; } = null!;
    public DbSet<Answer> Answers { get; set; } = null!;
}

// The "Shape" of your data
public class User {
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string PhoneNumber { get; set; } = "";
    public int Age { get; set; }
    public string Role { get; set; } = "Editor"; // "Admin" or "Editor"
    public int TenantId { get; set; } // The "Organization" ID
}

public class Form {
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = ""; // Good to have
    public bool IsPublished { get; set; } = false;
    public bool IsPublic { get; set; } = true; 
    
    // --- NEW FIELDS ---
    public DateTime? StartDate { get; set; } // 
    public DateTime? EndDate { get; set; }   // 
    public bool OneSubmissionPerUser { get; set; } = false; // 

    // VERSIONING & TENANCY (Keep these!)
    public int Version { get; set; } = 1; 
    public int? ParentGroupId { get; set; }
    public int TenantId { get; set; }
    
    public List<Question> Questions { get; set; } = new();
    public List<Submission> Submissions { get; set; } = new();
}

public class Question {
    public int Id { get; set; }
    public int FormId { get; set; }
    public string Label { get; set; } = ""; 
    public string HelpText { get; set; } = ""; 
    public string Type { get; set; } = "Text"; 
    public bool IsRequired { get; set; } = false;
    
    // --- NEW FIELDS ---
    public string Placeholder { get; set; } = ""; 
    public string DefaultValue { get; set; } = ""; 
    public string ValidationRules { get; set; } = ""; // Store JSON: { "min": 5, "regex": "..." }
    
    public string? Options { get; set; } 
}

public class Submission {
    public int Id { get; set; }
    public int FormId { get; set; }
    
    // If null, they are a Guest 
    // If set, they are a User
    public int? UserId { get; set; } 
    
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public List<Answer> Answers { get; set; } = new();
}

public class Answer {
    public int Id { get; set; }
    public int SubmissionId { get; set; }
    public int QuestionId { get; set; }
    public string Value { get; set; } = "";
}
