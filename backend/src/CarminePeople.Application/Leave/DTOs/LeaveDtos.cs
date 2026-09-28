using CarminePeople.Domain.Enums;

namespace CarminePeople.Application.Leave.DTOs;

public record LeaveTypeDto(Guid Id, string Name, bool IsPaid, bool RequiresAttachment, decimal DefaultAnnualDays);

public record CreateLeaveTypeRequest(string Name, bool IsPaid, bool RequiresAttachment, decimal DefaultAnnualDays);

public record UpdateLeaveTypeRequest(string Name, bool IsPaid, bool RequiresAttachment, decimal DefaultAnnualDays);

public record LeaveBalanceDto(Guid LeaveTypeId, string LeaveTypeName, int Year, decimal Available, decimal Used, decimal Pending);

// IsHalfDay is only honored when StartDate == EndDate (single-day leave); the service
// validates that combination and rejects half-day requests spanning multiple days.
public record ApplyLeaveRequest(Guid LeaveTypeId, DateOnly StartDate, DateOnly EndDate, string? Reason, string? AttachmentUrl, bool IsHalfDay = false);

public record LeaveApplicationDto(
    Guid Id, Guid EmployeeId, string EmployeeName, Guid LeaveTypeId, string LeaveTypeName,
    DateOnly StartDate, DateOnly EndDate, decimal DaysCount, string? Reason,
    LeaveApplicationStatus Status, bool IsHalfDay);

public record LeaveApplicationQuery(Guid? EmployeeId, LeaveApplicationStatus? Status, Guid? LeaveTypeId, int Page = 1, int PageSize = 20);

public record LeaveDecisionRequest(string? Comment);

public record LeavePredictionDto(Guid EmployeeId, string EmployeeName, int TargetMonth, double LikelihoodPercent, string Reason);
