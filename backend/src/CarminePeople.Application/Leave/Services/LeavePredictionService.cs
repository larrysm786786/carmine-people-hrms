using CarminePeople.Application.Common.Interfaces;
using CarminePeople.Application.Common.Time;
using CarminePeople.Application.Leave.DTOs;
using CarminePeople.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CarminePeople.Application.Leave.Services;

// Separate from LeaveService (which owns the leave apply/approve transactional path) —
// this only reads historical Approved leave applications to project which employees are
// statistically likely to take leave in the coming month, so HR can plan staffing/coverage
// ahead of time rather than reacting to leave requests as they arrive.
//
// The method is calendar-month frequency counting across all prior years an employee has
// history for (e.g. "this employee has taken leave in March in 2 of the last 2 years"), not
// a trained forecasting model — with typically only 1-2 years of history per employee in a
// system this age, a statistical model would be overfitting noise. This is transparent
// (HR can see exactly why someone was flagged) and becomes more informative automatically
// as more years of real history accumulate, without needing to retrain anything.
public record LeaveHistoryPoint(int Year, int Month);

public class LeavePredictionService
{
    // Flag an employee if they've taken leave in this calendar month in at least this
    // fraction of the years they have any history for — 1-in-2 years is a real pattern,
    // not chance (a leave application depends on many independent variables; not enough
    // history exists yet for this to falsely fire from a single applicable year: a lone
    // prior year always calculates to exactly 100%, so requiring at least 2 distinct years
    // of history, enforced separately below, is what keeps a one-off leave from qualifying).
    public const double MinimumRecurrenceRate = 0.5;
    public const int MinimumYearsOfHistory = 2;

    private readonly IApplicationDbContext _db;
    private readonly CompanyClock _clock;

    public LeavePredictionService(IApplicationDbContext db, CompanyClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<List<LeavePredictionDto>> GetUpcomingMonthPredictionsAsync(CancellationToken ct)
    {
        var today = await _clock.TodayAsync(ct);
        var targetMonth = today.Month == 12 ? 1 : today.Month + 1;

        var approvedLeaves = await _db.LeaveApplications
            .Include(l => l.Employee)
            .Where(l => l.Status == LeaveApplicationStatus.Approved)
            .Select(l => new { l.EmployeeId, l.Employee.FirstName, l.Employee.LastName, l.StartDate })
            .ToListAsync(ct);

        var predictions = new List<LeavePredictionDto>();
        foreach (var group in approvedLeaves.GroupBy(l => l.EmployeeId))
        {
            var points = group.Select(l => new LeaveHistoryPoint(l.StartDate.Year, l.StartDate.Month)).ToList();
            var result = AnalyzeEmployee(points, targetMonth);
            if (result is null) continue;

            var employeeName = $"{group.First().FirstName} {group.First().LastName}";
            predictions.Add(new LeavePredictionDto(group.Key, employeeName, targetMonth, result.Value.LikelihoodPercent, result.Value.Reason));
        }

        return predictions.OrderByDescending(p => p.LikelihoodPercent).ToList();
    }

    // Pure, DB-free recurrence calculation — split out so the actual prediction math can be
    // unit tested without a database or years of HTTP-driven leave-application seeding.
    public static (double LikelihoodPercent, string Reason)? AnalyzeEmployee(List<LeaveHistoryPoint> history, int targetMonth)
    {
        var years = history.Select(h => h.Year).Distinct().ToList();
        if (years.Count < MinimumYearsOfHistory) return null;

        var yearsWithLeaveInTargetMonth = history
            .Where(h => h.Month == targetMonth)
            .Select(h => h.Year)
            .Distinct()
            .Count();

        var recurrenceRate = (double)yearsWithLeaveInTargetMonth / years.Count;
        if (recurrenceRate < MinimumRecurrenceRate) return null;

        return (Math.Round(recurrenceRate * 100, 0), $"Took leave in this month in {yearsWithLeaveInTargetMonth} of the last {years.Count} years.");
    }
}
