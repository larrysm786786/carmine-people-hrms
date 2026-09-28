using Asp.Versioning;
using CarminePeople.Application.Auth.DTOs;
using CarminePeople.Application.Auth.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CarminePeople.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/auth")]
[EnableRateLimiting("auth")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;

    public AuthController(AuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var result = await _authService.LoginAsync(request, GetClientIp(), ct);
        if (!result.Success)
        {
            return Unauthorized(new { success = false, error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        }
        return Ok(new { success = true, data = result.Data });
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken ct)
    {
        await _authService.ForgotPasswordAsync(request.Email, ct);
        // Always return success regardless of whether the email exists, to avoid leaking account existence.
        return Ok(new { success = true, data = (object?)null });
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken ct)
    {
        var result = await _authService.ResetPasswordAsync(request.Token, request.NewPassword, ct);
        if (!result.Success)
        {
            return BadRequest(new { success = false, error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        }
        return Ok(new { success = true, data = (object?)null });
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request, CancellationToken ct)
    {
        var result = await _authService.RefreshAsync(request.RefreshToken, GetClientIp(), ct);
        if (!result.Success)
        {
            return Unauthorized(new { success = false, error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        }
        return Ok(new { success = true, data = result.Data });
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest request, CancellationToken ct)
    {
        await _authService.LogoutAsync(request.RefreshToken, GetClientIp(), ct);
        return Ok(new { success = true, data = (object?)null });
    }

    [HttpGet("me")]
    [Authorize]
    public IActionResult Me()
    {
        var claims = User.Claims.Select(c => new { c.Type, c.Value });
        return Ok(new { success = true, data = claims });
    }

    private string? GetClientIp() => HttpContext.Connection.RemoteIpAddress?.ToString();
}
