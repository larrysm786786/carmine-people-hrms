using CarminePeople.Domain.Enums;

namespace CarminePeople.Application.Attendance.DTOs;

public record CheckInRequest(double Latitude, double Longitude, double AccuracyMeters, string? DeviceInfo = null, float[]? FaceEmbedding = null, string? WifiSsid = null);
public record CheckOutRequest(double Latitude, double Longitude, double AccuracyMeters);

public record AttendanceRecordDto(
    Guid Id, Guid EmployeeId, string EmployeeName, string EmployeeCode, DateOnly Date,
    DateTime? CheckInAt, DateTime? CheckOutAt,
    AttendanceStatus Status, int WorkingMinutes, int BreakMinutes, Guid LocationId, CheckInMethod CheckInMethod, bool FaceVerified,
    bool IsCheckedIn);

public record BreakDto(Guid Id, DateTime BreakStartAt, DateTime? BreakEndAt, int? DurationMinutes);

public record AttendanceQuery(Guid? EmployeeId, DateOnly? From, DateOnly? To, AttendanceStatus? Status, int Page = 1, int PageSize = 20);

public record RegularizeRequest(string Reason, DateTime? RequestedCheckInAt, DateTime? RequestedCheckOutAt);

public record RegularizationDecisionRequest(string? Comment);

public record TodayScheduleDto(string? ShiftName, TimeSpan? StartTime, TimeSpan? EndTime, int GracePeriodMinutes, BreakDto? ActiveBreak, int TotalBreakMinutesToday);

public record MonthlySummaryDto(int Present, int Absent, int Leave, int Late, int WorkingHours);

public record HomeSummaryDto(TodayScheduleDto Schedule, MonthlySummaryDto MonthlySummary);

public record FaceEnrollmentRequest(float[] Embedding);
public record FaceEnrollmentStatusDto(bool IsEnrolled, DateTime? EnrolledAt);

// Severity is the largest single rate delta (percentage points) that triggered this
// anomaly, used only for sorting — it isn't a calibrated risk score.
public record AttendanceAnomalyDto(Guid EmployeeId, string EmployeeName, List<string> Reasons, double Severity);
