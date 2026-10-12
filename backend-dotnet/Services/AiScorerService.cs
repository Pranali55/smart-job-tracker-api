using System.Text;
using System.Text.Json;
using JobTracker.API.DTOs;

namespace JobTracker.API.Services;

public class AiScorerService
{
    private readonly HttpClient _http;
    private readonly ILogger<AiScorerService> _logger;

    public AiScorerService(HttpClient http, ILogger<AiScorerService> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<ScoreResponse?> GetMatchScoreAsync(string resumeText, string jobDescription)
    {
        try
        {
            var payload = new { resume_text = resumeText, job_description = jobDescription };
            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _http.PostAsync("/score", content);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<ScoreResponse>();
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get AI score from FastAPI service");
            return null;
        }
    }
}
