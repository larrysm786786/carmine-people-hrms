using Asp.Versioning;
using CarminePeople.Api.Common;
using CarminePeople.Application.Recruitment.DTOs;
using CarminePeople.Application.Recruitment.Services;
using CarminePeople.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarminePeople.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/recruitment")]
[Authorize(Policy = Permissions.EmployeeManage)]
public class RecruitmentController : ControllerBase
{
    private readonly RecruitmentService _recruitmentService;

    public RecruitmentController(RecruitmentService recruitmentService)
    {
        _recruitmentService = recruitmentService;
    }

    [HttpGet("job-postings")]
    public async Task<IActionResult> GetJobPostings(CancellationToken ct)
    {
        var jobs = await _recruitmentService.GetJobPostingsAsync(ct);
        return Ok(new { success = true, data = jobs });
    }

    [HttpPost("job-postings")]
    public async Task<IActionResult> CreateJobPosting([FromBody] CreateJobPostingRequest request, CancellationToken ct)
    {
        var job = await _recruitmentService.CreateJobPostingAsync(request, User.GetEmployeeId(), ct);
        return Ok(new { success = true, data = job });
    }

    [HttpPut("job-postings/{id:guid}/close")]
    public async Task<IActionResult> CloseJobPosting(Guid id, CancellationToken ct)
    {
        var result = await _recruitmentService.CloseJobPostingAsync(id, User.GetEmployeeId(), ct);
        if (!result.Success) return NotFound(Envelope(result.ErrorCode!, result.ErrorMessage!));
        return Ok(new { success = true, data = (object?)null });
    }

    [HttpGet("job-postings/{id:guid}/candidates")]
    public async Task<IActionResult> GetCandidates(Guid id, CancellationToken ct)
    {
        var result = await _recruitmentService.GetCandidatesAsync(id, ct);
        if (!result.Success) return NotFound(Envelope(result.ErrorCode!, result.ErrorMessage!));
        return Ok(new { success = true, data = result.Data });
    }

    [HttpPost("job-postings/{id:guid}/candidates")]
    public async Task<IActionResult> AddCandidate(Guid id, [FromBody] CreateCandidateRequest request, CancellationToken ct)
    {
        var result = await _recruitmentService.AddCandidateAsync(id, request, User.GetEmployeeId(), ct);
        if (!result.Success) return NotFound(Envelope(result.ErrorCode!, result.ErrorMessage!));
        return Ok(new { success = true, data = result.Data });
    }

    [HttpPut("candidates/{id:guid}/stage")]
    public async Task<IActionResult> UpdateStage(Guid id, [FromBody] UpdateCandidateStageRequest request, CancellationToken ct)
    {
        var result = await _recruitmentService.UpdateStageAsync(id, request, User.GetEmployeeId(), ct);
        if (!result.Success) return NotFound(Envelope(result.ErrorCode!, result.ErrorMessage!));
        return Ok(new { success = true, data = result.Data });
    }

    [HttpDelete("candidates/{id:guid}")]
    public async Task<IActionResult> DeleteCandidate(Guid id, CancellationToken ct)
    {
        var result = await _recruitmentService.DeleteCandidateAsync(id, User.GetEmployeeId(), ct);
        if (!result.Success) return NotFound(Envelope(result.ErrorCode!, result.ErrorMessage!));
        return Ok(new { success = true, data = (object?)null });
    }

    private object Envelope(string code, string message) => ApiEnvelope.Error(HttpContext, code, message);
}
