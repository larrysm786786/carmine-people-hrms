using CarminePeople.Application.Common.Interfaces;
using CarminePeople.Application.Common.Models;
using CarminePeople.Application.Leave.DTOs;
using CarminePeople.Application.Notifications.Services;
using CarminePeople.Domain.Constants;
using CarminePeople.Domain.Entities;
using CarminePeople.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CarminePeople.Application.Leave.Services;

public class LeaveService
{
    private readonly IApplicationDbContext _db;
    private readonly NotificationService _notificationService;
    private readonly IAuditLogger _auditLogger;

    public LeaveService(IApplicationDbContext db, NotificationService notificationService, IAuditLogger auditLogger)
    {
        _db = db;
        _notificationService = notificationService;
        _auditLogger = auditLogger;
    }

    public async Task<List<LeaveTypeDto>> GetTypesAsync(CancellationToken ct) =>
        await _db.LeaveTypes
            .Select(lt => new LeaveTypeDto(lt.Id, lt.Name, lt.IsPaid, lt.RequiresAttachment, lt.DefaultAnnualDays))
            .ToListAsync(ct);

    public async Task<Result<LeaveTypeDto>> CreateTypeAsync(CreateLeaveTypeRequest request, Guid actorId, CancellationToken ct)
    {
        if (await _db.LeaveTypes.AnyAsync(lt => lt.Name == request.Name, ct))
        {
            return Result<LeaveTypeDto>.Fail("NAME_TAKEN", "A leave type with this name already exists.");
        }

        var leaveType = new LeaveType
        {
            Name = request.Name,
            IsPaid = request.IsPaid,
            RequiresAttachment = request.RequiresAttachment,
            DefaultAnnualDays = request.DefaultAnnualDays,
            CreatedBy = actorId
        };
        _db.LeaveTypes.Add(leaveType);
        await _db.SaveChangesAsync(ct);
        await _auditLogger.LogAsync(actorId, "leave_type.created", "LeaveType", leaveType.Id.ToString(), null, ct);

        return Result<LeaveTypeDto>.Ok(new LeaveTypeDto(leaveType.Id, leaveType.Name, leaveType.IsPaid, leaveType.RequiresAttachment, leaveType.DefaultAnnualDays));
    }

    public async Task<Result<LeaveTypeDto>> UpdateTypeAsync(Guid id, UpdateLeaveTypeRequest request, Guid actorId, CancellationToken ct)
    {
        var leaveType = await _db.LeaveTypes.FirstOrDefaultAsync(lt => lt.Id == id, ct);
        if (leaveType is null) return Result<LeaveTypeDto>.Fail("NOT_FOUND", "Leave type not found.");

        if (await _db.LeaveTypes.AnyAsync(lt => lt.Id != id && lt.Name == request.Name, ct))
        {
            return Result<LeaveTypeDto>.Fail("NAME_TAKEN", "A leave type with this name already exists.");
        }

        leaveType.Name = request.Name;
        leaveType.IsPaid = request.IsPaid;
        leaveType.RequiresAttachment = request.RequiresAttachment;
        leaveType.DefaultAnnualDays = request.DefaultAnnualDays;
        leaveType.UpdatedBy = actorId;

        await _db.SaveChangesAsync(ct);
        await _auditLogger.LogAsync(actorId, "leave_type.updated", "LeaveType", leaveType.Id.ToString(), null, ct);

        return Result<LeaveTypeDto>.Ok(new LeaveTypeDto(leaveType.Id, leaveType.Name, leaveType.IsPaid, leaveType.RequiresAttachment, leaveType.DefaultAnnualDays));
    }

    public async Task<List<LeaveBalanceDto>> GetBalancesAsync(Guid employeeId, int? year, CancellationToken ct)
    {
        var targetYear = year ?? DateTime.UtcNow.Year;
        return await _db.LeaveBalances.AsNoTracking()
            .Where(lb => lb.EmployeeId == employeeId && lb.Year == targetYear)
            .Select(lb => new LeaveBalanceDto(lb.LeaveTypeId, lb.LeaveType.Name, lb.Year, lb.Available, lb.Used, lb.Pending))
            .ToListAsync(ct);
    }

