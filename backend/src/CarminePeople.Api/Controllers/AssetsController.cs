using Asp.Versioning;
using CarminePeople.Api.Common;
using CarminePeople.Application.Assets.DTOs;
using CarminePeople.Application.Assets.Services;
using CarminePeople.Domain.Constants;
using CarminePeople.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarminePeople.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/assets")]
[Authorize]
public class AssetsController : ControllerBase
{
    private readonly AssetService _assetService;

    public AssetsController(AssetService assetService)
    {
        _assetService = assetService;
    }

    [HttpGet]
    [Authorize(Policy = Permissions.EmployeeManage)]
    public async Task<IActionResult> GetList([FromQuery] string? category, [FromQuery] AssetStatus? status, [FromQuery] Guid? employeeId, CancellationToken ct)
    {
        var result = await _assetService.GetListAsync(new AssetQuery(category, status, employeeId), ct);
        return Ok(new { success = true, data = result });
    }

    [HttpPost]
    [Authorize(Policy = Permissions.EmployeeManage)]
    public async Task<IActionResult> Create([FromBody] CreateAssetRequest request, CancellationToken ct)
    {
        var result = await _assetService.CreateAsync(request, User.GetEmployeeId(), ct);
        if (!result.Success) return Conflict(ApiEnvelope.Error(HttpContext, result.ErrorCode!, result.ErrorMessage!));
        return Ok(new { success = true, data = result.Data });
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Permissions.EmployeeManage)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateAssetRequest request, CancellationToken ct)
    {
        var result = await _assetService.UpdateAsync(id, request, User.GetEmployeeId(), ct);
        if (!result.Success) return NotFound(ApiEnvelope.Error(HttpContext, result.ErrorCode!, result.ErrorMessage!));
        return Ok(new { success = true, data = result.Data });
    }

    [HttpPost("{id:guid}/assign")]
    [Authorize(Policy = Permissions.EmployeeManage)]
    public async Task<IActionResult> Assign(Guid id, [FromBody] AssignAssetRequest request, CancellationToken ct)
    {
        var result = await _assetService.AssignAsync(id, request, User.GetEmployeeId(), ct);
        if (!result.Success) return Conflict(ApiEnvelope.Error(HttpContext, result.ErrorCode!, result.ErrorMessage!));
        return Ok(new { success = true, data = result.Data });
    }

    [HttpPost("{id:guid}/unassign")]
    [Authorize(Policy = Permissions.EmployeeManage)]
    public async Task<IActionResult> Unassign(Guid id, CancellationToken ct)
    {
        var result = await _assetService.UnassignAsync(id, User.GetEmployeeId(), ct);
        if (!result.Success) return NotFound(ApiEnvelope.Error(HttpContext, result.ErrorCode!, result.ErrorMessage!));
        return Ok(new { success = true, data = result.Data });
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Permissions.EmployeeManage)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await _assetService.DeleteAsync(id, User.GetEmployeeId(), ct);
        if (!result.Success) return NotFound(ApiEnvelope.Error(HttpContext, result.ErrorCode!, result.ErrorMessage!));
        return Ok(new { success = true, data = (object?)null });
    }
}
