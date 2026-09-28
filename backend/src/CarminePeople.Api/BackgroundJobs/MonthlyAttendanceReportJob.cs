using CarminePeople.Application.Common.Interfaces;
using CarminePeople.Application.Common.Time;
using CarminePeople.Application.Documents.Services;
using CarminePeople.Application.Notifications.Services;
using CarminePeople.Domain.Enums;
using CarminePeople.Infrastructure.Persistence;
using CarminePeople.Infrastructure.Reports;
using Microsoft.EntityFrameworkCore;

namespace CarminePeople.Api.BackgroundJobs;

// Once a calendar month completes (checked daily against the company's local clock), builds
// each active employee's attendance summary for that month and delivers it three ways: an
// in-app notification, an email with a day-by-day grid PDF attached (matching the company's
// familiar biometric-machine report layout), and the same PDF saved to the employee's
// in-app Documents list (so it's available even if the email bounces or is missed). Tracks
// the last month it already sent for, in-memory, so a process restart on the same day
// doesn't resend — a rare duplicate after a restart on the 1st is harmless, but this keeps
// the common case clean.
public class MonthlyAttendanceReportJob : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(6);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MonthlyAttendanceReportJob> _logger;
    private DateOnly? _lastSentForMonth;

    public MonthlyAttendanceReportJob(IServiceScopeFactory scopeFactory, ILogger<MonthlyAttendanceReportJob> logger)
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
                await SendIfMonthCompletedAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MonthlyAttendanceReportJob run failed");
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

    private async Task SendIfMonthCompletedAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var clock = scope.ServiceProvider.GetRequiredService<CompanyClock>();
        var notificationService = scope.ServiceProvider.GetRequiredService<NotificationService>();
        var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();
        var documentService = scope.ServiceProvider.GetRequiredService<DocumentService>();

        var today = await clock.TodayAsync(ct);
        var firstOfThisMonth = new DateOnly(today.Year, today.Month, 1);
        var lastCompletedMonthStart = firstOfThisMonth.AddMonths(-1);

        if (today != firstOfThisMonth) return;
        if (_lastSentForMonth == lastCompletedMonthStart) return;

        var monthEnd = firstOfThisMonth.AddDays(-1);

        var employees = await db.Employees.Include(e => e.Department)
            .Where(e => e.EmploymentStatus == EmploymentStatus.Active)
            .Select(e => new { e.Id, e.FirstName, e.LastName, e.Email, e.EmployeeCode, DepartmentName = e.Department.Name })
            .ToListAsync(ct);

        var records = await db.AttendanceRecords
            .Where(a => a.Date >= lastCompletedMonthStart && a.Date <= monthEnd)
            .Select(a => new { a.EmployeeId, a.Date, a.Status, a.WorkingMinutes, a.CheckInAt, a.CheckOutAt })
            .ToListAsync(ct);

        var recordsByEmployee = records.GroupBy(r => r.EmployeeId).ToDictionary(g => g.Key, g => g.ToList());
        var monthLabel = lastCompletedMonthStart.ToString("MMMM yyyy");

        foreach (var employee in employees)
        {
            var employeeRecords = recordsByEmployee.GetValueOrDefault(employee.Id, []);
            var recordsByDate = employeeRecords.ToDictionary(r => r.Date);

            var present = employeeRecords.Count(r => r.Status is AttendanceStatus.Present or AttendanceStatus.Late);
            var late = employeeRecords.Count(r => r.Status == AttendanceStatus.Late);
            var absent = employeeRecords.Count(r => r.Status == AttendanceStatus.Absent);
            var leave = employeeRecords.Count(r => r.Status == AttendanceStatus.Leave);
            var totalHours = Math.Round(employeeRecords.Sum(r => r.WorkingMinutes) / 60m, 1);

            var body = $"Present: {present}, Late: {late}, Absent: {absent}, Leave: {leave}, Total hours: {totalHours}";

            await notificationService.NotifyAsync(
                employee.Id,
                $"Attendance summary — {monthLabel}",
                body,
                "attendance_report",
                null,
                ct);

            var days = new List<AttendanceGridDay>();
            for (var date = lastCompletedMonthStart; date <= monthEnd; date = date.AddDays(1))
            {
                if (recordsByDate.TryGetValue(date, out var record))
                {
                    days.Add(new AttendanceGridDay(
                        date,
                        AttendanceStatusCode.For(record.Status),
                        record.CheckInAt.HasValue ? TimeOnly.FromDateTime(record.CheckInAt.Value) : null,
                        record.CheckOutAt.HasValue ? TimeOnly.FromDateTime(record.CheckOutAt.Value) : null,
                        record.WorkingMinutes));
                }
                else
                {
                    days.Add(new AttendanceGridDay(date, null, null, null, 0));
                }
            }

            var pdfBytes = AttendanceGridPdfGenerator.Generate(new AttendanceGridReportData(
                "Carmine Communications LLP",
                employee.EmployeeCode,
                $"{employee.FirstName} {employee.LastName}",
                employee.DepartmentName,
                lastCompletedMonthStart,
                monthEnd,
                days));
            var pdfFileName = $"Attendance-{monthLabel.Replace(' ', '-')}.pdf";

            // Save to the employee's in-app Documents list first — this must succeed
            // independently of email delivery, so the report is still reachable in the app
            // even if the employee's mailbox bounces the message or the email is missed.
            using (var pdfStream = new MemoryStream(pdfBytes))
            {
                await documentService.UploadAsync(
                    employee.Id, employee.Id, DocumentType.AttendanceReport,
                    pdfFileName, "application/pdf", pdfBytes.Length, pdfStream, ct);
            }

            if (!string.IsNullOrWhiteSpace(employee.Email))
            {
                var html = $"""
                    <p>Hi {employee.FirstName},</p>
                    <p>Here is your attendance summary for <strong>{monthLabel}</strong>:</p>
                    <ul>
                        <li>Present: {present}</li>
                        <li>Late: {late}</li>
                        <li>Absent: {absent}</li>
                        <li>Leave: {leave}</li>
                        <li>Total hours worked: {totalHours}</li>
                    </ul>
                    <p>Your full day-by-day attendance is attached as a PDF, and is also saved under Documents in the app.</p>
                    <p>— Carmine People</p>
                    """;

                var attachment = new EmailAttachment(pdfFileName, pdfBytes, "application/pdf");
                await emailSender.SendAsync(employee.Email, $"Your attendance summary — {monthLabel}", html, ct, [attachment]);
            }
        }

        _lastSentForMonth = lastCompletedMonthStart;
        _logger.LogInformation("Sent monthly attendance report for {Month} to {Count} employee(s)", monthLabel, employees.Count);
    }
}
