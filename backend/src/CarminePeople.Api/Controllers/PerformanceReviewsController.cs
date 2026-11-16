using Asp.Versioning;
using CarminePeople.Api.Common;
using CarminePeople.Application.PerformanceReviews.DTOs;
using CarminePeople.Application.PerformanceReviews.Services;
using CarminePeople.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarminePeople.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/performance-reviews")]
[Authorize]
public class PerformanceReviewsController : ControllerBase
{
    private readonly PerformanceReviewService _performanceReviewService;

    public PerformanceReviewsController(PerformanceReviewService performanceReviewService)
    {
        _performanceReviewService = performanceReviewService;
    }

    [HttpPost]
    [Authorize(Policy = Permissions.EmployeeManage)]
    public async Task<IActionResult> Create([FromBody] CreatePerformanceReviewRequest request, CancellationToken ct)
    {
        var result = await _performanceReviewService.CreateAsync(request, User.GetEmployeeId(), ct);
        if (!result.Success) return NotFound(Envelope(result.ErrorCode!, result.ErrorMessage!));
        return Ok(new { success = true, data = result.Data });
    }

    [HttpGet]
    [Authorize(Policy = Permissions.EmployeeManage)]
    public async Task<IActionResult> GetList([FromQuery] Guid? employeeId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await _performanceReviewService.GetListAsync(new PerformanceReviewQuery(employeeId, page, pageSize), ct);
        return Ok(new { success = true, data = result.Items, meta = new { result.Page, result.PageSize, result.Total } });
    }

    [HttpGet("mine")]
    public async Task<IActionResult> GetMine([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await _performanceReviewService.GetListAsync(new PerformanceReviewQuery(User.GetEmployeeId(), page, pageSize), ct);
        return Ok(new { success = true, data = result.Items, meta = new { result.Page, result.PageSize, result.Total } });
    }

    [HttpGet("team-pending")]
    [Authorize(Policy = Permissions.LeaveApprove)]
    public async Task<IActionResult> GetTeamPending([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var unscopedVisibility = User.HasClaim("permission", Permissions.EmployeeManage);
        var result = await _performanceReviewService.GetTeamPendingAsync(User.GetEmployeeId(), unscopedVisibility, page, pageSize, ct);
        return Ok(new { success = true, data = result.Items, meta = new { result.Page, result.PageSize, result.Total } });
    }

    [HttpPut("{id:guid}/self-review")]
    public async Task<IActionResult> SubmitSelfReview(Guid id, [FromBody] SubmitSelfReviewRequest request, CancellationToken ct)
    {
        var result = await _performanceReviewService.SubmitSelfReviewAsync(id, User.GetEmployeeId(), request, ct);
        if (!result.Success)
        {
            return result.ErrorCode == "FORBIDDEN" ? StatusCode(403, Envelope(result.ErrorCode!, result.ErrorMessage!)) : BadRequest(Envelope(result.ErrorCode!, result.ErrorMessage!));
        }
        return Ok(new { success = true, data = (object?)null });
    }

    [HttpPut("{id:guid}/manager-review")]
    [Authorize(Policy = Permissions.LeaveApprove)]
    public async Task<IActionResult> SubmitManagerReview(Guid id, [FromBody] SubmitManagerReviewRequest request, CancellationToken ct)
    {
        var result = await _performanceReviewService.SubmitManagerReviewAsync(id, User.GetEmployeeId(), request, ct);
        if (!result.Success) return BadRequest(Envelope(result.ErrorCode!, result.ErrorMessage!));
        return Ok(new { success = true, data = (object?)null });
    }

    private object Envelope(string code, string message) => ApiEnvelope.Error(HttpContext, code, message);
}
