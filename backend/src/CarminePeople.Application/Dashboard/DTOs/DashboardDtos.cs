namespace CarminePeople.Application.Dashboard.DTOs;

public record DashboardKpisDto(
    int TotalEmployees, int PresentToday, int AbsentToday, int OnLeaveToday, int LateToday, int WfhToday);

public record AttendanceTrendPointDto(DateOnly Date, int Present, int Absent, int Late);

public record DepartmentAttendanceDto(string DepartmentName, int Present, int Total);

public record LeaveTrendPointDto(string Month, int Applications, decimal DaysTaken);

public record WorkingHoursPointDto(DateOnly Date, decimal AverageHours);

public record RecentAttendanceRowDto(Guid Id, string EmployeeName, DateOnly Date, string Status, DateTime? CheckInAt, DateTime? CheckOutAt);

public record PendingApprovalDto(Guid Id, string Type, string EmployeeName, string Summary, DateTime RequestedAt);

public record RecentEmployeeDto(Guid Id, string FullName, string Department, string Designation, DateTime DateOfJoining);

public record RecentAnnouncementDto(Guid Id, string Title, DateTime PublishAt);

public record DashboardDto(
    DashboardKpisDto Kpis,
    List<AttendanceTrendPointDto> AttendanceTrend,
    List<DepartmentAttendanceDto> DepartmentAttendance,
    List<LeaveTrendPointDto> LeaveTrend,
    List<WorkingHoursPointDto> WorkingHours,
    List<RecentAttendanceRowDto> RecentAttendance,
    List<PendingApprovalDto> PendingApprovals,
    List<RecentEmployeeDto> RecentEmployees,
    List<RecentAnnouncementDto> RecentAnnouncements);
