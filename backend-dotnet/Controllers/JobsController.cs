using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JobTracker.API.Data;
using JobTracker.API.DTOs;
using JobTracker.API.Models;

namespace JobTracker.API.Controllers;

[ApiController]
[Route("api/jobs")]
[Authorize]
public class JobsController : ControllerBase
{
    private readonly AppDbContext _db;

    public JobsController(AppDbContext db) => _db = db;

    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>Get all jobs added by the current user</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var jobs = await _db.Jobs
            .Where(j => j.UserId == UserId)
            .OrderByDescending(j => j.PostedAt)
            .Select(j => MapToResponse(j))
            .ToListAsync();

        return Ok(jobs);
    }

    /// <summary>Get a single job by ID</summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var job = await _db.Jobs.FirstOrDefaultAsync(j => j.Id == id && j.UserId == UserId);
        if (job == null) return NotFound();
        return Ok(MapToResponse(job));
    }

    /// <summary>Add a new job to track</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateJobRequest req)
    {
        var job = new Job
        {
            Title = req.Title,
            Company = req.Company,
            Location = req.Location,
            Description = req.Description,
            RequiredSkills = req.RequiredSkills,
            JobType = req.JobType,
            Url = req.Url,
            UserId = UserId
        };

        _db.Jobs.Add(job);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = job.Id }, MapToResponse(job));
    }

    /// <summary>Update a job</summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] CreateJobRequest req)
    {
        var job = await _db.Jobs.FirstOrDefaultAsync(j => j.Id == id && j.UserId == UserId);
        if (job == null) return NotFound();

        job.Title = req.Title;
        job.Company = req.Company;
        job.Location = req.Location;
        job.Description = req.Description;
        job.RequiredSkills = req.RequiredSkills;
        job.JobType = req.JobType;
        job.Url = req.Url;

        await _db.SaveChangesAsync();
        return Ok(MapToResponse(job));
    }

    /// <summary>Delete a job</summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var job = await _db.Jobs.FirstOrDefaultAsync(j => j.Id == id && j.UserId == UserId);
        if (job == null) return NotFound();

        _db.Jobs.Remove(job);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private static JobResponse MapToResponse(Job j) =>
        new(j.Id, j.Title, j.Company, j.Location, j.Description, j.RequiredSkills, j.JobType, j.Url, j.PostedAt);
}
