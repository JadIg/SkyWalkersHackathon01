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

    // VERSIONING & TENANCY (Keep these!)
    public int Version { get; set; } = 1; 
    public int? ParentGroupId { get; set; }
    public int TenantId { get; set; }
    
    public List<Question> Questions { get; set; } = new();
    public List<Submission> Submissions { get; set; } = new();
}
