namespace MyHackathonAPI.Models;

public class Answer {
    public int Id { get; set; }
    public int SubmissionId { get; set; }
    public int QuestionId { get; set; }
    public string Value { get; set; } = "";
}
