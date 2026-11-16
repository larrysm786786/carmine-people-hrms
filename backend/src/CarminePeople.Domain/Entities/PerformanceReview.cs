using CarminePeople.Domain.Common;
using CarminePeople.Domain.Enums;

namespace CarminePeople.Domain.Entities;

public class PerformanceReview : AuditableEntity
{
    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    public string Period { get; set; } = string.Empty;

    public int? SelfRating { get; set; }
    public string? SelfComments { get; set; }
    public DateTime? SelfSubmittedAt { get; set; }

    public int? ManagerRating { get; set; }
    public string? ManagerComments { get; set; }
    public DateTime? ManagerSubmittedAt { get; set; }
    public Guid? ReviewedById { get; set; }
    public Employee? ReviewedBy { get; set; }

    public PerformanceReviewStatus Status { get; set; } = PerformanceReviewStatus.PendingSelfReview;
}
