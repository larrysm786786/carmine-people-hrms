using CarminePeople.Application.Attendance.Services;
using CarminePeople.Application.Common.Time;
using CarminePeople.Application.Notifications.Services;
using CarminePeople.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CarminePeople.Api.BackgroundJobs;

// Mirrors AttendanceReminderJob for the opposite edge: periodically checks for employees who
// are still checked in (IsCheckedIn) a configurable buffer past their shift's scheduled end
// time, and sends a one-time "don't forget to check out" notification. Catches the common case
// same-day, before ForgottenCheckoutJob's next-day sweep would otherwise be the first signal —
// that job still runs as the backstop for whoever ignores this reminder.
public class CheckoutReminderJob : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan ReminderBufferAfterShiftEnd = TimeSpan.FromMinutes(30);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CheckoutReminderJob> _logger;
    private readonly HashSet<(DateOnly Date, Guid EmployeeId)> _remindedToday = new();

    public CheckoutReminderJob(IServiceScopeFactory scopeFactory, ILogger<CheckoutReminderJob> logger)
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
                _logger.LogError(ex, "CheckoutReminderJob run failed");
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
        var today = await clock.TodayAsync(ct);
        _remindedToday.RemoveWhere(r => r.Date != today);

        var dueShifts = await db.EmployeeShifts
            .Include(es => es.Shift)
            .Where(es => es.EffectiveFrom <= now && (es.EffectiveTo == null || es.EffectiveTo >= now))
            .ToListAsync(ct);

        // Only employees still on-site today (checked in, not yet checked out) are candidates —
        // same IsCheckedIn signal ForgottenCheckoutJob uses, so this job and that one agree on
        // what "still checked in" means.
        var stillCheckedInEmployeeIds = (await db.AttendanceRecords
            .Where(a => a.Date == today && a.IsCheckedIn)
            .Select(a => a.EmployeeId)
            .ToListAsync(ct)).ToHashSet();

        foreach (var assignment in dueShifts)
        {
            if (!stillCheckedInEmployeeIds.Contains(assignment.EmployeeId)) continue;
            if (_remindedToday.Contains((today, assignment.EmployeeId))) continue;

            var shiftEnd = ShiftTimingCalculator.ResolveShiftEnd(assignment.Shift, now);
            var reminderMoment = shiftEnd.Add(ReminderBufferAfterShiftEnd);
            if (now < reminderMoment) continue;

            await notificationService.NotifyAsync(
                assignment.EmployeeId, "Checkout reminder", "Your shift has ended — don't forget to check out.", "attendance", null, ct);
            _remindedToday.Add((today, assignment.EmployeeId));
        }
    }
}