    // Bulk-allocates each active employee's leave balance for a year, using every leave type's
    // DefaultAnnualDays. Only fills in gaps — an employee/type/year combination that already has
    // a balance row is left untouched, so re-running this (e.g. next year, or after adding a new
    // leave type) never overwrites Used/Pending an employee has already accrued.
    public async Task<int> AllocateDefaultBalancesAsync(int? year, Guid actorId, CancellationToken ct)
    {
        var targetYear = year ?? DateTime.UtcNow.Year;

        var activeEmployeeIds = await _db.Employees
            .Where(e => e.EmploymentStatus == EmploymentStatus.Active)
            .Select(e => e.Id)
            .ToListAsync(ct);
        var leaveTypes = await _db.LeaveTypes.ToListAsync(ct);

        var existing = await _db.LeaveBalances
            .Where(lb => lb.Year == targetYear)
            .Select(lb => new { lb.EmployeeId, lb.LeaveTypeId })
            .ToListAsync(ct);
        var existingSet = existing.Select(e => (e.EmployeeId, e.LeaveTypeId)).ToHashSet();

        var created = 0;
        foreach (var employeeId in activeEmployeeIds)
        {
            foreach (var leaveType in leaveTypes)
            {
                if (existingSet.Contains((employeeId, leaveType.Id))) continue;

                _db.LeaveBalances.Add(new LeaveBalance
                {
                    EmployeeId = employeeId,
                    LeaveTypeId = leaveType.Id,
                    Year = targetYear,
                    Available = leaveType.DefaultAnnualDays,
                    Used = 0,
                    Pending = 0,
                });
                created++;
            }
        }

        if (created > 0)
        {
            await _db.SaveChangesAsync(ct);
            await _auditLogger.LogAsync(actorId, "leave_balance.bulk_allocated", "LeaveBalance", null, new { targetYear, created }, ct);
        }

        return created;
    }

    public async Task<Result<LeaveApplicationDto>> ApplyAsync(Guid employeeId, ApplyLeaveRequest request, CancellationToken ct)
    {
        var applicant = await _db.Employees.FirstOrDefaultAsync(e => e.Id == employeeId, ct);
        if (applicant is null) return Result<LeaveApplicationDto>.Fail("NOT_FOUND", "Employee not found.");
        if (applicant.EmploymentStatus != EmploymentStatus.Active)
        {
            return Result<LeaveApplicationDto>.Fail("EMPLOYEE_INACTIVE", "Your employment status does not permit new leave applications.");
        }

        var leaveType = await _db.LeaveTypes.FirstOrDefaultAsync(lt => lt.Id == request.LeaveTypeId, ct);
        if (leaveType is null) return Result<LeaveApplicationDto>.Fail("NOT_FOUND", "Leave type not found.");

        if (leaveType.RequiresAttachment && string.IsNullOrWhiteSpace(request.AttachmentUrl))
        {
            return Result<LeaveApplicationDto>.Fail("ATTACHMENT_REQUIRED", "This leave type requires an attachment.");
        }

        if (request.IsHalfDay && request.StartDate != request.EndDate)
        {
            return Result<LeaveApplicationDto>.Fail("INVALID_HALF_DAY", "Half-day leave can only be applied for a single date.");
        }

        var holidayDates = await _db.Holidays
            .Where(h => h.Date >= request.StartDate && h.Date <= request.EndDate)
            .Select(h => h.Date)
            .ToListAsync(ct);

        var daysCount = LeaveDayCounter.CountWorkingDays(request.StartDate, request.EndDate, holidayDates.ToHashSet(), request.IsHalfDay);
        if (daysCount <= 0)
        {
            return Result<LeaveApplicationDto>.Fail("NO_WORKING_DAYS", "The selected date range contains no working days.");
        }

        // Block date ranges that overlap an application still in flight (or already
        // finalized as Approved) for this employee — two standard interval-overlap
        // comparisons cover every overlap shape (partial, nested, identical).
        var hasOverlap = await _db.LeaveApplications.AnyAsync(
            la => la.EmployeeId == employeeId
                && la.Status != LeaveApplicationStatus.Rejected && la.Status != LeaveApplicationStatus.Cancelled
                && la.StartDate <= request.EndDate && la.EndDate >= request.StartDate, ct);
        if (hasOverlap)
        {
            return Result<LeaveApplicationDto>.Fail("OVERLAPPING_LEAVE", "You already have a leave application covering part of this date range.");
        }

        var year = request.StartDate.Year;
        var balance = await _db.LeaveBalances.FirstOrDefaultAsync(
            lb => lb.EmployeeId == employeeId && lb.LeaveTypeId == request.LeaveTypeId && lb.Year == year, ct);
        if (balance is null)
        {
            return Result<LeaveApplicationDto>.Fail("NO_BALANCE", "No leave balance found for this leave type/year.");
        }
        if (balance.Available - balance.Pending < daysCount)
        {
            return Result<LeaveApplicationDto>.Fail("INSUFFICIENT_BALANCE", "Insufficient leave balance for the requested dates.");
        }

        var application = new LeaveApplication
        {
            EmployeeId = employeeId,
            LeaveTypeId = request.LeaveTypeId,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            DaysCount = daysCount,
            IsHalfDay = request.IsHalfDay,
            Reason = request.Reason,
            AttachmentUrl = request.AttachmentUrl,
            Status = LeaveApplicationStatus.Pending
        };
        _db.LeaveApplications.Add(application);

        balance.Pending += daysCount;

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Two concurrent applications for the same employee/type/year both read the
            // balance and passed the availability check above before either committed; the
            // RowVersion concurrency token on LeaveBalance catches the loser here instead of
            // silently overwriting the first application's Pending update (which would let
            // the employee overdraw their balance).
            return Result<LeaveApplicationDto>.Fail("CONCURRENT_UPDATE", "Your leave balance was just updated elsewhere — please try again.");
        }

