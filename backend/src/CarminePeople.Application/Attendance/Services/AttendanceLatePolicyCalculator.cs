namespace CarminePeople.Application.Attendance.Services;

// Implements the company's cyclical late-arrival policy: within a calendar month, an employee's
// first `freeLatesBeforeHalfDay` late check-ins are "free" (Status stays Late — a warning, not a
// penalty), the next one is a half-day, then the cycle repeats (the following
// `freeLatesBeforeHalfDay` are free again, the one after that a half-day, and so on for the rest
// of the month). `freeLatesBeforeHalfDay` is company-configurable (BrandingSettings, editable in
// Admin → Settings), not hardcoded. Pure/static like ShiftTimingCalculator, so it's trivially
// unit-testable without a DB.
public static class AttendanceLatePolicyCalculator
{
    // occurrenceNumberThisMonth is 1-based: the 1st late this month is occurrence 1, etc. —
    // pass the counter's LateCount AFTER incrementing it for today's late arrival.
    public static bool IsHalfDayOccurrence(int occurrenceNumberThisMonth, int freeLatesBeforeHalfDay) =>
        occurrenceNumberThisMonth % (freeLatesBeforeHalfDay + 1) == 0;
}
