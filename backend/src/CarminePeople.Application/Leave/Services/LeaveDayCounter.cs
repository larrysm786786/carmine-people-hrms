namespace CarminePeople.Application.Leave.Services;

public static class LeaveDayCounter
{
    // Counts calendar days in [start, end] excluding Saturdays, Sundays, and given holiday dates.
    // isHalfDay only applies meaningfully to a single-day range (start == end); the caller is
    // expected to validate that before calling, but this still degrades safely (only the day
    // matching start counts as 0.5) rather than misapplying the discount across a whole range.
    public static decimal CountWorkingDays(DateOnly start, DateOnly end, IReadOnlySet<DateOnly> holidayDates, bool isHalfDay = false)
    {
        if (end < start) return 0;

        decimal count = 0;
        for (var date = start; date <= end; date = date.AddDays(1))
        {
            var isWeekend = date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
            if (!isWeekend && !holidayDates.Contains(date))
            {
                count += isHalfDay && date == start ? 0.5m : 1m;
            }
        }
        return count;
    }
}
