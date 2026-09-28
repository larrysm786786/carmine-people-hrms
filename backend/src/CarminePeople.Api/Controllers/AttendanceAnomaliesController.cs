using Asp.Versioning;
using CarminePeople.Application.Attendance.Services;
using CarminePeople.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarminePeople.Api.Controllers;

// Company-wide attendance behavior-change report for HR — separate from AttendanceController
// since it is read-only analytics, not part of the check-in/out transactional surface.
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/attendance-anomalies")]
[Authorize(Policy = Permissions.AuditView)]
public class AttendanceAnomaliesController : ControllerBase
{
    private readonly AttendanceAnomalyService _anomalyService;

    public AttendanceAnomaliesController(AttendanceAnomalyService anomalyService)
    {
        _anomalyService = anomalyService;
    }

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var anomalies = await _anomalyService.GetAnomaliesAsync(ct);
        return Ok(new { success = true, data = anomalies });
    }
}
