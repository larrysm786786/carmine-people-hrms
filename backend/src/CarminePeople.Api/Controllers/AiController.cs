using Asp.Versioning;
using CarminePeople.Domain.Constants;
using CarminePeople.Api.Common;
using CarminePeople.Application.Ai.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CarminePeople.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/ai")]
[Authorize]
[EnableRateLimiting("ai")]
public class AiController : ControllerBase
{
    private readonly AiAssistantService _aiAssistantService;

    public AiController(AiAssistantService aiAssistantService)
    {
        _aiAssistantService = aiAssistantService;
    }

    [HttpGet("status")]
    public IActionResult GetStatus() => Ok(new { success = true, data = new { configured = _aiAssistantService.IsConfigured } });

    // Employee self-service: "what's my leave balance", "am I checked in today", etc.
    [HttpPost("chat")]
    public async Task<IActionResult> Chat([FromBody] AiChatRequest request, CancellationToken ct)
    {
        if (!_aiAssistantService.IsConfigured)
        {
            return Ok(new { success = true, data = new AiChatResponse("The AI assistant isn't configured yet. Please contact your administrator.") });
        }
        var response = await _aiAssistantService.ChatAsEmployeeAsync(User.GetEmployeeId(), request, ct);
        return Ok(new { success = true, data = response });
    }

    // HR/manager analytics: org-wide attendance and leave summaries.
    [HttpPost("chat/hr")]
    [Authorize(Policy = Permissions.AuditView)]
    public async Task<IActionResult> ChatHr([FromBody] AiChatRequest request, CancellationToken ct)
    {
        if (!_aiAssistantService.IsConfigured)
        {
            return Ok(new { success = true, data = new AiChatResponse("The AI assistant isn't configured yet. Please contact your administrator.") });
        }
        var response = await _aiAssistantService.ChatAsHrAsync(request, ct);
        return Ok(new { success = true, data = response });
    }
}
