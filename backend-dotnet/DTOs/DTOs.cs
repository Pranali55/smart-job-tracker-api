namespace JobTracker.API.DTOs;

// ── Auth ──────────────────────────────────────────────
public record RegisterRequest(string Name, string Email, string Password);
public record LoginRequest(string Email, string Password);
public record AuthResponse(string Token, string Name, string Email);

// ── Jobs ──────────────────────────────────────────────
public record CreateJobRequest(
    string Title,
    string Company,
    string Location,
    string Description,
    string RequiredSkills,
    string JobType,
    string Url
);

public record JobResponse(
    int Id,
    string Title,
    string Company,
    string Location,
    string Description,
    string RequiredSkills,
    string JobType,
    string Url,
    DateTime PostedAt
);

// ── Applications ──────────────────────────────────────
public record CreateApplicationRequest(int JobId, string Notes);

public record UpdateApplicationRequest(string Status, string Notes);

public record ApplicationResponse(
    int Id,
    int JobId,
    string JobTitle,
    string Company,
    string Status,
    string Notes,
    string ResumeUrl,
    int? AiMatchScore,
    string AiFeedback,
    DateTime AppliedAt
);

// ── AI Scorer ─────────────────────────────────────────
public record ScoreRequest(string ResumeText, string JobDescription);
public record ScoreResponse(int Score, string Feedback, List<string> MissingSkills, List<string> MatchedSkills);
