using Microsoft.EntityFrameworkCore;
using MyHackathonAPI.Data;
using MyHackathonAPI.Models;

namespace MyHackathonAPI.Endpoints;

public static class FormEndpoints {
    public static void MapFormEndpoints(this IEndpointRouteBuilder app) {
        
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
                
                // Reload the form to get the updated data with proper IDs
                await db.Entry(existingForm).Collection(f => f.Questions).LoadAsync();
                return Results.Ok(existingForm); // Return the updated form consistently
            }
        });

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

        // DELETE /forms/{id}
        app.MapDelete("/forms/{id}", async (int id, AppDb db) => {
            var form = await db.Forms.Include(f => f.Questions).FirstOrDefaultAsync(f => f.Id == id);
            if (form is null) return Results.NotFound();

            // Delete related submissions and their answers
            var submissions = await db.Submissions.Include(s => s.Answers).Where(s => s.FormId == id).ToListAsync();
            foreach (var submission in submissions) {
                db.Answers.RemoveRange(submission.Answers);
                db.Submissions.Remove(submission);
            }

            // Delete questions
            db.Questions.RemoveRange(form.Questions);
            
            // Delete form
            db.Forms.Remove(form);
            
            await db.SaveChangesAsync();
            return Results.Ok(new { message = "Form deleted successfully" });
        });
    }
}
