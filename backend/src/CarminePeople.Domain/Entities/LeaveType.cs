using CarminePeople.Domain.Common;

namespace CarminePeople.Domain.Entities;

public class LeaveType : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public bool IsPaid { get; set; } = true;
    public bool RequiresAttachment { get; set; }
    public decimal DefaultAnnualDays { get; set; }
}
