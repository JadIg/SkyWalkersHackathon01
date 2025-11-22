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
            IsPublished = inputForm.IsPublished,
            IsPublic = inputForm.IsPublic,
            TenantId = existingForm.TenantId,
            Version = existingForm.Version + 1,
            ParentGroupId = existingForm.ParentGroupId ?? existingForm.Id, // Link to original
            Questions = inputForm.Questions.Select(q => new Question {
                Label = q.Label,
                Type = q.Type,
                IsRequired = q.IsRequired,
                Options = q.Options,
                HelpText = q.HelpText
            }).ToList()
        };
        
        db.Forms.Add(newForm);
        await db.SaveChangesAsync();
        return Results.Ok(newForm); // Return the NEW version
    } else {
        // No submissions, safe to edit in place
        existingForm.Title = inputForm.Title;
        existingForm.IsPublished = inputForm.IsPublished;
        existingForm.IsPublic = inputForm.IsPublic;
        
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
    
    // Check Access
    if (!form.IsPublic && submission.UserId == null) {
        return Results.Unauthorized();
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
    public bool IsPublished { get; set; } = false;
    public bool IsPublic { get; set; } = true; // "Public" or "Authenticated Only"
    
    // VERSIONING MAGIC
    public int Version { get; set; } = 1; 
    public int? ParentGroupId { get; set; } // Links v1, v2, v3 together
    
    // TENANCY
    public int TenantId { get; set; }
    
    public List<Question> Questions { get; set; } = new();
    public List<Submission> Submissions { get; set; } = new();
}

public class Question {
    public int Id { get; set; }
    public int FormId { get; set; }
    public string Label { get; set; } = ""; // "What is your gender?"
    public string HelpText { get; set; } = ""; // "Please select one"
    public string Type { get; set; } = "Text"; // "Radio", "Checkbox", "Text", "Rating", "Date"
    public bool IsRequired { get; set; } = false;
    
    // STORE OPTIONS HERE (Hackathon style)
    // If Type is "Radio", Options = "Male,Female,Other"
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