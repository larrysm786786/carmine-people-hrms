using CarminePeople.Domain.Common;
using CarminePeople.Domain.Enums;

namespace CarminePeople.Domain.Entities;

// Company property (laptop, phone, ID card, etc.) that can be handed to an employee and later
// reclaimed — Status is the single source of truth for lifecycle state; there's no separate
// IsActive flag the way Department/Location have, since "Retired" already covers that role.
public class Asset : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty; // e.g. "Laptop", "Phone", "ID Card"
    public string? SerialNumber { get; set; }

    public Guid? AssignedToEmployeeId { get; set; }
    public Employee? AssignedToEmployee { get; set; }
    public DateTime? AssignedDate { get; set; }

    public AssetStatus Status { get; set; } = AssetStatus.Available;
    public string? Notes { get; set; }
}
