using CarminePeople.Domain.Enums;

namespace CarminePeople.Application.Overtime.DTOs;

public record RequestOvertimeRequest(DateOnly Date, decimal Hours, string? Reason);

public record OvertimeDecisionRequest(string? Comment);

public record OvertimeRequestDto(
    Guid Id, Guid EmployeeId, string EmployeeName, DateOnly Date, decimal Hours, string? Reason,
    OvertimeRequestStatus Status, string? DecisionComment);

public record OvertimeQuery(Guid? EmployeeId, OvertimeRequestStatus? Status, int Page = 1, int PageSize = 20);
