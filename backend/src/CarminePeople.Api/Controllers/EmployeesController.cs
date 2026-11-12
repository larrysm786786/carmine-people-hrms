using Asp.Versioning;
using CarminePeople.Domain.Constants;
using CarminePeople.Api.Common;
using CarminePeople.Application.Attendance.DTOs;
using CarminePeople.Application.Documents.Services;
using CarminePeople.Application.Employees.DTOs;
using CarminePeople.Application.Employees.Services;
using CarminePeople.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarminePeople.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/employees")]
[Authorize]
public class EmployeesController : ControllerBase
{
    private readonly EmployeeService _employeeService;
    private readonly DocumentService _documentService;
    private readonly FaceEnrollmentService _faceEnrollmentService;

    public EmployeesController(EmployeeService employeeService, DocumentService documentService, FaceEnrollmentService faceEnrollmentService)
    {
        _documentService = documentService;
        _employeeService = employeeService;
        _faceEnrollmentService = faceEnrollmentService;
    }

    // Every authenticated employee may view the org chart / upcoming celebrations —
    // deliberately not gated behind employee.view, which would also unlock browsing
    // the full flat directory (a broader grant than "see who reports to whom").
    [HttpGet("org-chart")]
    public async Task<IActionResult> GetOrgChart([FromQuery] Guid? rootId, CancellationToken ct)
    {
        var chart = await _employeeService.GetOrgChartAsync(rootId, ct);
        return Ok(new { success = true, data = chart });
    }

    [HttpGet("celebrations")]
    public async Task<IActionResult> GetCelebrations([FromQuery] int withinDays = 30, CancellationToken ct = default)
    {
        var celebrations = await _employeeService.GetUpcomingCelebrationsAsync(withinDays, ct);
        return Ok(new { success = true, data = celebrations });
    }

