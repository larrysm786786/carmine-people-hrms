using CarminePeople.Domain.Common;
using CarminePeople.Domain.Enums;

namespace CarminePeople.Domain.Entities;

public class LeaveApplication : AuditableEntity
{
    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    public Guid LeaveTypeId { get; set; }
    public LeaveType LeaveType { get; set; } = null!;

    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }

    // Server-computed: excludes weekends and holidays. Never accepted directly from a client.
    public decimal DaysCount { get; set; }

    // Only meaningful when StartDate == EndDate; DaysCount will be 0.5 in that case.
    public bool IsHalfDay { get; set; }

    public string? Reason { get; set; }
    public string? AttachmentUrl { get; set; }

    public LeaveApplicationStatus Status { get; set; } = LeaveApplicationStatus.Pending;

    public ICollection<LeaveApproval> ApprovalTrail { get; set; } = new List<LeaveApproval>();
}

public class LeaveApproval
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid LeaveApplicationId { get; set; }
    public LeaveApplication LeaveApplication { get; set; } = null!;

    public Guid ApproverId { get; set; }
    public Employee Approver { get; set; } = null!;

    public LeaveApplicationStatus Action { get; set; }
    public DateTime ActedAt { get; set; }
    public string? Comment { get; set; }
}
