using CarminePeople.Domain.Entities;

namespace CarminePeople.Application.Attendance.Services;

public static class ShiftTimingCalculator
{
    // Resolves the shift's scheduled start DateTime relative to a given moment, correctly
    // handling night shifts (EndTime < StartTime, e.g. 22:00-06:00) whose scheduled start
    // can fall on the PREVIOUS calendar day relative to a check-in just after midnight —
    // e.g. a shift starting 22:00 on Aug 19 should still "own" a 00:05 Aug 20 check-in
    // rather than being compared against a start time 24h in the future.
    public static DateTime ResolveShiftStart(Shift shift, DateTime referenceMoment)
    {
        var startOnSameDate = referenceMoment.Date.Add(shift.StartTime);
        var isNightShift = shift.EndTime < shift.StartTime;
        if (isNightShift && referenceMoment < startOnSameDate)
        {
            return startOnSameDate.AddDays(-1);
        }
        return startOnSameDate;
    }

    public static DateTime ResolveGraceDeadline(Shift shift, DateTime referenceMoment) =>
        ResolveShiftStart(shift, referenceMoment).AddMinutes(shift.GracePeriodMinutes);

    // Resolves the shift's scheduled end DateTime for the same shift occurrence as
    // ResolveShiftStart — i.e. duration-from-start, not "EndTime on referenceMoment's date",
    // so a night shift's end correctly lands on the day after its (possibly backdated) start.
    public static DateTime ResolveShiftEnd(Shift shift, DateTime referenceMoment)
    {
        var shiftStart = ResolveShiftStart(shift, referenceMoment);
        var duration = shift.EndTime >= shift.StartTime
            ? shift.EndTime - shift.StartTime
            : (TimeSpan.FromHours(24) - shift.StartTime) + shift.EndTime;
        return shiftStart.Add(duration);
    }
}
