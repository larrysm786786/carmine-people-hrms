using CarminePeople.Application.Audit.DTOs;
using CarminePeople.Application.Common.Interfaces;
using CarminePeople.Application.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace CarminePeople.Application.Audit.Services;

public class AuditQueryService
{
    private readonly IApplicationDbContext _db;

    public AuditQueryService(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<AuditLogDto>> GetListAsync(AuditLogQuery query, CancellationToken ct)
    {
        var q = _db.AuditLogs.AsQueryable();
        if (!string.IsNullOrWhiteSpace(query.EntityType)) q = q.Where(a => a.EntityType == query.EntityType);
        if (!string.IsNullOrWhiteSpace(query.EntityId)) q = q.Where(a => a.EntityId == query.EntityId);
        if (query.ActorEmployeeId.HasValue) q = q.Where(a => a.ActorEmployeeId == query.ActorEmployeeId);
        if (query.From.HasValue) q = q.Where(a => a.Timestamp >= query.From);
        if (query.To.HasValue) q = q.Where(a => a.Timestamp <= query.To);

        var total = await q.CountAsync(ct);
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        // Left join to Employee by ID rather than a navigation property on AuditLog — keeps
        // the audit table decoupled from the Employee schema (per its "immutable, append-only"
        // design intent), while still letting the UI show a human-readable actor name.
        var items = await (
            from a in q.OrderByDescending(a => a.Timestamp).Skip((page - 1) * pageSize).Take(pageSize)
            join e in _db.Employees on a.ActorEmployeeId equals e.Id into employees
            from e in employees.DefaultIfEmpty()
            orderby a.Timestamp descending
            select new AuditLogDto(a.Id, a.ActorEmployeeId, e == null ? null : e.FirstName + " " + e.LastName, a.Action, a.EntityType, a.EntityId, a.Timestamp, a.IpAddress, a.MetadataJson)
        ).ToListAsync(ct);

        return new PagedResult<AuditLogDto> { Items = items, Page = page, PageSize = pageSize, Total = total };
    }
}
