using System.Text.Json;
using CarminePeople.Application.Common.Interfaces;
using CarminePeople.Domain.Entities;

namespace CarminePeople.Api.Auditing;

public class AuditLogger : IAuditLogger
{
    private readonly IApplicationDbContext _db;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditLogger(IApplicationDbContext db, IHttpContextAccessor httpContextAccessor)
    {
        _db = db;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task LogAsync(Guid? actorEmployeeId, string action, string entityType, string? entityId, object? metadata, CancellationToken ct)
    {
        _db.AuditLogs.Add(new AuditLog
        {
            ActorEmployeeId = actorEmployeeId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Timestamp = DateTime.UtcNow,
            IpAddress = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString(),
            MetadataJson = metadata is null ? null : JsonSerializer.Serialize(metadata)
        });
        // Audit writes are saved eagerly and independently of the caller's unit of work,
        // so a write never depends on—or is lost because of—the caller forgetting SaveChanges.
        await _db.SaveChangesAsync(ct);
    }
}
