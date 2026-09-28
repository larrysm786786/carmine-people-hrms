using Asp.Versioning;
using CarminePeople.Domain.Constants;
using CarminePeople.Api.Common;
using CarminePeople.Application.Organization.DTOs;
using CarminePeople.Application.Organization.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarminePeople.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Authorize]
public class OrganizationController : ControllerBase
{
    private readonly OrganizationService _organizationService;

    public OrganizationController(OrganizationService organizationService)
    {
        _organizationService = organizationService;
    }

    // Departments
    [HttpGet("api/v{version:apiVersion}/departments")]
    public async Task<IActionResult> GetDepartments(CancellationToken ct)
    {
        var items = await _organizationService.GetDepartmentsAsync(ct);
        return Ok(new { success = true, data = items });
    }

    [HttpPost("api/v{version:apiVersion}/departments")]
    [Authorize(Policy = Permissions.DepartmentManage)]
    public async Task<IActionResult> CreateDepartment([FromBody] CreateDepartmentRequest request, CancellationToken ct)
    {
        var result = await _organizationService.CreateDepartmentAsync(request, User.GetEmployeeId(), ct);
        if (!result.Success) return BadRequest(Envelope(result.ErrorCode!, result.ErrorMessage!));
        return Ok(new { success = true, data = result.Data });
    }

    [HttpPut("api/v{version:apiVersion}/departments/{id:guid}")]
    [Authorize(Policy = Permissions.DepartmentManage)]
    public async Task<IActionResult> UpdateDepartment(Guid id, [FromBody] UpdateDepartmentRequest request, CancellationToken ct)
    {
        var result = await _organizationService.UpdateDepartmentAsync(id, request, User.GetEmployeeId(), ct);
        if (!result.Success) return NotFound(Envelope(result.ErrorCode!, result.ErrorMessage!));
        return Ok(new { success = true, data = result.Data });
    }

    [HttpDelete("api/v{version:apiVersion}/departments/{id:guid}")]
    [Authorize(Policy = Permissions.DepartmentManage)]
    public async Task<IActionResult> DeleteDepartment(Guid id, CancellationToken ct)
    {
        var result = await _organizationService.DeleteDepartmentAsync(id, User.GetEmployeeId(), ct);
        if (!result.Success) return BadRequest(Envelope(result.ErrorCode!, result.ErrorMessage!));
        return Ok(new { success = true, data = (object?)null });
    }

    // Designations
    [HttpGet("api/v{version:apiVersion}/designations")]
    public async Task<IActionResult> GetDesignations([FromQuery] Guid? departmentId, CancellationToken ct)
    {
        var items = await _organizationService.GetDesignationsAsync(departmentId, ct);
        return Ok(new { success = true, data = items });
    }

    [HttpPost("api/v{version:apiVersion}/designations")]
    [Authorize(Policy = Permissions.DepartmentManage)]
    public async Task<IActionResult> CreateDesignation([FromBody] CreateDesignationRequest request, CancellationToken ct)
    {
        var result = await _organizationService.CreateDesignationAsync(request, User.GetEmployeeId(), ct);
        if (!result.Success) return BadRequest(Envelope(result.ErrorCode!, result.ErrorMessage!));
        return Ok(new { success = true, data = result.Data });
    }

    [HttpPut("api/v{version:apiVersion}/designations/{id:guid}")]
    [Authorize(Policy = Permissions.DepartmentManage)]
    public async Task<IActionResult> UpdateDesignation(Guid id, [FromBody] UpdateDesignationRequest request, CancellationToken ct)
    {
        var result = await _organizationService.UpdateDesignationAsync(id, request, User.GetEmployeeId(), ct);
        if (!result.Success) return NotFound(Envelope(result.ErrorCode!, result.ErrorMessage!));
        return Ok(new { success = true, data = result.Data });
    }

    [HttpDelete("api/v{version:apiVersion}/designations/{id:guid}")]
    [Authorize(Policy = Permissions.DepartmentManage)]
    public async Task<IActionResult> DeleteDesignation(Guid id, CancellationToken ct)
    {
        var result = await _organizationService.DeleteDesignationAsync(id, User.GetEmployeeId(), ct);
        if (!result.Success) return BadRequest(Envelope(result.ErrorCode!, result.ErrorMessage!));
        return Ok(new { success = true, data = (object?)null });
    }

