namespace CarminePeople.Domain.Entities;

// Tracks how many late-arrival occurrences an employee has accrued in a given calendar month,
// to drive the cyclical "2 free lates, 3rd is a half-day, cycle repeats" company policy.
// LateCount is the raw running total for the month; the cyclical position (is this occurrence
// the 3rd/6th/9th... of the month) is derived from it, not stored separately.
public class EmployeeMonthlyLateCounter
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    public int Year { get; set; }
    public int Month { get; set; }

    public int LateCount { get; set; }
}
