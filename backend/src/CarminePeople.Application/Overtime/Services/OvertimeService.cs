using CarminePeople.Application.Common.Interfaces;
using CarminePeople.Application.Common.Models;
using CarminePeople.Application.Notifications.Services;
using CarminePeople.Application.Overtime.DTOs;
using CarminePeople.Domain.Entities;
using CarminePeople.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CarminePeople.Application.Overtime.Services;

public class OvertimeService
{
    private readonly IApplicationDbContext _db;
    private readonly NotificationService _notificationService;
    private readonly IAuditLogger _auditLogger;

    public OvertimeService(IApplicationDbContext db, NotificationService notificationService, IAuditLogger auditLogger)
    {
        _db = db;
        _notificationService = notificationService;
        _auditLogger = auditLogger;
    }

    public async Task<Result<OvertimeRequestDto>> RequestAsync(Guid employeeId, RequestOvertimeRequest request, CancellationToken ct)
    {
        if (request.Hours <= 0 || request.Hours > 12)
        {
            return Result<OvertimeRequestDto>.Fail("INVALID_HOURS", "Overtime hours must be between 0 and 12 for a single day.");
        }

        var employee = await _db.Employees.FirstOrDefaultAsync(e => e.Id == employeeId, ct);
        if (employee is null) return Result<OvertimeRequestDto>.Fail("NOT_FOUND", "Employee not found.");
        if (employee.EmploymentStatus != EmploymentStatus.Active)
        {
            return Result<OvertimeRequestDto>.Fail("EMPLOYEE_INACTIVE", "Your employment status does not permit new overtime requests.");
        }

        var overtime = new OvertimeRequest
        {
            EmployeeId = employeeId,
            Date = request.Date,
            Hours = request.Hours,
            Reason = request.Reason,
            CreatedBy = employeeId
        };
        _db.OvertimeRequests.Add(overtime);
        await _db.SaveChangesAsync(ct);
        await _auditLogger.LogAsync(employeeId, "overtime.requested", "OvertimeRequest", overtime.Id.ToString(), null, ct);

        if (employee.ManagerId is Guid managerId)
        {
            await _notificationService.NotifyAsync(
                managerId,
                "New overtime request awaiting approval",
                $"{employee.FirstName} {employee.LastName} requested {request.Hours}h overtime for {request.Date:yyyy-MM-dd}.",
                "overtime",
                $"/overtime/{overtime.Id}",
                ct);
        }

        return Result<OvertimeRequestDto>.Ok(ToDto(overtime, $"{employee.FirstName} {employee.LastName}"));
    }

    public async Task<PagedResult<OvertimeRequestDto>> GetTeamPendingAsync(Guid managerId, bool unscopedVisibility, int page, int pageSize, CancellationToken ct)
    {
        var q = _db.OvertimeRequests.AsNoTracking()
            .Where(o => (unscopedVisibility || o.Employee.ManagerId == managerId) && o.Status == OvertimeRequestStatus.Pending);

        var total = await q.CountAsync(ct);
        var safePage = Math.Max(1, page);
        var safePageSize = Math.Clamp(pageSize, 1, 100);

        var items = await q.OrderBy(o => o.Date)
            .Skip((safePage - 1) * safePageSize).Take(safePageSize)
            .Select(o => new OvertimeRequestDto(
                o.Id, o.EmployeeId, o.Employee.FirstName + " " + o.Employee.LastName, o.Date, o.Hours, o.Reason,
                o.Status, o.DecisionComment))
            .ToListAsync(ct);

        return new PagedResult<OvertimeRequestDto> { Items = items, Page = safePage, PageSize = safePageSize, Total = total };
    }

