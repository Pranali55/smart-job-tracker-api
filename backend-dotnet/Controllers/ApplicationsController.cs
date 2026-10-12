using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JobTracker.API.Data;
using JobTracker.API.DTOs;
using JobTracker.API.Models;
using JobTracker.API.Services;

namespace JobTracker.API.Controllers;

[ApiController]
[Route("api/applications")]
[Authorize]
public class ApplicationsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly S3Service _s3;
    private readonly AiScorerService _aiScorer;

    public ApplicationsController(AppDbContext db, S3Service s3, AiScorerService aiScorer)
    {
        _db = db;
        _s3 = s3;
        _aiScorer = aiScorer;
    }

    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>Get all applications for the current user</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? status = null)
    {
        var query = _db.Applications
            .Include(a => a.Job)
            .Where(a => a.UserId == UserId);

        if (!string.IsNullOrEmpty(status))
            query = query.Where(a => a.Status == status);

        var apps = await query
            .OrderByDescending(a => a.AppliedAt)
            .Select(a => MapToResponse(a))
            .ToListAsync();

        return Ok(apps);
    }

    /// <summary>Get a single application</summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var app = await _db.Applications.Include(a => a.Job)
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == UserId);

        if (app == null) return NotFound();
        return Ok(MapToResponse(app));
    }

    /// <summary>Apply to a job</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateApplicationRequest req)
    {
        var jobExists = await _db.Jobs.AnyAsync(j => j.Id == req.JobId && j.UserId == UserId);
        if (!jobExists) return NotFound("Job not found.");

        var application = new Application
        {
            JobId = req.JobId,
            UserId = UserId,
            Notes = req.Notes,
            Status = "Applied"
        };

        _db.Applications.Add(application);
        await _db.SaveChangesAsync();

        var created = await _db.Applications.Include(a => a.Job)
            .FirstAsync(a => a.Id == application.Id);

        return CreatedAtAction(nameof(GetById), new { id = application.Id }, MapToResponse(created));
    }

    /// <summary>Update application status or notes</summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateApplicationRequest req)
    {
        var app = await _db.Applications.Include(a => a.Job)
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == UserId);

        if (app == null) return NotFound();

        var validStatuses = new[] { "Applied", "Interview", "Offer", "Rejected" };
        if (!validStatuses.Contains(req.Status))
            return BadRequest($"Invalid status. Valid values: {string.Join(", ", validStatuses)}");

        app.Status = req.Status;
        app.Notes = req.Notes;
        app.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(MapToResponse(app));
    }

    /// <summary>Upload resume to S3 and trigger AI match scoring</summary>
    [HttpPost("{id}/resume")]
    public async Task<IActionResult> UploadResume(int id, IFormFile file, [FromQuery] string? resumeText = null)
    {
        var app = await _db.Applications.Include(a => a.Job)
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == UserId);

        if (app == null) return NotFound();

        // Upload to S3
        var s3Key = await _s3.UploadResumeAsync(file, UserId);
        app.ResumeS3Key = s3Key;
        app.ResumeUrl = _s3.GetPreSignedUrl(s3Key);

        // Auto-score if resume text is provided
        if (!string.IsNullOrEmpty(resumeText) && !string.IsNullOrEmpty(app.Job.Description))
        {
            var score = await _aiScorer.GetMatchScoreAsync(resumeText, app.Job.Description);
            if (score != null)
            {
                app.AiMatchScore = score.Score;
                app.AiFeedback = score.Feedback;
            }
        }

        app.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(MapToResponse(app));
    }

    /// <summary>Get AI match score for an application</summary>
    [HttpPost("{id}/score")]
    public async Task<IActionResult> ScoreApplication(int id, [FromBody] ScoreRequest req)
    {
        var app = await _db.Applications.Include(a => a.Job)
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == UserId);

        if (app == null) return NotFound();

        var jobDesc = string.IsNullOrEmpty(req.JobDescription) ? app.Job.Description : req.JobDescription;
        var score = await _aiScorer.GetMatchScoreAsync(req.ResumeText, jobDesc);

        if (score == null)
            return StatusCode(503, "AI scoring service is unavailable.");

        app.AiMatchScore = score.Score;
        app.AiFeedback = score.Feedback;
        app.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(score);
    }

    /// <summary>Delete an application</summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var app = await _db.Applications.FirstOrDefaultAsync(a => a.Id == id && a.UserId == UserId);
        if (app == null) return NotFound();

        if (!string.IsNullOrEmpty(app.ResumeS3Key))
            await _s3.DeleteResumeAsync(app.ResumeS3Key);

        _db.Applications.Remove(app);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private static ApplicationResponse MapToResponse(Application a) =>
        new(a.Id, a.JobId, a.Job?.Title ?? "", a.Job?.Company ?? "",
            a.Status, a.Notes, a.ResumeUrl, a.AiMatchScore, a.AiFeedback, a.AppliedAt);
}
