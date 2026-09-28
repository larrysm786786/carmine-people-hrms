namespace CarminePeople.Application.Reports.DTOs;

public record ReportQuery(DateOnly? From, DateOnly? To, Guid? DepartmentId, Guid? LocationId, Guid? EmployeeId, Guid? ManagerId = null, int? Status = null);

public record AttendanceReportRow(
    string EmployeeCode, string EmployeeName, string Department, DateOnly Date,
    string Status, int WorkingMinutes, DateTime? CheckInAt, DateTime? CheckOutAt);

public record LeaveReportRow(
    string EmployeeCode, string EmployeeName, string LeaveType, DateOnly StartDate, DateOnly EndDate, decimal DaysCount, string Status);

public record EmployeeReportRow(
    string EmployeeCode, string FullName, string Email, string Department, string Designation, string Location, string Status, DateTime DateOfJoining);

public record LateReportRow(string EmployeeCode, string EmployeeName, DateOnly Date, DateTime CheckInAt);

public record WorkingHoursReportRow(string EmployeeCode, string EmployeeName, DateOnly Date, decimal Hours);
