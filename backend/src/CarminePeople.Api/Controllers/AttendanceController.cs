using Asp.Versioning;
using CarminePeople.Domain.Constants;
using CarminePeople.Api.Common;
using CarminePeople.Application.Attendance.DTOs;
using CarminePeople.Application.Attendance.Services;
using CarminePeople.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarminePeople.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/attendance")]
[Authorize]
public class AttendanceController : ControllerBase
{
    private readonly AttendanceService _attendanceService;

    public AttendanceController(AttendanceService attendanceService)
    {
        _attendanceService = attendanceService;
    }

    [HttpPost("check-in")]
    public async Task<IActionResult> CheckIn([FromBody] CheckInRequest request, CancellationToken ct)
    {
        var result = await _attendanceService.CheckInAsync(User.GetEmployeeId(), request, ct);
        if (!result.Success) return BadRequest(Envelope(result.ErrorCode!, result.ErrorMessage!));
        return Ok(new { success = true, data = result.Data });
    }

    [HttpPost("check-out")]
    public async Task<IActionResult> CheckOut([FromBody] CheckOutRequest request, CancellationToken ct)
    {
        var result = await _attendanceService.CheckOutAsync(User.GetEmployeeId(), request, ct);
        if (!result.Success) return BadRequest(Envelope(result.ErrorCode!, result.ErrorMessage!));
        return Ok(new { success = true, data = result.Data });
    }

    [HttpPost("break-start")]
    public async Task<IActionResult> BreakStart(CancellationToken ct)
    {
        var result = await _attendanceService.StartBreakAsync(User.GetEmployeeId(), ct);
        if (!result.Success) return BadRequest(Envelope(result.ErrorCode!, result.ErrorMessage!));
        return Ok(new { success = true, data = result.Data });
    }

    [HttpPost("break-end")]
    public async Task<IActionResult> BreakEnd(CancellationToken ct)
    {
        var result = await _attendanceService.EndBreakAsync(User.GetEmployeeId(), ct);
        if (!result.Success) return BadRequest(Envelope(result.ErrorCode!, result.ErrorMessage!));
        return Ok(new { success = true, data = result.Data });
    }

    [HttpGet("today")]
    public async Task<IActionResult> GetToday(CancellationToken ct)
    {
        var record = await _attendanceService.GetTodayAsync(User.GetEmployeeId(), ct);
        return Ok(new { success = true, data = record });
    }

    // Powers the mobile home screen's "Today's Schedule" and "Monthly Summary" cards.
    [HttpGet("home-summary")]
    public async Task<IActionResult> GetHomeSummary(CancellationToken ct)
    {
        var summary = await _attendanceService.GetHomeSummaryAsync(User.GetEmployeeId(), ct);
        return Ok(new { success = true, data = summary });
    }

    // Powers the mobile app's WiFi auto-check-in prompt: the employee's assigned office's
    // WiFi network name, so the app can compare it against the phone's currently-connected
    // SSID and suggest a check-in — never trusted as proof of presence by itself; the actual
    // check-in still goes through CheckInAsync's GPS geofence check regardless of how it
    // was triggered.
    [HttpGet("office-wifi")]
    public async Task<IActionResult> GetOfficeWifi(CancellationToken ct)
    {
        var ssid = await _attendanceService.GetOfficeWifiSsidAsync(User.GetEmployeeId(), ct);
        return Ok(new { success = true, data = new { ssid } });
    }

    [HttpGet("team")]
    [Authorize(Policy = Permissions.AttendanceView)]
    public async Task<IActionResult> GetTeam([FromQuery] DateOnly? date, CancellationToken ct)
    {
        var records = await _attendanceService.GetTeamAsync(User.GetEmployeeId(), date, ct);
        return Ok(new { success = true, data = records });
    }

    // attendance.view is deliberately granted to every role, including plain Employee, so
    // each employee can see their OWN attendance history via this endpoint — it is NOT a
    // "see everyone's attendance" permission. Only HR/SuperAdmin may omit employeeId (or
    // pass someone else's) to see company-wide records; anyone else querying without an
    // employeeId, or with someone else's, is silently redirected to their own — this
    // previously let any employee see every other employee's attendance by simply not
    // passing an employeeId filter.
    [HttpGet]
    [Authorize(Policy = Permissions.AttendanceView)]
    public async Task<IActionResult> GetList(
        [FromQuery] Guid? employeeId, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to,
        [FromQuery] AttendanceStatus? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var isPrivileged = await _attendanceService.IsHrOrSuperAdminAsync(User.GetEmployeeId(), ct);
        var scopedEmployeeId = isPrivileged ? employeeId : User.GetEmployeeId();
        var result = await _attendanceService.GetListAsync(new AttendanceQuery(scopedEmployeeId, from, to, status, page, pageSize), ct);
        return Ok(new { success = true, data = result.Items, meta = new { result.Page, result.PageSize, result.Total } });
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Permissions.AttendanceView)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _attendanceService.GetByIdAsync(id, ct);
        if (!result.Success) return NotFound(Envelope(result.ErrorCode!, result.ErrorMessage!));
        return Ok(new { success = true, data = result.Data });
    }

    [HttpPost("{id:guid}/regularize")]
    public async Task<IActionResult> Regularize(Guid id, [FromBody] RegularizeRequest request, CancellationToken ct)
    {
        var result = await _attendanceService.RequestRegularizationAsync(User.GetEmployeeId(), id, request, ct);
        if (!result.Success) return BadRequest(Envelope(result.ErrorCode!, result.ErrorMessage!));
        return Ok(new { success = true, data = new { regularizationId = result.Data } });
    }

    [HttpPut("regularization/{id:guid}/approve")]
    [Authorize(Policy = Permissions.AttendanceApprove)]
    public async Task<IActionResult> ApproveRegularization(Guid id, [FromBody] RegularizationDecisionRequest request, CancellationToken ct)
    {
        var isHr = User.IsInRole("HRAdmin") || User.IsInRole("SuperAdmin");
        var decision = isHr ? RegularizationStatus.HRApproved : RegularizationStatus.ManagerApproved;
        var result = await _attendanceService.DecideRegularizationAsync(id, User.GetEmployeeId(), decision, request.Comment, ct);
        if (!result.Success) return DecisionError(result.ErrorCode!, result.ErrorMessage!);
        return Ok(new { success = true, data = (object?)null });
    }

    [HttpPut("regularization/{id:guid}/reject")]
    [Authorize(Policy = Permissions.AttendanceApprove)]
    public async Task<IActionResult> RejectRegularization(Guid id, [FromBody] RegularizationDecisionRequest request, CancellationToken ct)
    {
        var result = await _attendanceService.DecideRegularizationAsync(id, User.GetEmployeeId(), RegularizationStatus.Rejected, request.Comment, ct);
        if (!result.Success) return DecisionError(result.ErrorCode!, result.ErrorMessage!);
        return Ok(new { success = true, data = (object?)null });
    }

    private IActionResult DecisionError(string code, string message) =>
        code == "FORBIDDEN" ? StatusCode(403, Envelope(code, message)) : BadRequest(Envelope(code, message));

    private object Envelope(string code, string message) => Api.Common.ApiEnvelope.Error(HttpContext, code, message);
}
