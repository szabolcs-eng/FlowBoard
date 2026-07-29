using Microsoft.AspNetCore.Mvc;
using FlowBoardApi.DTOs;
using FlowBoardApi.Services;

namespace FlowBoardApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request)
    {
        var result = await _authService.RegisterAsync(request);
        if (result is null) return Conflict(new { message = "Email already registered." });
        return Ok(result);
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        var result = await _authService.LoginAsync(request);
        if (result is null) return Unauthorized(new { message = "Invalid email or password." });
        return Ok(result);
    }
}
