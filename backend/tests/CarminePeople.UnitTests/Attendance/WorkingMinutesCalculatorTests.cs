using CarminePeople.Application.Attendance.Services;
using Xunit;

namespace CarminePeople.UnitTests.Attendance;

public class WorkingMinutesCalculatorTests
{
    [Fact]
    public void Calculate_EightHourShift_NoBreaks_ReturnsFullMinutes()
    {
        var checkIn = new DateTime(2026, 8, 17, 9, 0, 0, DateTimeKind.Utc);
        var checkOut = new DateTime(2026, 8, 17, 17, 0, 0, DateTimeKind.Utc);
        var result = WorkingMinutesCalculator.Calculate(checkIn, checkOut, 0);
        Assert.Equal(480, result);
    }

    [Fact]
    public void Calculate_SubtractsBreakMinutes()
    {
        var checkIn = new DateTime(2026, 8, 17, 9, 0, 0, DateTimeKind.Utc);
        var checkOut = new DateTime(2026, 8, 17, 17, 0, 0, DateTimeKind.Utc);
        var result = WorkingMinutesCalculator.Calculate(checkIn, checkOut, 30);
        Assert.Equal(450, result);
    }

    [Fact]
    public void Calculate_OvernightNightShift_CrossesCalendarDate()
    {
        // Night shift: check in 22:00, check out 06:00 next day
        var checkIn = new DateTime(2026, 8, 17, 22, 0, 0, DateTimeKind.Utc);
        var checkOut = new DateTime(2026, 8, 18, 6, 0, 0, DateTimeKind.Utc);
        var result = WorkingMinutesCalculator.Calculate(checkIn, checkOut, 0);
        Assert.Equal(480, result);
    }

    [Fact]
    public void Calculate_BreaksExceedingGrossTime_ClampsToZero()
    {
        // Defensive: should never happen given break validation, but must not go negative
        var checkIn = new DateTime(2026, 8, 17, 9, 0, 0, DateTimeKind.Utc);
        var checkOut = new DateTime(2026, 8, 17, 9, 30, 0, DateTimeKind.Utc);
        var result = WorkingMinutesCalculator.Calculate(checkIn, checkOut, 999);
        Assert.Equal(0, result);
    }

    [Fact]
    public void Calculate_ZeroDuration_ReturnsZero()
    {
        var instant = new DateTime(2026, 8, 17, 9, 0, 0, DateTimeKind.Utc);
        var result = WorkingMinutesCalculator.Calculate(instant, instant, 0);
        Assert.Equal(0, result);
    }
}
