using Asp.Versioning;
using CarminePeople.Api.Common;
using CarminePeople.Application.Onboarding.DTOs;
using CarminePeople.Application.Onboarding.Services;
using CarminePeople.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarminePeople.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/onboarding")]
[Authorize]
public class OnboardingController : ControllerBase
{
    private readonly OnboardingService _onboardingService;

    public OnboardingController(OnboardingService onboardingService)
    {
        _onboardingService = onboardingService;
    }

    [HttpGet("templates")]
    [Authorize(Policy = Permissions.EmployeeManage)]
    public async Task<IActionResult> GetTemplates(CancellationToken ct)
    {
        var templates = await _onboardingService.GetTemplatesAsync(ct);
        return Ok(new { success = true, data = templates });
    }

    [HttpPost("templates")]
    [Authorize(Policy = Permissions.EmployeeManage)]
    public async Task<IActionResult> CreateTemplate([FromBody] CreateOnboardingTaskTemplateRequest request, CancellationToken ct)
    {
        var template = await _onboardingService.CreateTemplateAsync(request, User.GetEmployeeId(), ct);
        return Ok(new { success = true, data = template });
    }

    [HttpPut("templates/{id:guid}")]
    [Authorize(Policy = Permissions.EmployeeManage)]
    public async Task<IActionResult> UpdateTemplate(Guid id, [FromBody] UpdateOnboardingTaskTemplateRequest request, CancellationToken ct)
    {
        var result = await _onboardingService.UpdateTemplateAsync(id, request, User.GetEmployeeId(), ct);
        if (!result.Success) return NotFound(Envelope(result.ErrorCode!, result.ErrorMessage!));
        return Ok(new { success = true, data = result.Data });
    }

    [HttpDelete("templates/{id:guid}")]
    [Authorize(Policy = Permissions.EmployeeManage)]
    public async Task<IActionResult> DeleteTemplate(Guid id, CancellationToken ct)
    {
        var result = await _onboardingService.DeleteTemplateAsync(id, User.GetEmployeeId(), ct);
        if (!result.Success) return NotFound(Envelope(result.ErrorCode!, result.ErrorMessage!));
        return Ok(new { success = true, data = (object?)null });
    }

    [HttpGet("employees/{employeeId:guid}/tasks")]
    [Authorize(Policy = Permissions.EmployeeManage)]
    public async Task<IActionResult> GetTasksForEmployee(Guid employeeId, CancellationToken ct)
    {
        var tasks = await _onboardingService.GetTasksForEmployeeAsync(employeeId, ct);
        return Ok(new { success = true, data = tasks });
    }

    [HttpPost("employees/{employeeId:guid}/apply-templates")]
    [Authorize(Policy = Permissions.EmployeeManage)]
    public async Task<IActionResult> ApplyTemplates(Guid employeeId, CancellationToken ct)
    {
        var result = await _onboardingService.ApplyTemplatesAsync(employeeId, User.GetEmployeeId(), ct);
        if (!result.Success) return NotFound(Envelope(result.ErrorCode!, result.ErrorMessage!));
        return Ok(new { success = true, data = new { added = result.Data } });
    }

    [HttpPost("employees/{employeeId:guid}/tasks")]
    [Authorize(Policy = Permissions.EmployeeManage)]
    public async Task<IActionResult> AddTask(Guid employeeId, [FromBody] CreateOnboardingTaskRequest request, CancellationToken ct)
    {
        var result = await _onboardingService.AddTaskAsync(employeeId, request, User.GetEmployeeId(), ct);
        if (!result.Success) return NotFound(Envelope(result.ErrorCode!, result.ErrorMessage!));
        return Ok(new { success = true, data = result.Data });
    }

    [HttpGet("tasks/mine")]
    public async Task<IActionResult> GetMine(CancellationToken ct)
    {
        var tasks = await _onboardingService.GetMineAsync(User.GetEmployeeId(), ct);
        return Ok(new { success = true, data = tasks });
    }

    [HttpPut("tasks/{id:guid}/complete")]
    public async Task<IActionResult> Complete(Guid id, CancellationToken ct) => await SetCompletion(id, true, ct);

    [HttpPut("tasks/{id:guid}/reopen")]
    public async Task<IActionResult> Reopen(Guid id, CancellationToken ct) => await SetCompletion(id, false, ct);

    private async Task<IActionResult> SetCompletion(Guid id, bool isCompleted, CancellationToken ct)
    {
        var unscopedAccess = User.HasClaim("permission", Permissions.EmployeeManage);
        var result = await _onboardingService.SetCompletionAsync(id, User.GetEmployeeId(), unscopedAccess, isCompleted, ct);
        if (!result.Success)
        {
            return result.ErrorCode == "FORBIDDEN" ? StatusCode(403, Envelope(result.ErrorCode!, result.ErrorMessage!)) : NotFound(Envelope(result.ErrorCode!, result.ErrorMessage!));
        }
        return Ok(new { success = true, data = (object?)null });
    }

    [HttpDelete("tasks/{id:guid}")]
    [Authorize(Policy = Permissions.EmployeeManage)]
    public async Task<IActionResult> DeleteTask(Guid id, CancellationToken ct)
    {
        var result = await _onboardingService.DeleteTaskAsync(id, User.GetEmployeeId(), ct);
        if (!result.Success) return NotFound(Envelope(result.ErrorCode!, result.ErrorMessage!));
        return Ok(new { success = true, data = (object?)null });
    }

    private object Envelope(string code, string message) => ApiEnvelope.Error(HttpContext, code, message);
}
