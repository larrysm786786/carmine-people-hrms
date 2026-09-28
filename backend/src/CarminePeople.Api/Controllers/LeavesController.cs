using Asp.Versioning;
using CarminePeople.Domain.Constants;
using CarminePeople.Api.Common;
using CarminePeople.Application.Leave.DTOs;
using CarminePeople.Application.Leave.Services;
using CarminePeople.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarminePeople.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/leaves")]
[Authorize]
public class LeavesController : ControllerBase
{
    private readonly LeaveService _leaveService;

    public LeavesController(LeaveService leaveService)
    {
        _leaveService = leaveService;
    }

    [HttpGet("types")]
    public async Task<IActionResult> GetTypes(CancellationToken ct)
    {
        var types = await _leaveService.GetTypesAsync(ct);
        return Ok(new { success = true, data = types });
    }

    [HttpPost("types")]
    [Authorize(Policy = Permissions.LeaveTypeManage)]
    public async Task<IActionResult> CreateType([FromBody] CreateLeaveTypeRequest request, CancellationToken ct)
    {
        var result = await _leaveService.CreateTypeAsync(request, User.GetEmployeeId(), ct);
        if (!result.Success) return Conflict(Envelope(result.ErrorCode!, result.ErrorMessage!));
        return Ok(new { success = true, data = result.Data });
    }

    [HttpPut("types/{id:guid}")]
    [Authorize(Policy = Permissions.LeaveTypeManage)]
    public async Task<IActionResult> UpdateType(Guid id, [FromBody] UpdateLeaveTypeRequest request, CancellationToken ct)
    {
        var result = await _leaveService.UpdateTypeAsync(id, request, User.GetEmployeeId(), ct);
        if (!result.Success) return NotFound(Envelope(result.ErrorCode!, result.ErrorMessage!));
        return Ok(new { success = true, data = result.Data });
    }

    [HttpGet("balances")]
    public async Task<IActionResult> GetBalances([FromQuery] Guid? employeeId, [FromQuery] int? year, CancellationToken ct)
    {
        var targetEmployeeId = employeeId ?? User.GetEmployeeId();
        var balances = await _leaveService.GetBalancesAsync(targetEmployeeId, year, ct);
        return Ok(new { success = true, data = balances });
    }

    // Fills in each active employee's missing leave-type balances for a year, using each leave
    // type's DefaultAnnualDays. Existing balances are never overwritten, so this is safe to run
    // again (e.g. for the new year, or after a new leave type/employee is added).
    [HttpPost("balances/allocate-defaults")]
    [Authorize(Policy = Permissions.LeaveTypeManage)]
    public async Task<IActionResult> AllocateDefaultBalances([FromQuery] int? year, CancellationToken ct)
    {
        var created = await _leaveService.AllocateDefaultBalancesAsync(year, User.GetEmployeeId(), ct);
        return Ok(new { success = true, data = new { created } });
    }

    [HttpPost]
    public async Task<IActionResult> Apply([FromBody] ApplyLeaveRequest request, CancellationToken ct)
    {
        var result = await _leaveService.ApplyAsync(User.GetEmployeeId(), request, ct);
        if (!result.Success) return BadRequest(Envelope(result.ErrorCode!, result.ErrorMessage!));
        return Ok(new { success = true, data = result.Data });
    }

    [HttpGet]
    [Authorize(Policy = Permissions.LeaveView)]
    public async Task<IActionResult> GetList(
        [FromQuery] Guid? employeeId, [FromQuery] LeaveApplicationStatus? status, [FromQuery] Guid? leaveTypeId,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await _leaveService.GetListAsync(new LeaveApplicationQuery(employeeId, status, leaveTypeId, page, pageSize), ct);
        return Ok(new { success = true, data = result.Items, meta = new { result.Page, result.PageSize, result.Total } });
    }

    // Pending requests for the caller's own direct reports (or company-wide for HR/
    // SuperAdmin) — distinct from GetList, which only ever returns one employee's history.
    [HttpGet("team-pending")]
    [Authorize(Policy = Permissions.LeaveApprove)]
    public async Task<IActionResult> GetTeamPending([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var unscopedVisibility = User.HasClaim("permission", Permissions.EmployeeManage);
        var result = await _leaveService.GetTeamPendingAsync(User.GetEmployeeId(), unscopedVisibility, page, pageSize, ct);
        return Ok(new { success = true, data = result.Items, meta = new { result.Page, result.PageSize, result.Total } });
    }

    [HttpPut("{id:guid}/approve")]
    [Authorize(Policy = Permissions.LeaveApprove)]
    public async Task<IActionResult> Approve(Guid id, [FromBody] LeaveDecisionRequest request, CancellationToken ct)
    {
        var result = await _leaveService.ApproveAsync(id, User.GetEmployeeId(), request.Comment, ct);
        if (!result.Success) return DecisionError(result.ErrorCode!, result.ErrorMessage!);
        return Ok(new { success = true, data = (object?)null });
    }

    [HttpPut("{id:guid}/reject")]
    [Authorize(Policy = Permissions.LeaveApprove)]
    public async Task<IActionResult> Reject(Guid id, [FromBody] LeaveDecisionRequest request, CancellationToken ct)
    {
        var result = await _leaveService.RejectAsync(id, User.GetEmployeeId(), request.Comment, ct);
        if (!result.Success) return DecisionError(result.ErrorCode!, result.ErrorMessage!);
        return Ok(new { success = true, data = (object?)null });
    }

    private IActionResult DecisionError(string code, string message) =>
        code == "FORBIDDEN" ? StatusCode(403, Envelope(code, message)) : BadRequest(Envelope(code, message));

    [HttpPut("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
    {
        var result = await _leaveService.CancelAsync(id, User.GetEmployeeId(), ct);
        if (!result.Success) return BadRequest(Envelope(result.ErrorCode!, result.ErrorMessage!));
        return Ok(new { success = true, data = (object?)null });
    }

    private object Envelope(string code, string message) => Api.Common.ApiEnvelope.Error(HttpContext, code, message);
}
