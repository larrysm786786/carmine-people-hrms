using CarminePeople.Domain.Common;
using CarminePeople.Domain.Enums;

namespace CarminePeople.Domain.Entities;

public class Expense : AuditableEntity
{
    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    public string Category { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateOnly ExpenseDate { get; set; }
    public string? Description { get; set; }
    public string? ReceiptUrl { get; set; }

    public ExpenseStatus Status { get; set; } = ExpenseStatus.Pending;

    public Guid? DecidedById { get; set; }
    public Employee? DecidedBy { get; set; }
    public DateTime? DecidedAt { get; set; }
    public string? DecisionComment { get; set; }

    public DateTime? ReimbursedAt { get; set; }
}
