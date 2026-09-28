using Asp.Versioning;
using CarminePeople.Api.Common;
using CarminePeople.Application.Overtime.DTOs;
using CarminePeople.Application.Overtime.Services;
using CarminePeople.Domain.Constants;
using CarminePeople.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarminePeople.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/overtime")]
[Authorize]
public class OvertimeController : ControllerBase
{
    private readonly OvertimeService _overtimeService;

    public OvertimeController(OvertimeService overtimeService)
    {
        _overtimeService = overtimeService;
    }

    [HttpPost]
    [Authorize(Policy = Permissions.OvertimeApply)]
    public async Task<IActionResult> Create([FromBody] RequestOvertimeRequest request, CancellationToken ct)
    {
        var result = await _overtimeService.RequestAsync(User.GetEmployeeId(), request, ct);
        if (!result.Success) return BadRequest(Envelope(result.ErrorCode!, result.ErrorMessage!));
        return Ok(new { success = true, data = result.Data });
    }

    [HttpGet]
    public async Task<IActionResult> GetList(
        [FromQuery] Guid? employeeId, [FromQuery] OvertimeRequestStatus? status,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        // Self-service: an employee with only overtime.apply may list their own requests;
        // seeing anyone else's list requires overtime.approve (manager/HR/SuperAdmin).
        var targetEmployeeId = employeeId ?? User.GetEmployeeId();
        if (targetEmployeeId != User.GetEmployeeId() && !User.HasClaim("permission", Permissions.OvertimeApprove))
        {
            return Forbid();
        }

        var result = await _overtimeService.GetListAsync(new OvertimeQuery(targetEmployeeId, status, page, pageSize), ct);
        return Ok(new { success = true, data = result.Items, meta = new { result.Page, result.PageSize, result.Total } });
    }

    [HttpGet("team-pending")]
    [Authorize(Policy = Permissions.OvertimeApprove)]
    public async Task<IActionResult> GetTeamPending([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var unscopedVisibility = User.HasClaim("permission", Permissions.EmployeeManage);
        var result = await _overtimeService.GetTeamPendingAsync(User.GetEmployeeId(), unscopedVisibility, page, pageSize, ct);
        return Ok(new { success = true, data = result.Items, meta = new { result.Page, result.PageSize, result.Total } });
    }

    [HttpPut("{id:guid}/approve")]
    [Authorize(Policy = Permissions.OvertimeApprove)]
    public async Task<IActionResult> Approve(Guid id, [FromBody] OvertimeDecisionRequest request, CancellationToken ct)
    {
        var result = await _overtimeService.ApproveAsync(id, User.GetEmployeeId(), request.Comment, ct);
        if (!result.Success) return DecisionError(result.ErrorCode!, result.ErrorMessage!);
        return Ok(new { success = true, data = (object?)null });
    }

    [HttpPut("{id:guid}/reject")]
    [Authorize(Policy = Permissions.OvertimeApprove)]
    public async Task<IActionResult> Reject(Guid id, [FromBody] OvertimeDecisionRequest request, CancellationToken ct)
    {
        var result = await _overtimeService.RejectAsync(id, User.GetEmployeeId(), request.Comment, ct);
        if (!result.Success) return DecisionError(result.ErrorCode!, result.ErrorMessage!);
        return Ok(new { success = true, data = (object?)null });
    }

    [HttpPut("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
    {
        var result = await _overtimeService.CancelAsync(id, User.GetEmployeeId(), ct);
        if (!result.Success) return BadRequest(Envelope(result.ErrorCode!, result.ErrorMessage!));
        return Ok(new { success = true, data = (object?)null });
    }

    private IActionResult DecisionError(string code, string message) =>
        code == "FORBIDDEN" ? StatusCode(403, Envelope(code, message)) : BadRequest(Envelope(code, message));

    private object Envelope(string code, string message) => ApiEnvelope.Error(HttpContext, code, message);
}
