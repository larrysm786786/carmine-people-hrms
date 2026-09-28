using CarminePeople.Application.Attendance.Services;
using CarminePeople.Domain.Entities;
using Xunit;

namespace CarminePeople.UnitTests.Attendance;

public class ShiftTimingCalculatorTests
{
    private static Shift DayShift => new() { StartTime = new TimeSpan(9, 0, 0), EndTime = new TimeSpan(18, 0, 0), GracePeriodMinutes = 10 };
    private static Shift NightShift => new() { StartTime = new TimeSpan(22, 0, 0), EndTime = new TimeSpan(6, 0, 0), GracePeriodMinutes = 10 };

    [Fact]
    public void ResolveShiftStart_DayShift_SameCalendarDate()
    {
        var checkIn = new DateTime(2026, 8, 19, 9, 5, 0, DateTimeKind.Utc);
        var result = ShiftTimingCalculator.ResolveShiftStart(DayShift, checkIn);
        Assert.Equal(new DateTime(2026, 8, 19, 9, 0, 0), result);
    }

    [Fact]
    public void ResolveShiftStart_NightShift_CheckInBeforeMidnight_SameCalendarDate()
    {
        // Checked in at 22:15 on Aug 19 — the shift they're on time for started 22:00 same day.
        var checkIn = new DateTime(2026, 8, 19, 22, 15, 0, DateTimeKind.Utc);
        var result = ShiftTimingCalculator.ResolveShiftStart(NightShift, checkIn);
        Assert.Equal(new DateTime(2026, 8, 19, 22, 0, 0), result);
    }

    [Fact]
    public void ResolveShiftStart_NightShift_CheckInAfterMidnight_PreviousCalendarDate()
    {
        // The real bug this test guards against: checking in at 00:05 on Aug 20 for a shift
        // that started 22:00 on Aug 19 must resolve the start to Aug 19 22:00, NOT
        // Aug 20 22:00 (which would be 24h in the future and make every night-shift
        // check-in incorrectly "on time" against a start that hasn't happened yet).
        var checkIn = new DateTime(2026, 8, 20, 0, 5, 0, DateTimeKind.Utc);
        var result = ShiftTimingCalculator.ResolveShiftStart(NightShift, checkIn);
        Assert.Equal(new DateTime(2026, 8, 19, 22, 0, 0), result);
    }

    [Fact]
    public void ResolveGraceDeadline_NightShift_CheckInAfterMidnight_LateDetectedCorrectly()
    {
        // Checked in at 00:20 on Aug 20 — 20 minutes after the Aug 19 22:00 start plus a
        // 10-minute grace period (deadline 22:10) — should be flagged late.
        var checkIn = new DateTime(2026, 8, 20, 0, 20, 0, DateTimeKind.Utc);
        var deadline = ShiftTimingCalculator.ResolveGraceDeadline(NightShift, checkIn);
        Assert.Equal(new DateTime(2026, 8, 19, 22, 10, 0), deadline);
        Assert.True(checkIn > deadline);
    }

    [Fact]
    public void ResolveGraceDeadline_NightShift_CheckInWithinGrace_NotLate()
    {
        // Checked in at 22:08, within the 10-minute grace window of a 22:00 start.
        var checkIn = new DateTime(2026, 8, 19, 22, 8, 0, DateTimeKind.Utc);
        var deadline = ShiftTimingCalculator.ResolveGraceDeadline(NightShift, checkIn);
        Assert.False(checkIn > deadline);
    }
}
