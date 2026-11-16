using CarminePeople.Domain.Enums;

namespace CarminePeople.Application.Expenses.DTOs;

public record ExpenseDto(
    Guid Id, Guid EmployeeId, string EmployeeName, string Category, decimal Amount, DateOnly ExpenseDate,
    string? Description, string? ReceiptUrl, ExpenseStatus Status,
    string? DecidedByName, DateTime? DecidedAt, string? DecisionComment, DateTime? ReimbursedAt, DateTime CreatedAt);

public record SubmitExpenseRequest(string Category, decimal Amount, DateOnly ExpenseDate, string? Description, string? ReceiptUrl);

public record ExpenseDecisionRequest(string? Comment);

public record ExpenseQuery(Guid? EmployeeId, ExpenseStatus? Status, int Page = 1, int PageSize = 20);
