using CarminePeople.Domain.Common;

namespace CarminePeople.Domain.Entities;

public class Department : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? Description { get; set; }
    public Guid? ManagerId { get; set; }
    public Employee? Manager { get; set; }
    public bool IsActive { get; set; } = true;
}
