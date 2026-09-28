using Asp.Versioning;
using CarminePeople.Application.Leave.Services;
using CarminePeople.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarminePeople.Api.Controllers;

// HR-facing "who's likely to take leave next month" report — separate from LeavesController
// since it is read-only historical analytics, not part of the apply/approve transactional
// surface.
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/leave-predictions")]
[Authorize(Policy = Permissions.AuditView)]
public class LeavePredictionsController : ControllerBase
{
    private readonly LeavePredictionService _predictionService;

    public LeavePredictionsController(LeavePredictionService predictionService)
    {
        _predictionService = predictionService;
    }

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var predictions = await _predictionService.GetUpcomingMonthPredictionsAsync(ct);
        return Ok(new { success = true, data = predictions });
    }
}
