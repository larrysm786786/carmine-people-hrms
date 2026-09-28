using CarminePeople.Application.Common.Interfaces;
using CarminePeople.Application.Reports.DTOs;
using CarminePeople.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CarminePeople.Application.Reports.Services;

public class ReportService
{
    private readonly IApplicationDbContext _db;

    public ReportService(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<List<AttendanceReportRow>> GetAttendanceReportAsync(ReportQuery query, CancellationToken ct)
    {
        var q = _db.AttendanceRecords.Include(a => a.Employee).ThenInclude(e => e.Department).AsQueryable();
        q = ApplyCommonFilters(q, query);
        if (query.Status.HasValue) q = q.Where(a => (int)a.Status == query.Status);

        return await q.OrderBy(a => a.Date)
            .Select(a => new AttendanceReportRow(
                a.Employee.EmployeeCode, a.Employee.FirstName + " " + a.Employee.LastName, a.Employee.Department.Name,
                a.Date, a.Status.ToString(), a.WorkingMinutes, a.CheckInAt, a.CheckOutAt))
            .ToListAsync(ct);
    }

    public async Task<List<LateReportRow>> GetLateReportAsync(ReportQuery query, CancellationToken ct)
    {
        var q = _db.AttendanceRecords.Include(a => a.Employee).Where(a => a.Status == AttendanceStatus.Late && a.CheckInAt != null).AsQueryable();
        q = ApplyCommonFilters(q, query);

        return await q.OrderBy(a => a.Date)
            .Select(a => new LateReportRow(a.Employee.EmployeeCode, a.Employee.FirstName + " " + a.Employee.LastName, a.Date, a.CheckInAt!.Value))
            .ToListAsync(ct);
    }

    public async Task<List<WorkingHoursReportRow>> GetWorkingHoursReportAsync(ReportQuery query, CancellationToken ct)
    {
        var q = _db.AttendanceRecords.Include(a => a.Employee).Where(a => a.WorkingMinutes > 0).AsQueryable();
        q = ApplyCommonFilters(q, query);

        return await q.OrderBy(a => a.Date)
            .Select(a => new WorkingHoursReportRow(a.Employee.EmployeeCode, a.Employee.FirstName + " " + a.Employee.LastName, a.Date, Math.Round(a.WorkingMinutes / 60m, 2)))
            .ToListAsync(ct);
    }

    public async Task<List<LeaveReportRow>> GetLeaveReportAsync(ReportQuery query, CancellationToken ct)
    {
        var q = _db.LeaveApplications.Include(l => l.Employee).ThenInclude(e => e.Department).Include(l => l.LeaveType).AsQueryable();

        if (query.From.HasValue) q = q.Where(l => l.EndDate >= query.From);
        if (query.To.HasValue) q = q.Where(l => l.StartDate <= query.To);
        if (query.DepartmentId.HasValue) q = q.Where(l => l.Employee.DepartmentId == query.DepartmentId);
        if (query.EmployeeId.HasValue) q = q.Where(l => l.EmployeeId == query.EmployeeId);
        if (query.ManagerId.HasValue) q = q.Where(l => l.Employee.ManagerId == query.ManagerId);
        if (query.Status.HasValue) q = q.Where(l => (int)l.Status == query.Status);

        return await q.OrderBy(l => l.StartDate)
            .Select(l => new LeaveReportRow(
                l.Employee.EmployeeCode, l.Employee.FirstName + " " + l.Employee.LastName, l.LeaveType.Name,
                l.StartDate, l.EndDate, l.DaysCount, l.Status.ToString()))
            .ToListAsync(ct);
    }

    public async Task<List<EmployeeReportRow>> GetEmployeeReportAsync(ReportQuery query, CancellationToken ct)
    {
        var q = _db.Employees.Include(e => e.Department).Include(e => e.Designation).Include(e => e.Location).AsQueryable();
        if (query.DepartmentId.HasValue) q = q.Where(e => e.DepartmentId == query.DepartmentId);
        if (query.LocationId.HasValue) q = q.Where(e => e.LocationId == query.LocationId);
        if (query.ManagerId.HasValue) q = q.Where(e => e.ManagerId == query.ManagerId);

        return await q.OrderBy(e => e.EmployeeCode)
            .Select(e => new EmployeeReportRow(
                e.EmployeeCode, e.FirstName + " " + e.LastName, e.Email, e.Department.Name, e.Designation.Title,
                e.Location.Name, e.EmploymentStatus.ToString(), e.DateOfJoining))
            .ToListAsync(ct);
    }

    private static IQueryable<Domain.Entities.AttendanceRecord> ApplyCommonFilters(IQueryable<Domain.Entities.AttendanceRecord> q, ReportQuery query)
    {
        if (query.From.HasValue) q = q.Where(a => a.Date >= query.From);
        if (query.To.HasValue) q = q.Where(a => a.Date <= query.To);
        if (query.DepartmentId.HasValue) q = q.Where(a => a.Employee.DepartmentId == query.DepartmentId);
        if (query.LocationId.HasValue) q = q.Where(a => a.LocationId == query.LocationId);
        if (query.EmployeeId.HasValue) q = q.Where(a => a.EmployeeId == query.EmployeeId);
        if (query.ManagerId.HasValue) q = q.Where(a => a.Employee.ManagerId == query.ManagerId);
        return q;
    }
}
