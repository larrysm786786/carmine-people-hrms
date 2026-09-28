namespace CarminePeople.Application.Attendance.Services;

// Pure calculation of a personalized check-in reminder time from an employee's own
// recent check-in history — an employee who reliably checks in around 9:45 shouldn't
// be nudged at the generic 9:00 shift start every single day. Deliberately a simple
// median-based heuristic (not ML): transparent, needs no training data or model-serving
// infrastructure, and is accurate enough for the actual question ("when does this
// person usually arrive?"). Revisit with a real model once there's enough historical
// data across the org to validate one against.
public static class SmartReminderCalculator
{
    public const int MinimumHistoryDays = 10;
    public const int HistoryWindowDays = 30;

    // Nudge the employee shortly before their usual check-in time, not after it — a
    // reminder that fires after they normally arrive is useless.
    public static readonly TimeSpan LeadTime = TimeSpan.FromMinutes(10);

    // Never let a personalized time push the reminder later than the shift's own grace
    // deadline — someone whose recent check-ins were all late shouldn't be reminded
    // to be late; the shift-based deadline is always the outer bound.
    public static TimeOnly ResolvePersonalizedReminderTime(List<TimeOnly> recentCheckInTimes, TimeOnly shiftGraceDeadline)
    {
        if (recentCheckInTimes.Count < MinimumHistoryDays) return shiftGraceDeadline;

        var sorted = recentCheckInTimes.OrderBy(t => t).ToList();
        var median = sorted[sorted.Count / 2];
        var personalized = median.AddMinutes(-LeadTime.TotalMinutes);

        return personalized < shiftGraceDeadline ? personalized : shiftGraceDeadline;
    }
}
