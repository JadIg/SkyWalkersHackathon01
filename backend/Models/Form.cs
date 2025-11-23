namespace MyHackathonAPI.Models;

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

    // VERSIONING & TENANCY
    public int Version { get; private set; } = 1; 
    public int? ParentGroupId { get; set; }
    public int TenantId { get; set; }
    
    // SOFT DELETE
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public int? DeletedBy { get; set; }
    
    // ACCESS CONTROL
    public int CreatedBy { get; set; }
    
    public List<Question> Questions { get; set; } = new();
    public List<Submission> Submissions { get; set; } = new();

    // Apply in-place update when there are no submissions (no optimistic concurrency)
    public void UpdateContent(string title, string description, bool isPublished, bool isPublic,
        DateTime? startDate, DateTime? endDate, bool oneSubmissionPerUser, IEnumerable<Question> newQuestions) {
        Title = title;
        Description = description;
        IsPublished = isPublished;
        IsPublic = isPublic;
        StartDate = startDate;
        EndDate = endDate;
        OneSubmissionPerUser = oneSubmissionPerUser;
        Questions = newQuestions.ToList();
        Version++; // simple auto increment
    }

    // Factory for initial form (always version 1, ParentGroupId left null until persisted)
    public static Form CreateInitial(string title, string description, int tenantId, bool isPublished, bool isPublic,
        DateTime? startDate, DateTime? endDate, bool oneSubmissionPerUser, IEnumerable<Question> questions) =>
        new Form {
            Title = title,
            Description = description,
            TenantId = tenantId,
            IsPublished = isPublished,
            IsPublic = isPublic,
            StartDate = startDate,
            EndDate = endDate,
            OneSubmissionPerUser = oneSubmissionPerUser,
            Version = 1, // allowed inside class
            Questions = questions.ToList()
        };

    // Factory for a new version when submissions exist
    public static Form CreateNextVersion(Form existing, Form inputTemplate) =>
        new Form {
            Title = inputTemplate.Title,
            Description = inputTemplate.Description,
            TenantId = existing.TenantId,
            CreatedBy = existing.CreatedBy,
            IsPublished = inputTemplate.IsPublished,
            IsPublic = inputTemplate.IsPublic,
            StartDate = inputTemplate.StartDate,
            EndDate = inputTemplate.EndDate,
            OneSubmissionPerUser = inputTemplate.OneSubmissionPerUser,
            Version = existing.Version + 1,
            ParentGroupId = existing.ParentGroupId ?? existing.Id,
            Questions = inputTemplate.Questions.Select(q => new Question {
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
}
