using CarminePeople.Application.Attendance.Services;
using CarminePeople.Domain.Enums;
using Xunit;

namespace CarminePeople.UnitTests.Attendance;

public class AttendanceAnomalyServiceTests
{
    private static readonly DateOnly Today = new(2026, 8, 20);

    // Builds a list of Present days for the baseline period (days 74..15 before today), then
    // lets the caller supply the last 14 days (the "recent" window) explicitly.
    private static List<AttendanceHistoryPoint> BaselinePresentDays(int count = 50) =>
        Enumerable.Range(15, count)
            .Select(daysAgo => new AttendanceHistoryPoint(Today.AddDays(-daysAgo), AttendanceStatus.Present))
            .ToList();

    private static List<AttendanceHistoryPoint> RecentDays(int lateCount = 0, int absentCount = 0, int presentCount = 0)
    {
        var points = new List<AttendanceHistoryPoint>();
        var day = 1;
        for (var i = 0; i < lateCount; i++, day++) points.Add(new AttendanceHistoryPoint(Today.AddDays(-day), AttendanceStatus.Late));
        for (var i = 0; i < absentCount; i++, day++) points.Add(new AttendanceHistoryPoint(Today.AddDays(-day), AttendanceStatus.Absent));
        for (var i = 0; i < presentCount; i++, day++) points.Add(new AttendanceHistoryPoint(Today.AddDays(-day), AttendanceStatus.Present));
        return points;
    }

    [Fact]
    public void AnalyzeEmployee_NoBaselineChange_ReturnsNoAnomalies()
    {
        var records = BaselinePresentDays().Concat(RecentDays(presentCount: 10)).ToList();
        var result = AttendanceAnomalyService.AnalyzeEmployee(records, Today);
        Assert.Empty(result);
    }

    [Fact]
    public void AnalyzeEmployee_SignificantLateIncrease_FlagsAnomaly()
    {
        // Baseline: 0% late. Recent: 5 of 10 days late = 50% — a 50-point swing, well past
        // the 20-point significance threshold.
        var records = BaselinePresentDays().Concat(RecentDays(lateCount: 5, presentCount: 5)).ToList();
        var result = AttendanceAnomalyService.AnalyzeEmployee(records, Today);

        Assert.Single(result);
        Assert.Contains("Late arrivals up", result[0].Message);
    }

    [Fact]
    public void AnalyzeEmployee_SignificantAbsenceIncrease_FlagsAnomaly()
    {
        var records = BaselinePresentDays().Concat(RecentDays(absentCount: 4, presentCount: 6)).ToList();
        var result = AttendanceAnomalyService.AnalyzeEmployee(records, Today);

        Assert.Single(result);
        Assert.Contains("Absences up", result[0].Message);
    }

    [Fact]
    public void AnalyzeEmployee_MinorFluctuation_DoesNotFlag()
    {
        // 1 late day out of 10 recent days = 10% — below the 20-point significance threshold.
        var records = BaselinePresentDays().Concat(RecentDays(lateCount: 1, presentCount: 9)).ToList();
        var result = AttendanceAnomalyService.AnalyzeEmployee(records, Today);
        Assert.Empty(result);
    }

    [Fact]
    public void AnalyzeEmployee_InsufficientBaselineHistory_NeverFlags()
    {
        // A new hire with only 5 days of baseline history — even a dramatic recent change
        // must not be flagged, since there's no reliable baseline to compare against.
        var records = BaselinePresentDays(5).Concat(RecentDays(absentCount: 10)).ToList();
        var result = AttendanceAnomalyService.AnalyzeEmployee(records, Today);
        Assert.Empty(result);
    }

    [Fact]
    public void AnalyzeEmployee_NoRecentRecords_ReturnsNoAnomalies()
    {
        var records = BaselinePresentDays();
        var result = AttendanceAnomalyService.AnalyzeEmployee(records, Today);
        Assert.Empty(result);
    }

    [Fact]
    public void AnalyzeEmployee_BothLateAndAbsenceIncrease_FlagsBothReasons()
    {
        var records = BaselinePresentDays().Concat(RecentDays(lateCount: 4, absentCount: 4, presentCount: 2)).ToList();
        var result = AttendanceAnomalyService.AnalyzeEmployee(records, Today);
        Assert.Equal(2, result.Count);
    }
}
