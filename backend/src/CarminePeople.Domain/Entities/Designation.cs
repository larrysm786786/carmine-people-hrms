using CarminePeople.Domain.Common;

namespace CarminePeople.Domain.Entities;

public class Designation : AuditableEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid DepartmentId { get; set; }
    public Department Department { get; set; } = null!;
    public bool IsActive { get; set; } = true;
}
