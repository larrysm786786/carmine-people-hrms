using Asp.Versioning;
using CarminePeople.Domain.Constants;
using CarminePeople.Api.Common;
using CarminePeople.Application.Roles.DTOs;
using CarminePeople.Application.Roles.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarminePeople.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/roles")]
[Authorize(Policy = Permissions.RoleManage)]
public class RolesController : ControllerBase
{
    private readonly RoleService _roleService;

    public RolesController(RoleService roleService)
    {
        _roleService = roleService;
    }

    [HttpGet]
    public async Task<IActionResult> GetRoles(CancellationToken ct)
    {
        var roles = await _roleService.GetRolesAsync(ct);
        return Ok(new { success = true, data = roles });
    }

    [HttpGet("permissions")]
    public async Task<IActionResult> GetPermissions(CancellationToken ct)
    {
        var permissions = await _roleService.GetPermissionsAsync(ct);
        return Ok(new { success = true, data = permissions });
    }

    [HttpPost]
    public async Task<IActionResult> CreateRole([FromBody] CreateRoleRequest request, CancellationToken ct)
    {
        var result = await _roleService.CreateRoleAsync(request, User.GetEmployeeId(), ct);
        if (!result.Success) return Conflict(new { success = false, error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return Ok(new { success = true, data = result.Data });
    }

    [HttpPut("{id:guid}/permissions")]
    public async Task<IActionResult> SetPermissions(Guid id, [FromBody] SetRolePermissionsRequest request, CancellationToken ct)
    {
        var result = await _roleService.SetPermissionsAsync(id, request, User.GetEmployeeId(), ct);
        if (!result.Success) return NotFound(new { success = false, error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return Ok(new { success = true, data = result.Data });
    }
}
