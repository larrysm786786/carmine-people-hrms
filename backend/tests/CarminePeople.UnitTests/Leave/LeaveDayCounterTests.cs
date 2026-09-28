using CarminePeople.Application.Leave.Services;
using Xunit;

namespace CarminePeople.UnitTests.Leave;

public class LeaveDayCounterTests
{
    [Fact]
    public void CountWorkingDays_SingleWeekday_ReturnsOne()
    {
        // 2026-08-17 is a Monday
        var result = LeaveDayCounter.CountWorkingDays(new DateOnly(2026, 8, 17), new DateOnly(2026, 8, 17), new HashSet<DateOnly>());
        Assert.Equal(1, result);
    }

    [Fact]
    public void CountWorkingDays_ExcludesWeekends()
    {
        // Mon 2026-08-17 through Sun 2026-08-23 = 5 weekdays, 2 weekend days excluded
        var result = LeaveDayCounter.CountWorkingDays(new DateOnly(2026, 8, 17), new DateOnly(2026, 8, 23), new HashSet<DateOnly>());
        Assert.Equal(5, result);
    }

    [Fact]
    public void CountWorkingDays_ExcludesHolidays()
    {
        var holidays = new HashSet<DateOnly> { new DateOnly(2026, 8, 19) }; // Wednesday
        var result = LeaveDayCounter.CountWorkingDays(new DateOnly(2026, 8, 17), new DateOnly(2026, 8, 21), holidays);
        Assert.Equal(4, result); // Mon,Tue,Thu,Fri (Wed excluded as holiday)
    }

    [Fact]
    public void CountWorkingDays_OnlyWeekendRange_ReturnsZero()
    {
        // Sat 2026-08-22 to Sun 2026-08-23
        var result = LeaveDayCounter.CountWorkingDays(new DateOnly(2026, 8, 22), new DateOnly(2026, 8, 23), new HashSet<DateOnly>());
        Assert.Equal(0, result);
    }

    [Fact]
    public void CountWorkingDays_EndBeforeStart_ReturnsZero()
    {
        var result = LeaveDayCounter.CountWorkingDays(new DateOnly(2026, 8, 20), new DateOnly(2026, 8, 17), new HashSet<DateOnly>());
        Assert.Equal(0, result);
    }

    [Fact]
    public void CountWorkingDays_HolidayOnWeekend_DoesNotDoubleSubtract()
    {
        var holidays = new HashSet<DateOnly> { new DateOnly(2026, 8, 22) }; // Saturday, already excluded as weekend
        var result = LeaveDayCounter.CountWorkingDays(new DateOnly(2026, 8, 17), new DateOnly(2026, 8, 23), holidays);
        Assert.Equal(5, result);
    }
}
