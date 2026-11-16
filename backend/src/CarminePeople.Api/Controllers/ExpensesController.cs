using Asp.Versioning;
using CarminePeople.Api.Common;
using CarminePeople.Application.Expenses.DTOs;
using CarminePeople.Application.Expenses.Services;
using CarminePeople.Domain.Constants;
using CarminePeople.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarminePeople.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/expenses")]
[Authorize]
public class ExpensesController : ControllerBase
{
    private readonly ExpenseService _expenseService;

    public ExpensesController(ExpenseService expenseService)
    {
        _expenseService = expenseService;
    }

    [HttpPost]
    public async Task<IActionResult> Submit([FromBody] SubmitExpenseRequest request, CancellationToken ct)
    {
        var result = await _expenseService.SubmitAsync(User.GetEmployeeId(), request, ct);
        if (!result.Success) return BadRequest(Envelope(result.ErrorCode!, result.ErrorMessage!));
        return Ok(new { success = true, data = result.Data });
    }

    [HttpGet]
    [Authorize(Policy = Permissions.PayrollView)]
    public async Task<IActionResult> GetList(
        [FromQuery] Guid? employeeId, [FromQuery] ExpenseStatus? status,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await _expenseService.GetListAsync(new ExpenseQuery(employeeId, status, page, pageSize), ct);
        return Ok(new { success = true, data = result.Items, meta = new { result.Page, result.PageSize, result.Total } });
    }

    [HttpGet("mine")]
    public async Task<IActionResult> GetMine([FromQuery] ExpenseStatus? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await _expenseService.GetListAsync(new ExpenseQuery(User.GetEmployeeId(), status, page, pageSize), ct);
        return Ok(new { success = true, data = result.Items, meta = new { result.Page, result.PageSize, result.Total } });
    }

    [HttpGet("team-pending")]
    [Authorize(Policy = Permissions.LeaveApprove)]
    public async Task<IActionResult> GetTeamPending([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var unscopedVisibility = User.HasClaim("permission", Permissions.EmployeeManage);
        var result = await _expenseService.GetTeamPendingAsync(User.GetEmployeeId(), unscopedVisibility, page, pageSize, ct);
        return Ok(new { success = true, data = result.Items, meta = new { result.Page, result.PageSize, result.Total } });
    }

    [HttpPut("{id:guid}/approve")]
    [Authorize(Policy = Permissions.LeaveApprove)]
    public async Task<IActionResult> Approve(Guid id, [FromBody] ExpenseDecisionRequest request, CancellationToken ct)
    {
        var result = await _expenseService.ApproveAsync(id, User.GetEmployeeId(), request.Comment, ct);
        if (!result.Success) return BadRequest(Envelope(result.ErrorCode!, result.ErrorMessage!));
        return Ok(new { success = true, data = (object?)null });
    }

    [HttpPut("{id:guid}/reject")]
    [Authorize(Policy = Permissions.LeaveApprove)]
    public async Task<IActionResult> Reject(Guid id, [FromBody] ExpenseDecisionRequest request, CancellationToken ct)
    {
        var result = await _expenseService.RejectAsync(id, User.GetEmployeeId(), request.Comment, ct);
        if (!result.Success) return BadRequest(Envelope(result.ErrorCode!, result.ErrorMessage!));
        return Ok(new { success = true, data = (object?)null });
    }

    [HttpPut("{id:guid}/reimburse")]
    [Authorize(Policy = Permissions.PayrollManage)]
    public async Task<IActionResult> Reimburse(Guid id, CancellationToken ct)
    {
        var result = await _expenseService.ReimburseAsync(id, User.GetEmployeeId(), ct);
        if (!result.Success) return BadRequest(Envelope(result.ErrorCode!, result.ErrorMessage!));
        return Ok(new { success = true, data = (object?)null });
    }

    private object Envelope(string code, string message) => ApiEnvelope.Error(HttpContext, code, message);
}
