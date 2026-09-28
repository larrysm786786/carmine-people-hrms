namespace CarminePeople.Domain.Entities;

// Immutable, append-only. No AuditableEntity base — this table is never soft-deleted or updated.
public class AuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid? ActorEmployeeId { get; set; }
    public Employee? ActorEmployee { get; set; }

    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public DateTime Timestamp { get; set; }
    public string? IpAddress { get; set; }
    public string? MetadataJson { get; set; }
}
