namespace CarminePeople.Application.Audit.DTOs;

public record AuditLogDto(Guid Id, Guid? ActorEmployeeId, string? ActorName, string Action, string EntityType, string? EntityId, DateTime Timestamp, string? IpAddress, string? MetadataJson);

public record AuditLogQuery(string? EntityType, string? EntityId, Guid? ActorEmployeeId, DateTime? From, DateTime? To, int Page = 1, int PageSize = 20);