    // Managers see only their direct reports (their own manager ID is force-applied below
    // regardless of any managerId query value they pass); HR/SuperAdmin see the full company
    // via employee.view and may optionally filter by managerId themselves.
    [HttpGet]
    [Authorize(Policy = Permissions.EmployeeView)]
    public async Task<IActionResult> GetList(
        [FromQuery] string? search, [FromQuery] Guid? departmentId, [FromQuery] Guid? locationId,
        [FromQuery] EmploymentStatus? status, [FromQuery] Guid? managerId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] string? sortBy = null, [FromQuery] string? sortDirection = null,
        CancellationToken ct = default)
    {
        var effectiveManagerId = User.IsInRole("Manager") ? User.GetEmployeeId() : managerId;
        var result = await _employeeService.GetListAsync(
            new EmployeeQuery(search, departmentId, locationId, status, effectiveManagerId, page, pageSize, sortBy, sortDirection), ct);
        return Ok(new { success = true, data = result.Items, meta = new { result.Page, result.PageSize, result.Total } });
    }

    // Any employee may view their own record; viewing someone else's requires employee.view
    // (HR/Manager/SuperAdmin), so a plain Employee can see their own profile without the
    // broader permission that would also unlock browsing the entire company directory.
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        if (id != User.GetEmployeeId() && !User.HasClaim("permission", Permissions.EmployeeView)) return Forbid();
        var result = await _employeeService.GetByIdAsync(id, ct);
        if (!result.Success) return NotFound(Envelope(result.ErrorCode!, result.ErrorMessage!));
        return Ok(new { success = true, data = result.Data });
    }

    [HttpGet("{id:guid}/documents")]
    public async Task<IActionResult> GetDocuments(Guid id, CancellationToken ct)
    {
        if (id != User.GetEmployeeId() && !User.HasClaim("permission", Permissions.EmployeeView)) return Forbid();
        var documents = await _documentService.GetForEmployeeAsync(id, ct);
        return Ok(new { success = true, data = documents });
    }

    // Self-service: any authenticated employee may view/update their own contact details,
    // without needing the employee.manage permission required for HR-managed fields.
    [HttpPut("me")]
    public async Task<IActionResult> UpdateSelf([FromBody] UpdateSelfProfileRequest request, CancellationToken ct)
    {
        var result = await _employeeService.UpdateSelfAsync(User.GetEmployeeId(), request, ct);
        if (!result.Success) return NotFound(Envelope(result.ErrorCode!, result.ErrorMessage!));
        return Ok(new { success = true, data = result.Data });
    }

    // Client uploads the photo via POST /documents/upload/profile-photo first, then points
    // ProfileImageUrl at the returned document id with this call.
    [HttpPut("me/profile-image")]
    public async Task<IActionResult> UpdateProfileImage([FromBody] UpdateProfileImageRequest request, CancellationToken ct)
    {
        var result = await _employeeService.UpdateProfileImageAsync(User.GetEmployeeId(), request, ct);
        if (!result.Success) return NotFound(Envelope(result.ErrorCode!, result.ErrorMessage!));
        return Ok(new { success = true, data = result.Data });
    }

    [HttpPost]
    [Authorize(Policy = Permissions.EmployeeManage)]
    public async Task<IActionResult> Create([FromBody] CreateEmployeeRequest request, CancellationToken ct)
    {
        var result = await _employeeService.CreateAsync(request, User.GetEmployeeId(), ct);
        if (!result.Success) return Conflict(Envelope(result.ErrorCode!, result.ErrorMessage!));
        return CreatedAtAction(nameof(GetById), new { id = result.Data!.Id, version = "1.0" }, new { success = true, data = result.Data });
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Permissions.EmployeeManage)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateEmployeeRequest request, CancellationToken ct)
    {
        var result = await _employeeService.UpdateAsync(id, request, User.GetEmployeeId(), ct);
        if (!result.Success) return NotFound(Envelope(result.ErrorCode!, result.ErrorMessage!));
        return Ok(new { success = true, data = result.Data });
    }

    [HttpPut("{id:guid}/password")]
    [Authorize(Policy = Permissions.EmployeeManage)]
    public async Task<IActionResult> SetPassword(Guid id, [FromBody] SetEmployeePasswordRequest request, CancellationToken ct)
    {
        var result = await _employeeService.SetPasswordAsync(id, request, User.GetEmployeeId(), ct);
        if (!result.Success) return NotFound(Envelope(result.ErrorCode!, result.ErrorMessage!));
        return Ok(new { success = true, data = (object?)null });
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Permissions.EmployeeManage)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await _employeeService.SoftDeleteAsync(id, User.GetEmployeeId(), ct);
        if (!result.Success) return NotFound(Envelope(result.ErrorCode!, result.ErrorMessage!));
        return Ok(new { success = true, data = (object?)null });
    }

    // Write is HR/Admin-gated since capture happens via a dedicated mobile enrollment
    // screen; read reuses employee.view so the admin panel's read-only status badge
    // doesn't need a separate permission just to display enrollment state.
    [HttpPost("{id:guid}/face-enrollment")]
    [Authorize(Policy = Permissions.FaceEnroll)]
    public async Task<IActionResult> EnrollFace(Guid id, [FromBody] FaceEnrollmentRequest request, CancellationToken ct)
    {
        var result = await _faceEnrollmentService.EnrollAsync(id, request, User.GetEmployeeId(), ct);
        if (!result.Success) return BadRequest(Envelope(result.ErrorCode!, result.ErrorMessage!));
        return Ok(new { success = true, data = result.Data });
    }

    // Any employee needs to check their OWN enrollment status before the face-checkin flow
    // decides whether to prompt the camera at all — requiring employee.view here would mean
    // a plain Employee (who deliberately doesn't hold that permission) gets a 403 on every
    // single check-in attempt, silently falling back to GPS-only every time.
    [HttpGet("{id:guid}/face-enrollment")]
    public async Task<IActionResult> GetFaceEnrollmentStatus(Guid id, CancellationToken ct)
    {
        if (id != User.GetEmployeeId() && !User.HasClaim("permission", Permissions.EmployeeView)) return Forbid();
        var result = await _faceEnrollmentService.GetStatusAsync(id, ct);
        if (!result.Success) return NotFound(Envelope(result.ErrorCode!, result.ErrorMessage!));
        return Ok(new { success = true, data = result.Data });
    }

    private object Envelope(string code, string message) => Api.Common.ApiEnvelope.Error(HttpContext, code, message);
}
