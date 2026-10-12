namespace JobTracker.API.Models;

public class Application
{
    public int Id { get; set; }
    public int JobId { get; set; }
    public Job Job { get; set; } = null!;
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public string Status { get; set; } = "Applied"; // Applied, Interview, Offer, Rejected
    public string Notes { get; set; } = string.Empty;
    public string ResumeS3Key { get; set; } = string.Empty;   // S3 object key
    public string ResumeUrl { get; set; } = string.Empty;     // Pre-signed URL

    public int? AiMatchScore { get; set; }          // 0-100
    public string AiFeedback { get; set; } = string.Empty;

    public DateTime AppliedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
