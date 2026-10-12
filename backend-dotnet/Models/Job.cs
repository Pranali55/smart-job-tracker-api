namespace JobTracker.API.Models;

public class Job
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string RequiredSkills { get; set; } = string.Empty;
    public string JobType { get; set; } = "Full-time"; // Full-time, Contract, Remote
    public string Url { get; set; } = string.Empty;
    public DateTime PostedAt { get; set; } = DateTime.UtcNow;
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public ICollection<Application> Applications { get; set; } = new List<Application>();
}
