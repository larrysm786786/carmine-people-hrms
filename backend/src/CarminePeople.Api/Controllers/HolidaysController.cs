using Asp.Versioning;
using CarminePeople.Domain.Constants;
using CarminePeople.Application.Common.Interfaces;
using CarminePeople.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarminePeople.Api.Controllers;

public record HolidayDto(Guid Id, string Name, string? Description, DateOnly Date, Guid? LocationId, bool IsOptional);
public record CreateHolidayRequest(string Name, string? Description, DateOnly Date, Guid? LocationId, bool IsOptional);
public record UpdateHolidayRequest(string Name, string? Description, DateOnly Date, Guid? LocationId, bool IsOptional);

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/holidays")]
[Authorize]
public class HolidaysController : ControllerBase
{
    private readonly IApplicationDbContext _db;

    public HolidaysController(IApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetList([FromQuery] Guid? locationId, [FromQuery] int? year, CancellationToken ct)
    {
        var q = _db.Holidays.AsQueryable();
        if (locationId.HasValue) q = q.Where(h => h.LocationId == null || h.LocationId == locationId);
        if (year.HasValue) q = q.Where(h => h.Date.Year == year);

        var items = await q.OrderBy(h => h.Date)
            .Select(h => new HolidayDto(h.Id, h.Name, h.Description, h.Date, h.LocationId, h.IsOptional))
            .ToListAsync(ct);
        return Ok(new { success = true, data = items });
    }

    [HttpPost]
    [Authorize(Policy = Permissions.HolidayManage)]
    public async Task<IActionResult> Create([FromBody] CreateHolidayRequest request, CancellationToken ct)
    {
        var holiday = new Holiday { Name = request.Name, Description = request.Description, Date = request.Date, LocationId = request.LocationId, IsOptional = request.IsOptional };
        _db.Holidays.Add(holiday);
        await _db.SaveChangesAsync(ct);
        return Ok(new { success = true, data = new HolidayDto(holiday.Id, holiday.Name, holiday.Description, holiday.Date, holiday.LocationId, holiday.IsOptional) });
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Permissions.HolidayManage)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateHolidayRequest request, CancellationToken ct)
    {
        var holiday = await _db.Holidays.FirstOrDefaultAsync(h => h.Id == id, ct);
        if (holiday is null) return NotFound(new { success = false, error = new { code = "NOT_FOUND", message = "Holiday not found." } });

        holiday.Name = request.Name;
        holiday.Description = request.Description;
        holiday.Date = request.Date;
        holiday.LocationId = request.LocationId;
        holiday.IsOptional = request.IsOptional;
        await _db.SaveChangesAsync(ct);
        return Ok(new { success = true, data = new HolidayDto(holiday.Id, holiday.Name, holiday.Description, holiday.Date, holiday.LocationId, holiday.IsOptional) });
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Permissions.HolidayManage)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var holiday = await _db.Holidays.FirstOrDefaultAsync(h => h.Id == id, ct);
        if (holiday is null) return NotFound(new { success = false, error = new { code = "NOT_FOUND", message = "Holiday not found." } });
        holiday.IsDeleted = true;
        await _db.SaveChangesAsync(ct);
        return Ok(new { success = true, data = (object?)null });
    }
}
