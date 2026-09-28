using CarminePeople.Application.Attendance.Services;
using CarminePeople.Application.Common.Time;
using CarminePeople.Application.Notifications.Services;
using CarminePeople.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CarminePeople.Api.BackgroundJobs;

// Periodically checks for employees whose shift has started (past its grace period) with no
// check-in recorded yet today, and sends a one-time "attendance reminder" notification. Uses
// an in-memory per-day "already reminded" set (reset on process restart) rather than a DB flag,
// since a duplicate reminder after a rare restart is harmless and this avoids a schema change
// for a purely best-effort nudge.
public class AttendanceReminderJob : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(15);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AttendanceReminderJob> _logger;
    private readonly HashSet<(DateOnly Date, Guid EmployeeId)> _remindedToday = new();

    public AttendanceReminderJob(IServiceScopeFactory scopeFactory, ILogger<AttendanceReminderJob> logger)
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
                await SendRemindersAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AttendanceReminderJob run failed");
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

    private async Task SendRemindersAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var notificationService = scope.ServiceProvider.GetRequiredService<NotificationService>();
        var clock = scope.ServiceProvider.GetRequiredService<CompanyClock>();

        var now = DateTime.UtcNow;
        // "Today" for matching against AttendanceRecord.Date must follow the company's
        // configured local calendar day (same as AttendanceService), not raw UTC — otherwise
        // this job's own idea of "today" could disagree with what CheckInAsync just wrote.
        var today = await clock.TodayAsync(ct);
        _remindedToday.RemoveWhere(r => r.Date != today);

        var dueShifts = await db.EmployeeShifts
            .Include(es => es.Shift)
            .Where(es => es.EffectiveFrom <= now && (es.EffectiveTo == null || es.EffectiveTo >= now))
            .ToListAsync(ct);

        var checkedInEmployeeIds = (await db.AttendanceRecords
            .Where(a => a.Date == today && a.CheckInAt != null)
            .Select(a => a.EmployeeId)
            .ToListAsync(ct)).ToHashSet();

        // Recent check-in history for personalizing each employee's reminder time
        // (see SmartReminderCalculator) — one query up front rather than per-employee.
        var historyStart = today.AddDays(-SmartReminderCalculator.HistoryWindowDays);
        var recentCheckInTimesByEmployee = (await db.AttendanceRecords
            .Where(a => a.Date >= historyStart && a.Date < today && a.CheckInAt != null)
            .Select(a => new { a.EmployeeId, a.CheckInAt })
            .ToListAsync(ct))
            .GroupBy(a => a.EmployeeId)
            .ToDictionary(g => g.Key, g => g.Select(a => TimeOnly.FromDateTime(a.CheckInAt!.Value)).ToList());

        foreach (var assignment in dueShifts)
        {
            if (checkedInEmployeeIds.Contains(assignment.EmployeeId)) continue;
            if (_remindedToday.Contains((today, assignment.EmployeeId))) continue;

            var shiftStart = ShiftTimingCalculator.ResolveShiftStart(assignment.Shift, now);
            var graceDeadline = shiftStart.AddMinutes(assignment.Shift.GracePeriodMinutes);

            var history = recentCheckInTimesByEmployee.GetValueOrDefault(assignment.EmployeeId, []);
            var reminderTimeOfDay = SmartReminderCalculator.ResolvePersonalizedReminderTime(history, TimeOnly.FromDateTime(graceDeadline));
            var reminderMoment = shiftStart.Date.Add(reminderTimeOfDay.ToTimeSpan());

            if (now < reminderMoment || now > graceDeadline) continue;

            await notificationService.NotifyAsync(
                assignment.EmployeeId, "Attendance reminder", "Don't forget to check in today.", "attendance", null, ct);
            _remindedToday.Add((today, assignment.EmployeeId));
        }
    }
}
