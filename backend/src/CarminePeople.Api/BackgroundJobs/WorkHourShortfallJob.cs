using CarminePeople.Domain.Enums;
using CarminePeople.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CarminePeople.Api.BackgroundJobs;

// Once a day's attendance is fully closed out (employee checked out for the last time),
// checks whether they actually worked the shift's required minimum (e.g. 9 hours) and marks
// the day HalfDay if they fell short — even if they arrived on time. Runs against yesterday
// and earlier so it never acts on a day still in progress (an employee who stepped out for a
// break hasn't "fallen short" yet, they just haven't finished their day).
public class WorkHourShortfallJob : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(6);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<WorkHourShortfallJob> _logger;

    public WorkHourShortfallJob(IServiceScopeFactory scopeFactory, ILogger<WorkHourShortfallJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await FlagShortfallsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "WorkHourShortfallJob run failed");
            }

            try
            {
                await Task.Delay(Interval, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                // Expected on shutdown.
            }
        }
    }

    private async Task FlagShortfallsAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Only Present/Late days are eligible — Absent/Leave/Holiday/WeekOff/HalfDay/
        // PendingRegularization are already final or don't represent a completed work day.
        var candidates = await db.AttendanceRecords
            .Where(a => a.Date < today && !a.IsCheckedIn && a.CheckOutAt != null &&
                        (a.Status == AttendanceStatus.Present || a.Status == AttendanceStatus.Late))
            .Select(a => new { a.Id, a.EmployeeId, a.Date, a.CheckInAt, a.WorkingMinutes })
            .ToListAsync(ct);

        if (candidates.Count == 0) return;

        var employeeIds = candidates.Select(c => c.EmployeeId).Distinct().ToList();
        var shiftAssignments = await db.EmployeeShifts
            .Include(es => es.Shift)
            .Where(es => employeeIds.Contains(es.EmployeeId))
            .ToListAsync(ct);

        var flaggedCount = 0;
        foreach (var candidate in candidates)
        {
            // Resolve whichever shift was effective on the day's check-in — the same
            // "currently effective assignment" rule AttendanceService uses at check-in time.
            var effectiveShift = shiftAssignments
                .Where(es => es.EmployeeId == candidate.EmployeeId &&
                             candidate.CheckInAt.HasValue &&
                             es.EffectiveFrom <= candidate.CheckInAt.Value &&
                             (es.EffectiveTo == null || es.EffectiveTo >= candidate.CheckInAt.Value))
                .OrderByDescending(es => es.EffectiveFrom)
                .FirstOrDefault();

            var requiredMinutes = effectiveShift?.Shift.RequiredWorkMinutes ?? 0;
            if (requiredMinutes <= 0 || candidate.WorkingMinutes >= requiredMinutes) continue;

            var record = await db.AttendanceRecords.FirstAsync(a => a.Id == candidate.Id, ct);
            record.Status = AttendanceStatus.HalfDay;
            flaggedCount++;
        }

        if (flaggedCount > 0)
        {
            await db.SaveChangesAsync(ct);
            _logger.LogInformation("Flagged {Count} attendance record(s) as HalfDay for falling short of required work hours", flaggedCount);
        }
    }
}
