using CarminePeople.Domain.Common;

namespace CarminePeople.Domain.Entities;

public class Permission : AuditableEntity
{
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }

    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
