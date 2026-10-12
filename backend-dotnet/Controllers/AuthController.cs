using Microsoft.AspNetCore.Mvc;
using JobTracker.API.DTOs;
using JobTracker.API.Services;

namespace JobTracker.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;

    public AuthController(AuthService authService) => _authService = authService;

    /// <summary>Register a new user</summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
            return BadRequest("Email and password are required.");

        var result = await _authService.RegisterAsync(req);
        if (result == null)
            return Conflict("A user with this email already exists.");

        return Ok(result);
    }

    /// <summary>Login and get JWT token</summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        var result = await _authService.LoginAsync(req);
        if (result == null)
            return Unauthorized("Invalid email or password.");

        return Ok(result);
    }
}
