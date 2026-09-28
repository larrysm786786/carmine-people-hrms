namespace CarminePeople.Application.Common.Interfaces;

public interface IAuditLogger
{
    Task LogAsync(Guid? actorEmployeeId, string action, string entityType, string? entityId, object? metadata, CancellationToken ct);
}
