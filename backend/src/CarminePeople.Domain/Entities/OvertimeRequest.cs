using CarminePeople.Domain.Common;
using CarminePeople.Domain.Enums;

namespace CarminePeople.Domain.Entities;

public class OvertimeRequest : AuditableEntity
{
    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    public DateOnly Date { get; set; }
    public decimal Hours { get; set; }
    public string? Reason { get; set; }

    public OvertimeRequestStatus Status { get; set; } = OvertimeRequestStatus.Pending;

    public Guid? DecidedByEmployeeId { get; set; }
    public Employee? DecidedByEmployee { get; set; }
    public DateTime? DecidedAt { get; set; }
    public string? DecisionComment { get; set; }
}
