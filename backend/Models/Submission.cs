namespace MyHackathonAPI.Models;

public class Submission {
    public int Id { get; set; }
    public int FormId { get; set; }
    
    // If null, they are a Guest 
    // If set, they are a User
    public int? UserId { get; set; } 
    
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public List<Answer> Answers { get; set; } = new();
}
