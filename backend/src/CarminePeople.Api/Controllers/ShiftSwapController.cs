using Asp.Versioning;
using CarminePeople.Api.Common;
using CarminePeople.Application.ShiftSwap.DTOs;
using CarminePeople.Application.ShiftSwap.Services;
using CarminePeople.Domain.Constants;
using CarminePeople.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarminePeople.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/shift-swaps")]
[Authorize]
public class ShiftSwapController : ControllerBase
{
    private readonly ShiftSwapService _shiftSwapService;

    public ShiftSwapController(ShiftSwapService shiftSwapService)
    {
        _shiftSwapService = shiftSwapService;
    }

    [HttpPost]
    [Authorize(Policy = Permissions.ShiftSwapApply)]
    public async Task<IActionResult> Create([FromBody] RequestShiftSwapRequest request, CancellationToken ct)
    {
        var result = await _shiftSwapService.RequestAsync(User.GetEmployeeId(), request, ct);
        if (!result.Success) return BadRequest(Envelope(result.ErrorCode!, result.ErrorMessage!));
        return Ok(new { success = true, data = result.Data });
    }

    // Always scoped to the caller via the JWT (either side of the swap) — the mobile app's
    // own "My Shift Swaps" screen must call this, not GetList below, or a manager/HR viewer
    // would see every employee's requests on what is meant to be their own personal history.
    [HttpGet("mine")]
    public async Task<IActionResult> GetMine([FromQuery] ShiftSwapStatus? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await _shiftSwapService.GetListAsync(new ShiftSwapQuery(User.GetEmployeeId(), status, page, pageSize), ct);
        return Ok(new { success = true, data = result.Items, meta = new { result.Page, result.PageSize, result.Total } });
    }

    // Company-wide (or a specific employee's, via employeeId) view for HR/managers — omitting
    // employeeId returns every employee's requests, not just the caller's own.
    [HttpGet]
    [Authorize(Policy = Permissions.ShiftSwapApprove)]
    public async Task<IActionResult> GetList(
        [FromQuery] Guid? employeeId, [FromQuery] ShiftSwapStatus? status,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await _shiftSwapService.GetListAsync(new ShiftSwapQuery(employeeId, status, page, pageSize), ct);
        return Ok(new { success = true, data = result.Items, meta = new { result.Page, result.PageSize, result.Total } });
    }

    [HttpGet("team-pending")]
    [Authorize(Policy = Permissions.ShiftSwapApprove)]
    public async Task<IActionResult> GetTeamPending([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var unscopedVisibility = User.HasClaim("permission", Permissions.EmployeeManage);
        var result = await _shiftSwapService.GetTeamPendingAsync(User.GetEmployeeId(), unscopedVisibility, page, pageSize, ct);
        return Ok(new { success = true, data = result.Items, meta = new { result.Page, result.PageSize, result.Total } });
    }

    [HttpPut("{id:guid}/respond")]
    public async Task<IActionResult> Respond(Guid id, [FromQuery] bool accept, CancellationToken ct)
    {
        var result = await _shiftSwapService.RespondAsync(id, User.GetEmployeeId(), accept, ct);
        if (!result.Success) return DecisionError(result.ErrorCode!, result.ErrorMessage!);
        return Ok(new { success = true, data = (object?)null });
    }

    [HttpPut("{id:guid}/approve")]
    [Authorize(Policy = Permissions.ShiftSwapApprove)]
    public async Task<IActionResult> Approve(Guid id, [FromBody] ShiftSwapDecisionRequest request, CancellationToken ct)
    {
        var result = await _shiftSwapService.ApproveAsync(id, User.GetEmployeeId(), request.Comment, ct);
        if (!result.Success) return DecisionError(result.ErrorCode!, result.ErrorMessage!);
        return Ok(new { success = true, data = (object?)null });
    }

    [HttpPut("{id:guid}/reject")]
    [Authorize(Policy = Permissions.ShiftSwapApprove)]
    public async Task<IActionResult> Reject(Guid id, [FromBody] ShiftSwapDecisionRequest request, CancellationToken ct)
    {
        var result = await _shiftSwapService.RejectAsync(id, User.GetEmployeeId(), request.Comment, ct);
        if (!result.Success) return DecisionError(result.ErrorCode!, result.ErrorMessage!);
        return Ok(new { success = true, data = (object?)null });
    }

    [HttpPut("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
    {
        var result = await _shiftSwapService.CancelAsync(id, User.GetEmployeeId(), ct);
        if (!result.Success) return BadRequest(Envelope(result.ErrorCode!, result.ErrorMessage!));
        return Ok(new { success = true, data = (object?)null });
    }

    private IActionResult DecisionError(string code, string message) =>
        code == "FORBIDDEN" ? StatusCode(403, Envelope(code, message)) : BadRequest(Envelope(code, message));

    private object Envelope(string code, string message) => ApiEnvelope.Error(HttpContext, code, message);
}
