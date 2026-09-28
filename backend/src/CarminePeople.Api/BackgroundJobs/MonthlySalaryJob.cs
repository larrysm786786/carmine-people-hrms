using CarminePeople.Application.Common.Interfaces;
using CarminePeople.Application.Common.Time;
using CarminePeople.Application.Notifications.Services;
using CarminePeople.Domain.Enums;
using CarminePeople.Infrastructure.Notifications;
using CarminePeople.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CarminePeople.Api.BackgroundJobs;

// Once a calendar month completes, computes each active employee's attendance-prorated salary
// for that month from their configured MonthlySalary, notifies them (in-app + email), and sends
// one consolidated summary email to the accounts mailbox so they know what to transfer to whom.
// Mirrors MonthlyAttendanceReportJob's month-completion detection exactly.
public class MonthlySalaryJob : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(6);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MonthlySalaryJob> _logger;
    private DateOnly? _lastSentForMonth;

    public MonthlySalaryJob(IServiceScopeFactory scopeFactory, ILogger<MonthlySalaryJob> logger)
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
                _logger.LogError(ex, "MonthlySalaryJob run failed");
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
        var payrollOptions = scope.ServiceProvider.GetRequiredService<IOptions<PayrollOptions>>().Value;

        var today = await clock.TodayAsync(ct);
        var firstOfThisMonth = new DateOnly(today.Year, today.Month, 1);
        var lastCompletedMonthStart = firstOfThisMonth.AddMonths(-1);

        if (today != firstOfThisMonth) return;
        if (_lastSentForMonth == lastCompletedMonthStart) return;

        var monthEnd = firstOfThisMonth.AddDays(-1);
        var totalDaysInMonth = monthEnd.Day;

        var employees = await db.Employees
            .Where(e => e.EmploymentStatus == EmploymentStatus.Active && e.MonthlySalary != null)
            .Select(e => new { e.Id, e.FirstName, e.LastName, e.Email, e.MonthlySalary })
            .ToListAsync(ct);

        if (employees.Count == 0) return;

        var records = await db.AttendanceRecords
            .Where(a => a.Date >= lastCompletedMonthStart && a.Date <= monthEnd)
            .Select(a => new { a.EmployeeId, a.Status })
            .ToListAsync(ct);

        var recordsByEmployee = records.GroupBy(r => r.EmployeeId).ToDictionary(g => g.Key, g => g.ToList());
        var monthLabel = lastCompletedMonthStart.ToString("MMMM yyyy");

        var summaryRows = new List<(string EmployeeName, string EmployeeCode, decimal Salary)>();

        foreach (var employee in employees)
        {
            var employeeRecords = recordsByEmployee.GetValueOrDefault(employee.Id, []);
            var absentDays = employeeRecords.Count(r => r.Status == AttendanceStatus.Absent);
            var halfDays = employeeRecords.Count(r => r.Status == AttendanceStatus.HalfDay);

            // Per the company's proration rule: an absent day loses a full day's pay, a
            // half-day loses half a day's pay. Leave/Holiday/WeekOff/Present/Late are all
            // fully paid, so only Absent and HalfDay reduce the payable-day count.
            var perDayRate = employee.MonthlySalary!.Value / totalDaysInMonth;
            var deduction = perDayRate * (absentDays + (halfDays * 0.5m));
            var payableSalary = Math.Round(employee.MonthlySalary.Value - deduction, 2);

            var employeeName = $"{employee.FirstName} {employee.LastName}".Trim();

            await notificationService.NotifyAsync(
                employee.Id,
                $"Salary for {monthLabel}",
                $"Your salary for {monthLabel} is Rs {payableSalary:N2}.",
                "salary",
                null,
                ct);

            if (!string.IsNullOrWhiteSpace(employee.Email))
            {
                var html = $"""
                    <p>Hi {employee.FirstName},</p>
                    <p>Your salary for <strong>{monthLabel}</strong> has been calculated based on your attendance:</p>
                    <ul>
                        <li>Monthly salary: Rs {employee.MonthlySalary.Value:N2}</li>
                        <li>Absent days: {absentDays}</li>
                        <li>Half days: {halfDays}</li>
                        <li>Deduction: Rs {deduction:N2}</li>
                        <li><strong>Payable salary: Rs {payableSalary:N2}</strong></li>
                    </ul>
                    <p>— Carmine People</p>
                    """;
                await emailSender.SendAsync(employee.Email, $"Your salary for {monthLabel}: Rs {payableSalary:N2}", html, ct);
            }

            summaryRows.Add((employeeName, employee.Id.ToString()[..8], payableSalary));
        }

        if (!string.IsNullOrWhiteSpace(payrollOptions.AccountsEmail) && summaryRows.Count > 0)
        {
            var rowsHtml = string.Join("\n", summaryRows.Select(r =>
                $"<tr><td>{r.EmployeeName}</td><td style=\"text-align:right\">Rs {r.Salary:N2}</td></tr>"));
            var totalPayout = summaryRows.Sum(r => r.Salary);

            var summaryHtml = $"""
                <p>Salary summary for <strong>{monthLabel}</strong> — {summaryRows.Count} employee(s):</p>
                <table border="1" cellpadding="6" cellspacing="0" style="border-collapse:collapse">
                    <tr><th>Employee</th><th>Salary Payable</th></tr>
                    {rowsHtml}
                    <tr><td><strong>Total</strong></td><td style="text-align:right"><strong>Rs {totalPayout:N2}</strong></td></tr>
                </table>
                """;
            await emailSender.SendAsync(payrollOptions.AccountsEmail, $"Salary summary — {monthLabel}", summaryHtml, ct);
        }

        _lastSentForMonth = lastCompletedMonthStart;
        _logger.LogInformation("Sent monthly salary for {Month} to {Count} employee(s)", monthLabel, employees.Count);
    }
}
