using CarminePeople.Application.Attendance.Services;
using Xunit;

namespace CarminePeople.UnitTests.Attendance;

public class SmartReminderCalculatorTests
{
    private static readonly TimeOnly GraceDeadline = new(9, 30);

    [Fact]
    public void ResolvePersonalizedReminderTime_InsufficientHistory_FallsBackToGraceDeadline()
    {
        var history = Enumerable.Repeat(new TimeOnly(9, 0), SmartReminderCalculator.MinimumHistoryDays - 1).ToList();
        var result = SmartReminderCalculator.ResolvePersonalizedReminderTime(history, GraceDeadline);
        Assert.Equal(GraceDeadline, result);
    }

    [Fact]
    public void ResolvePersonalizedReminderTime_ConsistentEarlyHistory_UsesMedianMinusLeadTime()
    {
        var history = Enumerable.Repeat(new TimeOnly(9, 0), SmartReminderCalculator.MinimumHistoryDays).ToList();
        var result = SmartReminderCalculator.ResolvePersonalizedReminderTime(history, GraceDeadline);
        Assert.Equal(new TimeOnly(8, 50), result);
    }

    [Fact]
    public void ResolvePersonalizedReminderTime_HistoryLaterThanGrace_ClampsToGraceDeadline()
    {
        // Employee habitually checks in well after the grace deadline (median 9:45, minus the
        // 10-min lead time = 9:35, which is still later than the 9:30 grace deadline) — the
        // reminder must never be pushed past the grace deadline itself, or it becomes useless.
        var history = Enumerable.Repeat(new TimeOnly(9, 45), SmartReminderCalculator.MinimumHistoryDays).ToList();
        var result = SmartReminderCalculator.ResolvePersonalizedReminderTime(history, GraceDeadline);
        Assert.Equal(GraceDeadline, result);
    }

    [Fact]
    public void ResolvePersonalizedReminderTime_MixedHistory_UsesMedianNotAverage()
    {
        // Sorted: 8:50, 9:00, 9:10, 9:10, 9:20, 9:20, 9:20, 9:25, 9:25, 9:40 (10 values) —
        // index 5 (this class's median = sorted[count/2]) = 9:20, minus the 10-min lead time
        // = 9:10, unaffected by the 9:40 outlier.
        var history = new List<TimeOnly>
        {
            new(9, 0), new(9, 20), new(8, 50), new(9, 10), new(9, 40),
            new(9, 25), new(9, 10), new(9, 20), new(9, 25), new(9, 20),
        };
        var result = SmartReminderCalculator.ResolvePersonalizedReminderTime(history, GraceDeadline);
        Assert.Equal(new TimeOnly(9, 10), result);
    }
}