        await _notificationService.NotifyAsync(
            employeeId,
            "Leave request submitted",
            $"Your leave request for {request.StartDate:yyyy-MM-dd} to {request.EndDate:yyyy-MM-dd} has been submitted and is pending approval.",
            "leave",
            $"/leave/{application.Id}",
            ct);

        var employee = await _db.Employees.FirstOrDefaultAsync(e => e.Id == employeeId, ct);
        if (employee?.ManagerId is Guid managerId)
        {
            await _notificationService.NotifyAsync(
                managerId,
                "New leave request awaiting approval",
                $"{employee.FirstName} {employee.LastName} requested leave for {request.StartDate:yyyy-MM-dd} to {request.EndDate:yyyy-MM-dd}.",
                "leave",
                $"/leave/{application.Id}",
                ct);
        }

        return Result<LeaveApplicationDto>.Ok(ToDto(application, leaveType.Name, $"{employee?.FirstName} {employee?.LastName}".Trim()));
    }

    // Requests awaiting decision — used by the approvals screen, distinct from
    // GetListAsync's "one employee's own history" query. A plain manager sees only their
    // direct reports' pending requests; HR/SuperAdmin (unscopedVisibility) see every
    // pending request company-wide, since they can decide on behalf of any manager.
    public async Task<PagedResult<LeaveApplicationDto>> GetTeamPendingAsync(Guid managerId, bool unscopedVisibility, int page, int pageSize, CancellationToken ct)
    {
        var q = _db.LeaveApplications.AsNoTracking()
            .Where(la => (unscopedVisibility || la.Employee.ManagerId == managerId) &&
                (la.Status == LeaveApplicationStatus.Pending || la.Status == LeaveApplicationStatus.ManagerApproved));

        var total = await q.CountAsync(ct);
        var safePage = Math.Max(1, page);
        var safePageSize = Math.Clamp(pageSize, 1, 100);

        var items = await q.OrderBy(la => la.StartDate)
            .Skip((safePage - 1) * safePageSize).Take(safePageSize)
            .Select(la => new LeaveApplicationDto(la.Id, la.EmployeeId, la.Employee.FirstName + " " + la.Employee.LastName, la.LeaveTypeId, la.LeaveType.Name,
                la.StartDate, la.EndDate, la.DaysCount, la.Reason, la.Status, la.IsHalfDay))
            .ToListAsync(ct);

        return new PagedResult<LeaveApplicationDto> { Items = items, Page = safePage, PageSize = safePageSize, Total = total };
    }

    public async Task<PagedResult<LeaveApplicationDto>> GetListAsync(LeaveApplicationQuery query, CancellationToken ct)
    {
        var q = _db.LeaveApplications.AsNoTracking().AsQueryable();
        if (query.EmployeeId.HasValue) q = q.Where(la => la.EmployeeId == query.EmployeeId);
        if (query.Status.HasValue) q = q.Where(la => la.Status == query.Status);
        if (query.LeaveTypeId.HasValue) q = q.Where(la => la.LeaveTypeId == query.LeaveTypeId);

        var total = await q.CountAsync(ct);
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var items = await q.OrderByDescending(la => la.StartDate)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(la => new LeaveApplicationDto(la.Id, la.EmployeeId, la.Employee.FirstName + " " + la.Employee.LastName, la.LeaveTypeId, la.LeaveType.Name,
                la.StartDate, la.EndDate, la.DaysCount, la.Reason, la.Status, la.IsHalfDay))
            .ToListAsync(ct);

        return new PagedResult<LeaveApplicationDto> { Items = items, Page = page, PageSize = pageSize, Total = total };
    }

    public async Task<Result<bool>> ApproveAsync(Guid leaveApplicationId, Guid approverId, string? comment, CancellationToken ct) =>
        await DecideAsync(leaveApplicationId, approverId, LeaveApplicationStatus.Approved, comment, ct);

    public async Task<Result<bool>> RejectAsync(Guid leaveApplicationId, Guid approverId, string? comment, CancellationToken ct) =>
        await DecideAsync(leaveApplicationId, approverId, LeaveApplicationStatus.Rejected, comment, ct);

    public async Task<Result<bool>> CancelAsync(Guid leaveApplicationId, Guid employeeId, CancellationToken ct)
    {
        var application = await _db.LeaveApplications.FirstOrDefaultAsync(la => la.Id == leaveApplicationId && la.EmployeeId == employeeId, ct);
        if (application is null) return Result<bool>.Fail("NOT_FOUND", "Leave application not found.");
        if (application.Status is not (LeaveApplicationStatus.Pending or LeaveApplicationStatus.ManagerApproved))
        {
            return Result<bool>.Fail("INVALID_STATE", "Only pending or manager-approved leave applications can be cancelled.");
        }

        application.Status = LeaveApplicationStatus.Cancelled;
        await RestoreBalanceAsync(application, ct);

        await _db.SaveChangesAsync(ct);
        await _auditLogger.LogAsync(employeeId, "leave.cancelled", "LeaveApplication", application.Id.ToString(), null, ct);

        await _notificationService.NotifyAsync(
            employeeId,
            "Leave request cancelled",
            $"Your leave request for {application.StartDate:yyyy-MM-dd} to {application.EndDate:yyyy-MM-dd} has been cancelled.",
            "leave",
            $"/leave/{application.Id}",
            ct);

        return Result<bool>.Ok(true);
    }

    // decision must be Approved or Rejected — the caller's intent. A Manager's "Approved"
    // intent is downgraded to the intermediate ManagerApproved status (per the spec's
    // Employee -> Manager -> HR if required chain); HR/SuperAdmin approval finalizes it.
    // Rejection is always final, from either role, at either stage.
    private async Task<Result<bool>> DecideAsync(Guid leaveApplicationId, Guid approverId, LeaveApplicationStatus decision, string? comment, CancellationToken ct)
    {
        var application = await _db.LeaveApplications.FirstOrDefaultAsync(la => la.Id == leaveApplicationId, ct);
        if (application is null) return Result<bool>.Fail("NOT_FOUND", "Leave application not found.");
        if (application.Status is not (LeaveApplicationStatus.Pending or LeaveApplicationStatus.ManagerApproved))
        {
            return Result<bool>.Fail("ALREADY_DECIDED", "This leave application has already been finalized.");
        }

        // A Manager may only decide leave for their own direct reports; HR/SuperAdmin
        // (identified by also holding employee.manage) can decide for anyone.
        var approver = await _db.Employees.Include(e => e.Role).FirstOrDefaultAsync(e => e.Id == approverId, ct);
        var isPrivileged = RoleNames.IsPrivileged(approver?.Role.Name);
        if (!isPrivileged)
        {
            var applicant = await _db.Employees.FirstOrDefaultAsync(e => e.Id == application.EmployeeId, ct);
            if (applicant?.ManagerId != approverId)
            {
                return Result<bool>.Fail("FORBIDDEN", "You can only approve or reject leave for your direct reports.");
            }
        }

        // A Manager cannot re-decide a request that's already past their stage (ManagerApproved
        // awaiting HR); only HR/SuperAdmin may act at that point.
        if (application.Status == LeaveApplicationStatus.ManagerApproved && !isPrivileged)
        {
            return Result<bool>.Fail("FORBIDDEN", "This request has already been approved by a manager and now requires HR action.");
        }

        var isFinal = decision == LeaveApplicationStatus.Rejected || isPrivileged;
        application.Status = isFinal ? decision : LeaveApplicationStatus.ManagerApproved;

        _db.LeaveApprovals.Add(new LeaveApproval
        {
            LeaveApplicationId = application.Id,
            ApproverId = approverId,
            Action = application.Status,
            ActedAt = DateTime.UtcNow,
            Comment = comment
        });

        if (isFinal)
        {
            var balance = await _db.LeaveBalances.FirstOrDefaultAsync(
                lb => lb.EmployeeId == application.EmployeeId && lb.LeaveTypeId == application.LeaveTypeId && lb.Year == application.StartDate.Year, ct);

            if (balance is not null)
            {
                balance.Pending = Math.Max(0, balance.Pending - application.DaysCount);
                if (decision == LeaveApplicationStatus.Approved)
                {
                    balance.Used += application.DaysCount;
                }
            }
        }

        await _db.SaveChangesAsync(ct);
        await _auditLogger.LogAsync(approverId, $"leave.{application.Status.ToString().ToLowerInvariant()}", "LeaveApplication", application.Id.ToString(), new { comment }, ct);

        var (title, body) = application.Status switch
        {
            LeaveApplicationStatus.ManagerApproved => (
                "Leave request approved by manager",
                $"Your leave request for {application.StartDate:yyyy-MM-dd} to {application.EndDate:yyyy-MM-dd} was approved by your manager and is now awaiting HR approval."),
            LeaveApplicationStatus.Approved => (
                "Leave request approved",
                $"Your leave request for {application.StartDate:yyyy-MM-dd} to {application.EndDate:yyyy-MM-dd} was approved."),
            _ => (
                "Leave request rejected",
                $"Your leave request for {application.StartDate:yyyy-MM-dd} to {application.EndDate:yyyy-MM-dd} was rejected."),
        };
        await _notificationService.NotifyAsync(application.EmployeeId, title, body, "leave", $"/leave/{application.Id}", ct);

        return Result<bool>.Ok(true);
    }

    private async Task RestoreBalanceAsync(LeaveApplication application, CancellationToken ct)
    {
        var balance = await _db.LeaveBalances.FirstOrDefaultAsync(
            lb => lb.EmployeeId == application.EmployeeId && lb.LeaveTypeId == application.LeaveTypeId && lb.Year == application.StartDate.Year, ct);
        if (balance is not null)
        {
            balance.Pending = Math.Max(0, balance.Pending - application.DaysCount);
        }
    }

    private static LeaveApplicationDto ToDto(LeaveApplication la, string leaveTypeName, string employeeName) => new(
        la.Id, la.EmployeeId, employeeName, la.LeaveTypeId, leaveTypeName, la.StartDate, la.EndDate, la.DaysCount, la.Reason, la.Status, la.IsHalfDay);
}
