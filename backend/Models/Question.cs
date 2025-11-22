namespace MyHackathonAPI.Models;

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
