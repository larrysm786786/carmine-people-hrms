using CarminePeople.Application.Attendance.DTOs;
using CarminePeople.Application.Common.Interfaces;
using CarminePeople.Application.Common.Time;
using CarminePeople.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CarminePeople.Application.Attendance.Services;

public record AttendanceHistoryPoint(DateOnly Date, AttendanceStatus Status);

// Deliberately kept separate from AttendanceService: this reads and aggregates
// attendance history to surface patterns for HR, it never writes attendance data, and
// splitting it out keeps AttendanceService focused on the check-in/out transactional path.
//
// The detection method is intentionally simple statistics (rolling baseline vs. a recent
// window), not a trained ML model — a threshold-based comparison is transparent (HR can see
// exactly why something was flagged), needs no training data or model-serving
// infrastructure, and is accurate enough for the actual questions this answers ("has this
// person's attendance pattern changed recently?"). A learned model is worth revisiting once
// there's enough historical data across enough employees to validate one against.
public class AttendanceAnomalyService
{
    public const int BaselineWindowDays = 60;
    public const int RecentWindowDays = 14;
    public const int MinimumRecordsForBaseline = 10;

    // A swing of more than this many percentage points in late-rate or absence-rate between
    // the baseline and recent window is treated as a genuine behavior change worth flagging,
    // not noise — smaller swings are common (a single late day in 14 shifts the rate ~7%).
    public const double SignificantRateDeltaPercent = 20;

    private readonly IApplicationDbContext _db;
    private readonly CompanyClock _clock;

    public AttendanceAnomalyService(IApplicationDbContext db, CompanyClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<List<AttendanceAnomalyDto>> GetAnomaliesAsync(CancellationToken ct)
    {
        var today = await _clock.TodayAsync(ct);
        var baselineStart = today.AddDays(-BaselineWindowDays);

        var records = await _db.AttendanceRecords
            .Include(a => a.Employee)
            .Where(a => a.Date >= baselineStart && a.Date < today
                && a.Status != AttendanceStatus.Holiday && a.Status != AttendanceStatus.WeekOff && a.Status != AttendanceStatus.Leave)
            .Select(a => new { a.EmployeeId, a.Employee.FirstName, a.Employee.LastName, a.Date, a.Status })
            .ToListAsync(ct);

        var anomalies = new List<AttendanceAnomalyDto>();
        foreach (var group in records.GroupBy(r => r.EmployeeId))
        {
            var points = group.Select(r => new AttendanceHistoryPoint(r.Date, r.Status)).ToList();
            var reasons = AnalyzeEmployee(points, today);
            if (reasons.Count == 0) continue;

            var employeeName = $"{group.First().FirstName} {group.First().LastName}";
            anomalies.Add(new AttendanceAnomalyDto(group.Key, employeeName, reasons.Select(r => r.Message).ToList(), reasons.Max(r => r.DeltaPercent)));
        }

        // Largest behavior change first — that's the one HR most likely wants to see first.
        return anomalies.OrderByDescending(a => a.Severity).ToList();
    }

    // Pure, DB-free comparison of one employee's baseline period against their recent
    // period — split out from GetAnomaliesAsync so the actual detection math can be unit
    // tested without spinning up a database or seeding 60+ days of HTTP-driven attendance.
    public static List<(string Message, double DeltaPercent)> AnalyzeEmployee(List<AttendanceHistoryPoint> records, DateOnly today)
    {
        var recentStart = today.AddDays(-RecentWindowDays);
        var baselineRecords = records.Where(r => r.Date < recentStart).ToList();
        var recentRecords = records.Where(r => r.Date >= recentStart && r.Date < today).ToList();

        var results = new List<(string, double)>();

        // Not enough history yet (new hire, or the window just doesn't have enough working
        // days) — comparing against a near-empty baseline would produce meaningless
        // 0%-vs-100% swings, so skip rather than false-flag.
        if (baselineRecords.Count < MinimumRecordsForBaseline || recentRecords.Count == 0) return results;

        var baselineLateRate = PercentOf(baselineRecords, r => r.Status == AttendanceStatus.Late);
        var recentLateRate = PercentOf(recentRecords, r => r.Status == AttendanceStatus.Late);
        var lateDelta = recentLateRate - baselineLateRate;
        if (lateDelta >= SignificantRateDeltaPercent)
        {
            results.Add(($"Late arrivals up from {baselineLateRate:F0}% to {recentLateRate:F0}% of days in the last {RecentWindowDays} days.", lateDelta));
        }

        var baselineAbsentRate = PercentOf(baselineRecords, r => r.Status == AttendanceStatus.Absent);
        var recentAbsentRate = PercentOf(recentRecords, r => r.Status == AttendanceStatus.Absent);
        var absentDelta = recentAbsentRate - baselineAbsentRate;
        if (absentDelta >= SignificantRateDeltaPercent)
        {
            results.Add(($"Absences up from {baselineAbsentRate:F0}% to {recentAbsentRate:F0}% of days in the last {RecentWindowDays} days.", absentDelta));
        }

        return results;
    }

    private static double PercentOf(List<AttendanceHistoryPoint> records, Func<AttendanceHistoryPoint, bool> predicate) =>
        records.Count == 0 ? 0 : 100.0 * records.Count(predicate) / records.Count;
}
