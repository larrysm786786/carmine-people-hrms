using CarminePeople.Domain.Enums;

namespace CarminePeople.Application.ShiftSwap.DTOs;

public record RequestShiftSwapRequest(Guid CounterpartId, DateOnly RequesterDate, DateOnly CounterpartDate, string? Reason);

public record ShiftSwapDecisionRequest(string? Comment);

public record ShiftSwapRequestDto(
    Guid Id, Guid RequesterId, string RequesterName, Guid CounterpartId, string CounterpartName,
    DateOnly RequesterDate, DateOnly CounterpartDate, string? Reason,
    ShiftSwapStatus Status, string? DecisionComment);

public record ShiftSwapQuery(Guid? EmployeeId, ShiftSwapStatus? Status, int Page = 1, int PageSize = 20);
