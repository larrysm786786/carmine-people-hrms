using CarminePeople.Application.Common.Time;
using Xunit;

namespace CarminePeople.UnitTests.Common;

public class CompanyClockTests
{
    [Fact]
    public void ResolveTimeZone_NullOrEmpty_FallsBackToUtc()
    {
        Assert.Equal(TimeZoneInfo.Utc, CompanyClock.ResolveTimeZone(null));
        Assert.Equal(TimeZoneInfo.Utc, CompanyClock.ResolveTimeZone(""));
        Assert.Equal(TimeZoneInfo.Utc, CompanyClock.ResolveTimeZone("   "));
    }

    [Fact]
    public void ResolveTimeZone_UnknownId_FallsBackToUtcRatherThanThrowing()
    {
        // A bad/typo'd BrandingSettings.TimeZoneId must never crash attendance/dashboard
        // requests — it should degrade to UTC instead of throwing TimeZoneNotFoundException.
        var tz = CompanyClock.ResolveTimeZone("Not/A/Real/Zone");
        Assert.Equal(TimeZoneInfo.Utc, tz);
    }

    [Fact]
    public void ResolveTimeZone_ValidId_ResolvesToThatZone()
    {
        var tz = CompanyClock.ResolveTimeZone("UTC");
        Assert.Equal(TimeZoneInfo.Utc, tz);
    }

    [Fact]
    public void ConvertUtcToLocal_TimeZoneAheadOfUtc_CrossesIntoNextCalendarDate()
    {
        // The exact bug class CompanyClock exists to prevent: 23:00 UTC on Aug 19 is already
        // 04:30 local on Aug 20 in a UTC+5:30 zone (Asia/Kolkata-equivalent offset) — the
        // company's "today" must be Aug 20, not Aug 19, for an employee checking in at this
        // moment local time.
        var utcNow = new DateTime(2026, 8, 19, 23, 0, 0, DateTimeKind.Utc);
        var istOffset = TimeZoneInfo.CreateCustomTimeZone("TestIST", TimeSpan.FromHours(5.5), "Test IST", "Test IST");

        var local = CompanyClock.ConvertUtcToLocal(utcNow, istOffset);

        Assert.Equal(new DateTime(2026, 8, 20, 4, 30, 0), local);
        Assert.Equal(new DateOnly(2026, 8, 20), DateOnly.FromDateTime(local));
    }

    [Fact]
    public void ConvertUtcToLocal_TimeZoneBehindUtc_StaysOnPreviousCalendarDate()
    {
        // 02:00 UTC on Aug 20 is still 21:00 on Aug 19 in a UTC-5 zone — "today" for that
        // company must stay Aug 19, not roll forward just because UTC already has.
        var utcNow = new DateTime(2026, 8, 20, 2, 0, 0, DateTimeKind.Utc);
        var estOffset = TimeZoneInfo.CreateCustomTimeZone("TestEST", TimeSpan.FromHours(-5), "Test EST", "Test EST");

        var local = CompanyClock.ConvertUtcToLocal(utcNow, estOffset);

        Assert.Equal(new DateTime(2026, 8, 19, 21, 0, 0), local);
        Assert.Equal(new DateOnly(2026, 8, 19), DateOnly.FromDateTime(local));
    }

    [Fact]
    public void ConvertUtcToLocal_Utc_IsUnchanged()
    {
        var utcNow = new DateTime(2026, 8, 19, 12, 30, 0, DateTimeKind.Utc);
        var local = CompanyClock.ConvertUtcToLocal(utcNow, TimeZoneInfo.Utc);
        Assert.Equal(utcNow, local);
    }
}
