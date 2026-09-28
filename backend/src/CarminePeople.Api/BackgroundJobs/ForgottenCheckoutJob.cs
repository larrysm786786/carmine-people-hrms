using CarminePeople.Domain.Enums;
using CarminePeople.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CarminePeople.Api.BackgroundJobs;

// Sweeps for attendance records checked in on a prior calendar day with no
// checkout recorded, and flags them PendingRegularization so the employee is
// prompted to file a correction rather than silently keeping stale "still
// checked in" state (per the roadmap's "forgotten checkout" edge case).
public class ForgottenCheckoutJob : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(1);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ForgottenCheckoutJob> _logger;

    public ForgottenCheckoutJob(IServiceScopeFactory scopeFactory, ILogger<ForgottenCheckoutJob> logger)
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
                await FlagForgottenCheckoutsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ForgottenCheckoutJob run failed");
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

    private async Task FlagForgottenCheckoutsAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        // IsCheckedIn (not just CheckOutAt == null) catches the case where the employee
        // re-entered the office after an earlier check-out today and then never checked out
        // again — CheckOutAt would already be non-null from that earlier check-out, so relying
        // on it alone would miss this "still on-site" state.
        var staleRecords = await db.AttendanceRecords
            .Where(a => a.Date < today && a.CheckInAt != null && a.IsCheckedIn && a.Status != AttendanceStatus.PendingRegularization)
            .ToListAsync(ct);

        if (staleRecords.Count == 0) return;

        foreach (var record in staleRecords)
        {
            record.Status = AttendanceStatus.PendingRegularization;
        }
        await db.SaveChangesAsync(ct);

        _logger.LogInformation("Flagged {Count} forgotten-checkout attendance record(s) as PendingRegularization", staleRecords.Count);
    }
}
