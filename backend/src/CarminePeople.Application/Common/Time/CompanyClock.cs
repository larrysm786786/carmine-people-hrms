using CarminePeople.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CarminePeople.Application.Common.Time;

// All attendance timestamps are stored in UTC (CheckInAt, CheckOutAt, etc.), but the
// AttendanceRecord.Date column is a calendar day, and "which day is this?" must be decided
// using the company's configured local time zone (BrandingSettings.TimeZoneId), not raw UTC.
// Without this, an employee checking in shortly after local midnight in a timezone ahead of
// UTC gets attributed to the wrong calendar date.
public class CompanyClock
{
    private readonly IApplicationDbContext _db;

    public CompanyClock(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<DateOnly> TodayAsync(CancellationToken ct)
    {
        var localNow = await LocalNowAsync(ct);
        return DateOnly.FromDateTime(localNow);
    }

    public async Task<DateTime> LocalNowAsync(CancellationToken ct)
    {
        var tz = await ResolveTimeZoneAsync(ct);
        return ConvertUtcToLocal(DateTime.UtcNow, tz);
    }

    private async Task<TimeZoneInfo> ResolveTimeZoneAsync(CancellationToken ct)
    {
        var timeZoneId = await _db.BrandingSettings.Select(b => b.TimeZoneId).FirstOrDefaultAsync(ct);
        return ResolveTimeZone(timeZoneId);
    }

    // Pure, deterministic pieces of the above — split out so they can be unit-tested without
    // a real DateTime.UtcNow (which would make a test flaky right around a day boundary) or a
    // real DbContext.
    public static TimeZoneInfo ResolveTimeZone(string? timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId)) return TimeZoneInfo.Utc;
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.Utc;
        }
    }

    public static DateTime ConvertUtcToLocal(DateTime utcNow, TimeZoneInfo tz) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utcNow, DateTimeKind.Utc), tz);
}
