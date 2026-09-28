using CarminePeople.Domain.Enums;

namespace CarminePeople.Domain.Entities;

public class RegularizationRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid AttendanceRecordId { get; set; }
    public AttendanceRecord AttendanceRecord { get; set; } = null!;

    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    public string Reason { get; set; } = string.Empty;
    public DateTime? RequestedCheckInAt { get; set; }
    public DateTime? RequestedCheckOutAt { get; set; }

    public RegularizationStatus Status { get; set; } = RegularizationStatus.Pending;
    public DateTime CreatedAt { get; set; }

    public ICollection<RegularizationApproval> ApprovalTrail { get; set; } = new List<RegularizationApproval>();
}

public class RegularizationApproval
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid RegularizationRequestId { get; set; }
    public RegularizationRequest RegularizationRequest { get; set; } = null!;

    public Guid ApproverId { get; set; }
    public Employee Approver { get; set; } = null!;

    public RegularizationStatus Action { get; set; }
    public DateTime ActedAt { get; set; }
    public string? Comment { get; set; }
}