    public async Task<PagedResult<OvertimeRequestDto>> GetListAsync(OvertimeQuery query, CancellationToken ct)
    {
        var q = _db.OvertimeRequests.AsNoTracking().AsQueryable();
        if (query.EmployeeId.HasValue) q = q.Where(o => o.EmployeeId == query.EmployeeId);
        if (query.Status.HasValue) q = q.Where(o => o.Status == query.Status);

        var total = await q.CountAsync(ct);
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var items = await q.OrderByDescending(o => o.Date)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(o => new OvertimeRequestDto(
                o.Id, o.EmployeeId, o.Employee.FirstName + " " + o.Employee.LastName, o.Date, o.Hours, o.Reason,
                o.Status, o.DecisionComment))
            .ToListAsync(ct);

        return new PagedResult<OvertimeRequestDto> { Items = items, Page = page, PageSize = pageSize, Total = total };
    }

    public async Task<Result<bool>> ApproveAsync(Guid overtimeId, Guid approverId, string? comment, CancellationToken ct) =>
        await DecideAsync(overtimeId, approverId, OvertimeRequestStatus.Approved, comment, ct);

    public async Task<Result<bool>> RejectAsync(Guid overtimeId, Guid approverId, string? comment, CancellationToken ct) =>
        await DecideAsync(overtimeId, approverId, OvertimeRequestStatus.Rejected, comment, ct);

    private async Task<Result<bool>> DecideAsync(Guid overtimeId, Guid approverId, OvertimeRequestStatus decision, string? comment, CancellationToken ct)
    {
        var overtime = await _db.OvertimeRequests.Include(o => o.Employee).FirstOrDefaultAsync(o => o.Id == overtimeId, ct);
        if (overtime is null) return Result<bool>.Fail("NOT_FOUND", "Overtime request not found.");
        if (overtime.Status != OvertimeRequestStatus.Pending)
        {
            return Result<bool>.Fail("INVALID_STATE", "This overtime request has already been decided.");
        }

        // A manager may only decide requests from their own reports; HR/SuperAdmin's
        // overtime.approve grant is unscoped and bypasses this check by holding the
        // broader permission the controller already gated on.
        if (overtime.Employee.ManagerId != approverId)
        {
            var approver = await _db.Employees.Include(e => e.Role).FirstOrDefaultAsync(e => e.Id == approverId, ct);
            var isElevated = approver is not null && (approver.Role.Name is "HRAdmin" or "SuperAdmin");
            if (!isElevated) return Result<bool>.Fail("FORBIDDEN", "You may only decide overtime requests for your own reports.");
        }

        overtime.Status = decision;
        overtime.DecidedByEmployeeId = approverId;
        overtime.DecidedAt = DateTime.UtcNow;
        overtime.DecisionComment = comment;
        overtime.UpdatedBy = approverId;

        await _db.SaveChangesAsync(ct);
        await _auditLogger.LogAsync(approverId, $"overtime.{decision.ToString().ToLowerInvariant()}", "OvertimeRequest", overtime.Id.ToString(), comment, ct);

        await _notificationService.NotifyAsync(
            overtime.EmployeeId,
            $"Your overtime request was {decision.ToString().ToLowerInvariant()}",
            comment ?? string.Empty,
            "overtime",
            $"/overtime/{overtime.Id}",
            ct);

        return Result<bool>.Ok(true);
    }

    public async Task<Result<bool>> CancelAsync(Guid overtimeId, Guid employeeId, CancellationToken ct)
    {
        var overtime = await _db.OvertimeRequests.FirstOrDefaultAsync(o => o.Id == overtimeId && o.EmployeeId == employeeId, ct);
        if (overtime is null) return Result<bool>.Fail("NOT_FOUND", "Overtime request not found.");
        if (overtime.Status != OvertimeRequestStatus.Pending)
        {
            return Result<bool>.Fail("INVALID_STATE", "Only a pending overtime request can be cancelled.");
        }

        overtime.Status = OvertimeRequestStatus.Cancelled;
        overtime.UpdatedBy = employeeId;
        await _db.SaveChangesAsync(ct);

        return Result<bool>.Ok(true);
    }

    private static OvertimeRequestDto ToDto(OvertimeRequest o, string employeeName) => new(
        o.Id, o.EmployeeId, employeeName, o.Date, o.Hours, o.Reason, o.Status, o.DecisionComment);
}
