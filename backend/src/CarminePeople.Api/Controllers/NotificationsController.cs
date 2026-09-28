using Asp.Versioning;
using CarminePeople.Api.Common;
using CarminePeople.Application.Notifications.DTOs;
using CarminePeople.Application.Notifications.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarminePeople.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/notifications")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly NotificationService _notificationService;

    public NotificationsController(NotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    [HttpGet]
    public async Task<IActionResult> GetList([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await _notificationService.GetListAsync(User.GetEmployeeId(), page, pageSize, ct);
        return Ok(new { success = true, data = result.Items, meta = new { result.Page, result.PageSize, result.Total } });
    }

    [HttpPut("{id:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid id, CancellationToken ct)
    {
        var result = await _notificationService.MarkReadAsync(User.GetEmployeeId(), id, ct);
        if (!result.Success) return NotFound(new { success = false, error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return Ok(new { success = true, data = (object?)null });
    }

    [HttpGet("preferences")]
    public async Task<IActionResult> GetPreferences(CancellationToken ct)
    {
        var prefs = await _notificationService.GetPreferencesAsync(User.GetEmployeeId(), ct);
        return Ok(new { success = true, data = prefs });
    }

    [HttpPut("preferences")]
    public async Task<IActionResult> SetPreference([FromBody] SetPreferenceRequest request, CancellationToken ct)
    {
        await _notificationService.SetPreferenceAsync(User.GetEmployeeId(), request, ct);
        return Ok(new { success = true, data = (object?)null });
    }

    [HttpPost("devices")]
    public async Task<IActionResult> RegisterDevice([FromBody] RegisterDeviceRequest request, CancellationToken ct)
    {
        await _notificationService.RegisterDeviceAsync(User.GetEmployeeId(), request, ct);
        return Ok(new { success = true, data = (object?)null });
    }
}
