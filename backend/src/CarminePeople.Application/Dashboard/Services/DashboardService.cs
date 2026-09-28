using CarminePeople.Application.Common.Interfaces;
using CarminePeople.Application.Common.Time;
using CarminePeople.Application.Dashboard.DTOs;
using CarminePeople.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CarminePeople.Application.Dashboard.Services;

public class DashboardService
{
    private readonly IApplicationDbContext _db;
    private readonly CompanyClock _clock;

    public DashboardService(IApplicationDbContext db, CompanyClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<DashboardDto> GetDashboardAsync(CancellationToken ct)
    {
        var today = await _clock.TodayAsync(ct);
        var trendStart = today.AddDays(-13); // last 14 days including today
        var monthsBack = today.AddMonths(-5); // last 6 months including current

        var totalEmployees = await _db.Employees.AsNoTracking().CountAsync(ct);

        // Counted via SQL-side GroupBy/aggregate rather than pulling every attendance
        // record into memory to count client-side.
        var todayStatusCounts = await _db.AttendanceRecords.AsNoTracking()
            .Where(a => a.Date == today)
            .GroupBy(a => a.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.Status, g => g.Count, ct);

        var kpis = new DashboardKpisDto(
            TotalEmployees: totalEmployees,
            PresentToday: todayStatusCounts.GetValueOrDefault(AttendanceStatus.Present),
            AbsentToday: todayStatusCounts.GetValueOrDefault(AttendanceStatus.Absent),
            OnLeaveToday: todayStatusCounts.GetValueOrDefault(AttendanceStatus.Leave),
            LateToday: todayStatusCounts.GetValueOrDefault(AttendanceStatus.Late),
            WfhToday: todayStatusCounts.GetValueOrDefault(AttendanceStatus.WFH));

        // Attendance trend: last 14 days, present/absent/late counts per day — grouped by
        // (Date, Status) at the SQL level instead of loading every row in the window and
        // filtering it 14 times in a loop.
        var trendGrouped = await _db.AttendanceRecords.AsNoTracking()
            .Where(a => a.Date >= trendStart && a.Date <= today)
            .GroupBy(a => new { a.Date, a.Status })
            .Select(g => new { g.Key.Date, g.Key.Status, Count = g.Count() })
            .ToListAsync(ct);
        var attendanceTrend = Enumerable.Range(0, 14)
            .Select(offset => trendStart.AddDays(offset))
            .Select(date =>
            {
                var dayRecords = trendGrouped.Where(r => r.Date == date).ToList();
                int CountOf(AttendanceStatus status) => dayRecords.FirstOrDefault(r => r.Status == status)?.Count ?? 0;
                return new AttendanceTrendPointDto(date, CountOf(AttendanceStatus.Present), CountOf(AttendanceStatus.Absent), CountOf(AttendanceStatus.Late));
            })
            .ToList();

        // Department attendance: today's present vs total headcount per department.
        var departmentHeadcounts = await _db.Employees.AsNoTracking()
            .GroupBy(e => e.Department.Name)
            .Select(g => new { DepartmentName = g.Key, Total = g.Count() })
            .ToListAsync(ct);
        var departmentPresentToday = await _db.AttendanceRecords.AsNoTracking()
            .Where(a => a.Date == today && a.Status == AttendanceStatus.Present)
            .GroupBy(a => a.Employee.Department.Name)
            .Select(g => new { DepartmentName = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.DepartmentName, g => g.Count, ct);
        var departmentAttendance = departmentHeadcounts
            .Select(d => new DepartmentAttendanceDto(d.DepartmentName, departmentPresentToday.GetValueOrDefault(d.DepartmentName), d.Total))
            .OrderByDescending(d => d.Total)
            .ToList();

        // Leave trend: applications submitted and days taken per month, last 6 months —
        // grouped by (Year, Month) at the SQL level.
        var leaveGrouped = await _db.LeaveApplications.AsNoTracking()
            .Where(l => l.StartDate >= monthsBack)
            .GroupBy(l => new { l.StartDate.Year, l.StartDate.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count(), TotalDays = g.Sum(l => l.DaysCount) })
            .ToListAsync(ct);
        var leaveTrend = Enumerable.Range(0, 6)
            .Select(offset => new DateOnly(monthsBack.Year, monthsBack.Month, 1).AddMonths(offset))
            .Select(monthStart =>
            {
                var monthData = leaveGrouped.FirstOrDefault(l => l.Year == monthStart.Year && l.Month == monthStart.Month);
                return new LeaveTrendPointDto(monthStart.ToString("MMM yyyy"), monthData?.Count ?? 0, monthData?.TotalDays ?? 0);
            })
            .ToList();

        // Working hours: average hours per day, last 14 days — averaged at the SQL level.
        var workingHoursGrouped = await _db.AttendanceRecords.AsNoTracking()
            .Where(a => a.Date >= trendStart && a.Date <= today && a.WorkingMinutes > 0)
            .GroupBy(a => a.Date)
            .Select(g => new { Date = g.Key, AvgMinutes = g.Average(a => a.WorkingMinutes) })
            .ToListAsync(ct);
        var workingHours = Enumerable.Range(0, 14)
            .Select(offset => trendStart.AddDays(offset))
            .Select(date =>
            {
                var avgMinutes = workingHoursGrouped.FirstOrDefault(r => r.Date == date)?.AvgMinutes ?? 0;
                return new WorkingHoursPointDto(date, Math.Round((decimal)avgMinutes / 60m, 2));
            })
            .ToList();

        // Recent attendance: latest 10 records company-wide.
        var recentAttendance = await _db.AttendanceRecords.AsNoTracking()
            .OrderByDescending(a => a.Date).ThenByDescending(a => a.CheckInAt)
            .Take(10)
            .Select(a => new RecentAttendanceRowDto(
                a.Id, a.Employee.FirstName + " " + a.Employee.LastName, a.Date, a.Status.ToString(), a.CheckInAt, a.CheckOutAt))
            .ToListAsync(ct);

        // Pending approvals: merges leave applications and attendance regularizations
        // awaiting any decision, newest first, capped to 10 combined.
        var pendingLeave = await _db.LeaveApplications.AsNoTracking()
            .Where(l => l.Status == LeaveApplicationStatus.Pending || l.Status == LeaveApplicationStatus.ManagerApproved)
            .OrderByDescending(l => l.CreatedAt)
            .Take(10)
            .Select(l => new PendingApprovalDto(
                l.Id, "Leave", l.Employee.FirstName + " " + l.Employee.LastName,
                $"{l.LeaveType.Name}: {l.StartDate} to {l.EndDate}", l.CreatedAt))
            .ToListAsync(ct);
        var pendingRegularizations = await _db.RegularizationRequests.AsNoTracking()
            .Where(r => r.Status == RegularizationStatus.Pending || r.Status == RegularizationStatus.ManagerApproved)
            .OrderByDescending(r => r.CreatedAt)
            .Take(10)
            .Select(r => new PendingApprovalDto(
                r.Id, "Regularization", r.Employee.FirstName + " " + r.Employee.LastName, r.Reason, r.CreatedAt))
            .ToListAsync(ct);
        var pendingApprovals = pendingLeave.Concat(pendingRegularizations)
            .OrderByDescending(p => p.RequestedAt)
            .Take(10)
            .ToList();

        // Recent employees: 5 most recently hired.
        var recentEmployees = await _db.Employees.AsNoTracking()
            .OrderByDescending(e => e.DateOfJoining)
            .Take(5)
            .Select(e => new RecentEmployeeDto(e.Id, e.FirstName + " " + e.LastName, e.Department.Name, e.Designation.Title, e.DateOfJoining))
            .ToListAsync(ct);

        // Recent announcements: 5 most recently published.
        var recentAnnouncements = await _db.Announcements.AsNoTracking()
            .OrderByDescending(a => a.PublishAt)
            .Take(5)
            .Select(a => new RecentAnnouncementDto(a.Id, a.Title, a.PublishAt))
            .ToListAsync(ct);

        return new DashboardDto(
            kpis, attendanceTrend, departmentAttendance, leaveTrend, workingHours,
            recentAttendance, pendingApprovals, recentEmployees, recentAnnouncements);
    }
}
