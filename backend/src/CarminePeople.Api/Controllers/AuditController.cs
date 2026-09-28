using Asp.Versioning;
using CarminePeople.Domain.Constants;
using CarminePeople.Application.Audit.DTOs;
using CarminePeople.Application.Audit.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarminePeople.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/audit")]
[Authorize(Policy = Permissions.AuditView)]
public class AuditController : ControllerBase
{
    private readonly AuditQueryService _auditQueryService;

    public AuditController(AuditQueryService auditQueryService)
    {
        _auditQueryService = auditQueryService;
    }

    [HttpGet]
    public async Task<IActionResult> GetList(
        [FromQuery] string? entityType, [FromQuery] string? entityId, [FromQuery] Guid? actorEmployeeId,
        [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _auditQueryService.GetListAsync(new AuditLogQuery(entityType, entityId, actorEmployeeId, from, to, page, pageSize), ct);
        return Ok(new { success = true, data = result.Items, meta = new { result.Page, result.PageSize, result.Total } });
    }
}
