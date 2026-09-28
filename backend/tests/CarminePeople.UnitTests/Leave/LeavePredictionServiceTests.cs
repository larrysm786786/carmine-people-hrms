using CarminePeople.Application.Leave.Services;
using Xunit;

namespace CarminePeople.UnitTests.Leave;

public class LeavePredictionServiceTests
{
    [Fact]
    public void AnalyzeEmployee_TookLeaveInTargetMonthBothYears_PredictsHighLikelihood()
    {
        var history = new List<LeaveHistoryPoint> { new(2024, 12), new(2025, 12) };
        var result = LeavePredictionService.AnalyzeEmployee(history, targetMonth: 12);

        Assert.NotNull(result);
        Assert.Equal(100, result!.Value.LikelihoodPercent);
    }

    [Fact]
    public void AnalyzeEmployee_TookLeaveInTargetMonthOnlyOnceOfThreeYears_DoesNotPredict()
    {
        // 1 of 3 years = 33%, below the 50% recurrence threshold.
        var history = new List<LeaveHistoryPoint> { new(2023, 6), new(2024, 12), new(2025, 3) };
        var result = LeavePredictionService.AnalyzeEmployee(history, targetMonth: 6);
        Assert.Null(result);
    }

    [Fact]
    public void AnalyzeEmployee_OnlyOneYearOfHistory_NeverPredictsRegardlessOfRate()
    {
        // A single year would compute to 100% recurrence — exactly the case the minimum
        // years-of-history guard exists to prevent from being treated as a real pattern.
        var history = new List<LeaveHistoryPoint> { new(2025, 12) };
        var result = LeavePredictionService.AnalyzeEmployee(history, targetMonth: 12);
        Assert.Null(result);
    }

    [Fact]
    public void AnalyzeEmployee_NoHistoryInTargetMonth_DoesNotPredict()
    {
        var history = new List<LeaveHistoryPoint> { new(2024, 3), new(2025, 4) };
        var result = LeavePredictionService.AnalyzeEmployee(history, targetMonth: 12);
        Assert.Null(result);
    }

    [Fact]
    public void AnalyzeEmployee_ExactlyAtThreshold_Predicts()
    {
        // 2 of 4 years = exactly 50%, the minimum recurrence rate — must be included, not excluded.
        var history = new List<LeaveHistoryPoint>
        {
            new(2022, 6), new(2023, 1), new(2024, 6), new(2025, 2),
        };
        var result = LeavePredictionService.AnalyzeEmployee(history, targetMonth: 6);

        Assert.NotNull(result);
        Assert.Equal(50, result!.Value.LikelihoodPercent);
    }
}
