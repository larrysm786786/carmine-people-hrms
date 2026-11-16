using Asp.Versioning;
using CarminePeople.Api.Common;
using CarminePeople.Application.PulseSurveys.DTOs;
using CarminePeople.Application.PulseSurveys.Services;
using CarminePeople.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarminePeople.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/pulse-surveys")]
[Authorize]
public class PulseSurveysController : ControllerBase
{
    private readonly PulseSurveyService _pulseSurveyService;

    public PulseSurveysController(PulseSurveyService pulseSurveyService)
    {
        _pulseSurveyService = pulseSurveyService;
    }

    [HttpGet]
    [Authorize(Policy = Permissions.EmployeeManage)]
    public async Task<IActionResult> GetList(CancellationToken ct)
    {
        var surveys = await _pulseSurveyService.GetListAsync(ct);
        return Ok(new { success = true, data = surveys });
    }

    [HttpPost]
    [Authorize(Policy = Permissions.EmployeeManage)]
    public async Task<IActionResult> Create([FromBody] CreatePulseSurveyRequest request, CancellationToken ct)
    {
        var survey = await _pulseSurveyService.CreateAsync(request, User.GetEmployeeId(), ct);
        return Ok(new { success = true, data = survey });
    }

    [HttpGet("{id:guid}/responses")]
    [Authorize(Policy = Permissions.EmployeeManage)]
    public async Task<IActionResult> GetResponses(Guid id, CancellationToken ct)
    {
        var result = await _pulseSurveyService.GetResponsesAsync(id, ct);
        if (!result.Success) return NotFound(Envelope(result.ErrorCode!, result.ErrorMessage!));
        return Ok(new { success = true, data = result.Data });
    }

    [HttpPut("{id:guid}/close")]
    [Authorize(Policy = Permissions.EmployeeManage)]
    public async Task<IActionResult> Close(Guid id, CancellationToken ct)
    {
        var result = await _pulseSurveyService.CloseAsync(id, User.GetEmployeeId(), ct);
        if (!result.Success) return NotFound(Envelope(result.ErrorCode!, result.ErrorMessage!));
        return Ok(new { success = true, data = (object?)null });
    }

    [HttpGet("pending")]
    public async Task<IActionResult> GetPending(CancellationToken ct)
    {
        var surveys = await _pulseSurveyService.GetPendingForEmployeeAsync(User.GetEmployeeId(), ct);
        return Ok(new { success = true, data = surveys });
    }

    [HttpPost("{id:guid}/responses")]
    public async Task<IActionResult> SubmitResponse(Guid id, [FromBody] SubmitPulseSurveyResponseRequest request, CancellationToken ct)
    {
        var result = await _pulseSurveyService.SubmitResponseAsync(id, User.GetEmployeeId(), request, ct);
        if (!result.Success) return BadRequest(Envelope(result.ErrorCode!, result.ErrorMessage!));
        return Ok(new { success = true, data = (object?)null });
    }

    private object Envelope(string code, string message) => ApiEnvelope.Error(HttpContext, code, message);
}
