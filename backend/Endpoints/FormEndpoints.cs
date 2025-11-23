using Microsoft.EntityFrameworkCore;
using MyHackathonAPI.Data;
using MyHackathonAPI.Models;

namespace MyHackathonAPI.Endpoints;

public static class FormEndpoints {
    public static void MapFormEndpoints(this IEndpointRouteBuilder app) {
        
        // GET /forms?tenantId=1&userId=1&role=Editor (only non-deleted)
        app.MapGet("/forms", async (int tenantId, int? userId, string? role, AppDb db) => {
            var query = db.Forms.Where(f => f.TenantId == tenantId && !f.IsDeleted);
            
            // If role is NOT Admin, filter by CreatedBy
            if (!string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase) && userId.HasValue) {
                query = query.Where(f => f.CreatedBy == userId || f.CreatedBy == 0); // 0 for legacy forms
            }
            
            return await query.ToListAsync();
        });

        // GET /forms/deleted?tenantId=1 (only deleted forms)
        app.MapGet("/forms/deleted", async (int tenantId, AppDb db) => 
            await db.Forms.Where(f => f.TenantId == tenantId && f.IsDeleted)
                .OrderByDescending(f => f.DeletedAt)
                .ToListAsync());

        // GET /forms/{id}
        app.MapGet("/forms/{id}", async (int id, AppDb db) => 
            await db.Forms.Include(f => f.Questions).FirstOrDefaultAsync(f => f.Id == id)
                is Form form
                    ? Results.Ok(form)
                    : Results.NotFound());

        // POST /forms
        app.MapPost("/forms", async (Form form, AppDb db) => {
            var initial = Form.CreateInitial(form.Title, form.Description, form.TenantId, form.IsPublished, form.IsPublic,
                form.StartDate, form.EndDate, form.OneSubmissionPerUser, form.Questions);
            
            // Set CreatedBy
            initial.CreatedBy = form.CreatedBy;

            db.Forms.Add(initial);
            await db.SaveChangesAsync();
            if (initial.ParentGroupId is null) { initial.ParentGroupId = initial.Id; await db.SaveChangesAsync(); }
            return Results.Created($"/forms/{initial.Id}", initial);
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
                // Ensure root has ParentGroupId set to its own Id
                if (existingForm.ParentGroupId is null) {
                    existingForm.ParentGroupId = existingForm.Id;
                }
                var newForm = Form.CreateNextVersion(existingForm, inputForm);
                db.Forms.Add(newForm);
                await db.SaveChangesAsync();
                return Results.Ok(newForm);
            } else {
                // Simple in-place update; ignore any incoming Version value
                existingForm.UpdateContent(
                    inputForm.Title,
                    inputForm.Description,
                    inputForm.IsPublished,
                    inputForm.IsPublic,
                    inputForm.StartDate,
                    inputForm.EndDate,
                    inputForm.OneSubmissionPerUser,
                    inputForm.Questions
                );
                await db.SaveChangesAsync();
                await db.Entry(existingForm).Collection(f => f.Questions).LoadAsync();
                return Results.Ok(existingForm);
            }
        });

        // GET /forms/{id}/versions - list all versions for the form's group
        app.MapGet("/forms/{id}/versions", async (int id, AppDb db) => {
            var form = await db.Forms.FindAsync(id);
            if (form is null) return Results.NotFound();
            var rootId = form.ParentGroupId ?? form.Id;
            var versions = await db.Forms
                .Where(f => f.ParentGroupId == rootId || f.Id == rootId)
                .OrderBy(f => f.Version)
                .Select(f => new {
                    f.Id,
                    f.Version,
                    f.Title,
                    f.IsPublished,
                    f.IsPublic,
                    f.TenantId
                })
                .ToListAsync();
            return Results.Ok(new { rootFormId = rootId, items = versions });
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

        // DELETE /forms/{id} - SOFT DELETE
        app.MapDelete("/forms/{id}", async (int id, int? userId, AppDb db) => {
            var form = await db.Forms.FirstOrDefaultAsync(f => f.Id == id);
            if (form is null) return Results.NotFound();
            if (form.IsDeleted) return Results.BadRequest("Form already deleted");

            form.IsDeleted = true;
            form.DeletedAt = DateTime.UtcNow;
            form.DeletedBy = userId;
            
            await db.SaveChangesAsync();
            return Results.Ok(new { message = "Form moved to trash" });
        });

        // POST /forms/{id}/restore - RESTORE DELETED FORM
        app.MapPost("/forms/{id}/restore", async (int id, AppDb db) => {
            var form = await db.Forms.FirstOrDefaultAsync(f => f.Id == id);
            if (form is null) return Results.NotFound();
            if (!form.IsDeleted) return Results.BadRequest("Form is not deleted");

            form.IsDeleted = false;
            form.DeletedAt = null;
            form.DeletedBy = null;
            
            await db.SaveChangesAsync();
            return Results.Ok(new { message = "Form restored successfully" });
        });

        // DELETE /forms/{id}/permanent - HARD DELETE (permanent)
        app.MapDelete("/forms/{id}/permanent", async (int id, AppDb db) => {
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
            return Results.Ok(new { message = "Form permanently deleted" });
        });
    }
}
