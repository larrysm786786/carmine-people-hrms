using CarminePeople.Domain.Common;
using CarminePeople.Domain.Enums;

namespace CarminePeople.Domain.Entities;

public class ShiftSwapRequest : AuditableEntity
{
    // The employee proposing the swap, giving up their shift on RequestedDate.
    public Guid RequesterId { get; set; }
    public Employee Requester { get; set; } = null!;

    // The counterpart who would take over RequestedDate and, in exchange,
    // has their own shift on CounterpartDate given to the requester.
    public Guid CounterpartId { get; set; }
    public Employee Counterpart { get; set; } = null!;

    public DateOnly RequesterDate { get; set; }
    public DateOnly CounterpartDate { get; set; }
    public string? Reason { get; set; }

    public ShiftSwapStatus Status { get; set; } = ShiftSwapStatus.RequestedAcceptance;

    public DateTime? CounterpartRespondedAt { get; set; }

    public Guid? DecidedByEmployeeId { get; set; }
    public Employee? DecidedByEmployee { get; set; }
    public DateTime? DecidedAt { get; set; }
    public string? DecisionComment { get; set; }
}
