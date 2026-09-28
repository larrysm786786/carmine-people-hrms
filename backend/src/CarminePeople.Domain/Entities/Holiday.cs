using CarminePeople.Domain.Common;

namespace CarminePeople.Domain.Entities;

public class Holiday : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateOnly Date { get; set; }
    public Guid? LocationId { get; set; } // null = applies to all locations
    public Location? Location { get; set; }
    public bool IsOptional { get; set; }
}
