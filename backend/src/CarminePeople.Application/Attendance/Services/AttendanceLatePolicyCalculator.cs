namespace CarminePeople.Application.Attendance.Services;

// Implements the company's cyclical late-arrival policy: within a calendar month, an employee's
// 1st and 2nd late check-ins are "free" (Status stays Late — a warning, not a penalty), the 3rd
// is a half-day, then the cycle repeats (4th and 5th free again, 6th a half-day, and so on for
// the rest of the month). Pure/static like ShiftTimingCalculator, so it's trivially unit-testable
// without a DB.
public static class AttendanceLatePolicyCalculator
{
    private const int FreeLatesBeforeHalfDay = 2;

    // occurrenceNumberThisMonth is 1-based: the 1st late this month is occurrence 1, etc. —
    // pass the counter's LateCount AFTER incrementing it for today's late arrival.
    public static bool IsHalfDayOccurrence(int occurrenceNumberThisMonth) =>
        occurrenceNumberThisMonth % (FreeLatesBeforeHalfDay + 1) == 0;
}