    // Locations
    [HttpGet("api/v{version:apiVersion}/locations")]
    public async Task<IActionResult> GetLocations(CancellationToken ct)
    {
        var items = await _organizationService.GetLocationsAsync(ct);
        return Ok(new { success = true, data = items });
    }

    [HttpPost("api/v{version:apiVersion}/locations")]
    [Authorize(Policy = Permissions.DepartmentManage)]
    public async Task<IActionResult> CreateLocation([FromBody] CreateLocationRequest request, CancellationToken ct)
    {
        var result = await _organizationService.CreateLocationAsync(request, User.GetEmployeeId(), ct);
        return Ok(new { success = true, data = result });
    }

    [HttpPut("api/v{version:apiVersion}/locations/{id:guid}")]
    [Authorize(Policy = Permissions.DepartmentManage)]
    public async Task<IActionResult> UpdateLocation(Guid id, [FromBody] UpdateLocationRequest request, CancellationToken ct)
    {
        var result = await _organizationService.UpdateLocationAsync(id, request, User.GetEmployeeId(), ct);
        if (!result.Success) return NotFound(Envelope(result.ErrorCode!, result.ErrorMessage!));
        return Ok(new { success = true, data = result.Data });
    }

    [HttpDelete("api/v{version:apiVersion}/locations/{id:guid}")]
    [Authorize(Policy = Permissions.DepartmentManage)]
    public async Task<IActionResult> DeleteLocation(Guid id, CancellationToken ct)
    {
        var result = await _organizationService.DeleteLocationAsync(id, User.GetEmployeeId(), ct);
        if (!result.Success) return BadRequest(Envelope(result.ErrorCode!, result.ErrorMessage!));
        return Ok(new { success = true, data = (object?)null });
    }

    // Shifts
    [HttpGet("api/v{version:apiVersion}/shifts")]
    public async Task<IActionResult> GetShifts(CancellationToken ct)
    {
        var items = await _organizationService.GetShiftsAsync(ct);
        return Ok(new { success = true, data = items });
    }

    [HttpPost("api/v{version:apiVersion}/shifts")]
    [Authorize(Policy = Permissions.DepartmentManage)]
    public async Task<IActionResult> CreateShift([FromBody] CreateShiftRequest request, CancellationToken ct)
    {
        var result = await _organizationService.CreateShiftAsync(request, User.GetEmployeeId(), ct);
        return Ok(new { success = true, data = result });
    }

    [HttpPut("api/v{version:apiVersion}/shifts/{id:guid}")]
    [Authorize(Policy = Permissions.DepartmentManage)]
    public async Task<IActionResult> UpdateShift(Guid id, [FromBody] UpdateShiftRequest request, CancellationToken ct)
    {
        var result = await _organizationService.UpdateShiftAsync(id, request, User.GetEmployeeId(), ct);
        if (!result.Success) return NotFound(Envelope(result.ErrorCode!, result.ErrorMessage!));
        return Ok(new { success = true, data = result.Data });
    }

    [HttpPost("api/v{version:apiVersion}/shifts/assign")]
    [Authorize(Policy = Permissions.DepartmentManage)]
    public async Task<IActionResult> AssignShift([FromBody] AssignShiftRequest request, CancellationToken ct)
    {
        var result = await _organizationService.AssignShiftAsync(request, User.GetEmployeeId(), ct);
        if (!result.Success) return BadRequest(Envelope(result.ErrorCode!, result.ErrorMessage!));
        return Ok(new { success = true, data = result.Data });
    }

    [HttpGet("api/v{version:apiVersion}/employee-shifts")]
    [Authorize(Policy = Permissions.DepartmentManage)]
    public async Task<IActionResult> GetEmployeeShifts(CancellationToken ct)
    {
        var items = await _organizationService.GetEmployeeShiftsAsync(ct);
        return Ok(new { success = true, data = items });
    }

    private object Envelope(string code, string message) => Api.Common.ApiEnvelope.Error(HttpContext, code, message);
}
