using CarminePeople.Application.Common.Interfaces;
using CarminePeople.Application.Common.Models;
using CarminePeople.Application.Notifications.Services;
using CarminePeople.Application.ShiftSwap.DTOs;
using CarminePeople.Domain.Entities;
using CarminePeople.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CarminePeople.Application.ShiftSwap.Services;

public class ShiftSwapService
{
    private readonly IApplicationDbContext _db;
    private readonly NotificationService _notificationService;
    private readonly IAuditLogger _auditLogger;

    public ShiftSwapService(IApplicationDbContext db, NotificationService notificationService, IAuditLogger auditLogger)
    {
        _db = db;
        _notificationService = notificationService;
        _auditLogger = auditLogger;
    }

    public async Task<Result<ShiftSwapRequestDto>> RequestAsync(Guid requesterId, RequestShiftSwapRequest request, CancellationToken ct)
    {
        if (requesterId == request.CounterpartId)
        {
            return Result<ShiftSwapRequestDto>.Fail("INVALID_COUNTERPART", "You cannot swap a shift with yourself.");
        }

        var requester = await _db.Employees.FirstOrDefaultAsync(e => e.Id == requesterId, ct);
        if (requester is null) return Result<ShiftSwapRequestDto>.Fail("NOT_FOUND", "Employee not found.");

        var counterpart = await _db.Employees.FirstOrDefaultAsync(e => e.Id == request.CounterpartId, ct);
        if (counterpart is null) return Result<ShiftSwapRequestDto>.Fail("NOT_FOUND", "Counterpart employee not found.");

        var swap = new Domain.Entities.ShiftSwapRequest
        {
            RequesterId = requesterId,
            CounterpartId = request.CounterpartId,
            RequesterDate = request.RequesterDate,
            CounterpartDate = request.CounterpartDate,
            Reason = request.Reason,
            CreatedBy = requesterId
        };
        _db.ShiftSwapRequests.Add(swap);
        await _db.SaveChangesAsync(ct);
        await _auditLogger.LogAsync(requesterId, "shift_swap.requested", "ShiftSwapRequest", swap.Id.ToString(), null, ct);

        await _notificationService.NotifyAsync(
            request.CounterpartId,
            "Shift swap request",
            $"{requester.FirstName} {requester.LastName} wants to swap their {request.RequesterDate:yyyy-MM-dd} shift for your {request.CounterpartDate:yyyy-MM-dd} shift.",
            "shift_swap",
            $"/shift-swap/{swap.Id}",
            ct);

        return Result<ShiftSwapRequestDto>.Ok(ToDto(swap, requester, counterpart));
    }

    // The counterpart must accept before this goes to a manager — this is the peer
    // half of the two-step flow, distinct from ApproveAsync/RejectAsync below.
    public async Task<Result<bool>> RespondAsync(Guid swapId, Guid counterpartId, bool accept, CancellationToken ct)
    {
        var swap = await _db.ShiftSwapRequests.Include(s => s.Requester).FirstOrDefaultAsync(s => s.Id == swapId, ct);
        if (swap is null) return Result<bool>.Fail("NOT_FOUND", "Shift swap request not found.");
        if (swap.CounterpartId != counterpartId) return Result<bool>.Fail("FORBIDDEN", "This request is not addressed to you.");
        if (swap.Status != ShiftSwapStatus.RequestedAcceptance)
        {
            return Result<bool>.Fail("INVALID_STATE", "This request has already been responded to.");
        }

        swap.Status = accept ? ShiftSwapStatus.Pending : ShiftSwapStatus.Rejected;
        swap.CounterpartRespondedAt = DateTime.UtcNow;
        swap.UpdatedBy = counterpartId;
        await _db.SaveChangesAsync(ct);
        await _auditLogger.LogAsync(counterpartId, accept ? "shift_swap.accepted" : "shift_swap.declined", "ShiftSwapRequest", swap.Id.ToString(), null, ct);

        if (accept && swap.Requester.ManagerId is Guid managerId)
        {
            await _notificationService.NotifyAsync(
                managerId,
                "Shift swap awaiting your approval",
                $"A shift swap between {swap.Requester.FirstName} {swap.Requester.LastName} and their counterpart needs your decision.",
                "shift_swap",
                $"/shift-swap/{swap.Id}",
                ct);
        }
        else if (!accept)
        {
            await _notificationService.NotifyAsync(
                swap.RequesterId,
                "Shift swap declined",
                "Your shift swap request was declined by the counterpart employee.",
                "shift_swap",
                $"/shift-swap/{swap.Id}",
                ct);
        }

        return Result<bool>.Ok(true);
    }

    public async Task<PagedResult<ShiftSwapRequestDto>> GetTeamPendingAsync(Guid managerId, bool unscopedVisibility, int page, int pageSize, CancellationToken ct)
    {
        var q = _db.ShiftSwapRequests.AsNoTracking()
            .Where(s => (unscopedVisibility || s.Requester.ManagerId == managerId) && s.Status == ShiftSwapStatus.Pending);

        var total = await q.CountAsync(ct);
        var safePage = Math.Max(1, page);
        var safePageSize = Math.Clamp(pageSize, 1, 100);

        var items = await q.OrderBy(s => s.RequesterDate)
            .Skip((safePage - 1) * safePageSize).Take(safePageSize)
            .Select(s => new ShiftSwapRequestDto(
                s.Id, s.RequesterId, s.Requester.FirstName + " " + s.Requester.LastName,
                s.CounterpartId, s.Counterpart.FirstName + " " + s.Counterpart.LastName,
                s.RequesterDate, s.CounterpartDate, s.Reason, s.Status, s.DecisionComment))
            .ToListAsync(ct);

        return new PagedResult<ShiftSwapRequestDto> { Items = items, Page = safePage, PageSize = safePageSize, Total = total };
    }

    public async Task<PagedResult<ShiftSwapRequestDto>> GetListAsync(ShiftSwapQuery query, CancellationToken ct)
    {
        var q = _db.ShiftSwapRequests.AsNoTracking().AsQueryable();
        if (query.EmployeeId.HasValue)
        {
            q = q.Where(s => s.RequesterId == query.EmployeeId || s.CounterpartId == query.EmployeeId);
        }
        if (query.Status.HasValue) q = q.Where(s => s.Status == query.Status);

        var total = await q.CountAsync(ct);
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var items = await q.OrderByDescending(s => s.RequesterDate)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(s => new ShiftSwapRequestDto(
                s.Id, s.RequesterId, s.Requester.FirstName + " " + s.Requester.LastName,
                s.CounterpartId, s.Counterpart.FirstName + " " + s.Counterpart.LastName,
                s.RequesterDate, s.CounterpartDate, s.Reason, s.Status, s.DecisionComment))
            .ToListAsync(ct);

        return new PagedResult<ShiftSwapRequestDto> { Items = items, Page = page, PageSize = pageSize, Total = total };
    }

    public async Task<Result<bool>> ApproveAsync(Guid swapId, Guid approverId, string? comment, CancellationToken ct) =>
        await DecideAsync(swapId, approverId, ShiftSwapStatus.Approved, comment, ct);

    public async Task<Result<bool>> RejectAsync(Guid swapId, Guid approverId, string? comment, CancellationToken ct) =>
        await DecideAsync(swapId, approverId, ShiftSwapStatus.Rejected, comment, ct);

    private async Task<Result<bool>> DecideAsync(Guid swapId, Guid approverId, ShiftSwapStatus decision, string? comment, CancellationToken ct)
    {
        var swap = await _db.ShiftSwapRequests.Include(s => s.Requester).FirstOrDefaultAsync(s => s.Id == swapId, ct);
        if (swap is null) return Result<bool>.Fail("NOT_FOUND", "Shift swap request not found.");
        if (swap.Status != ShiftSwapStatus.Pending)
        {
            return Result<bool>.Fail("INVALID_STATE", "This request is not awaiting manager decision.");
        }

        if (swap.Requester.ManagerId != approverId)
        {
            var approver = await _db.Employees.Include(e => e.Role).FirstOrDefaultAsync(e => e.Id == approverId, ct);
            var isElevated = approver is not null && (approver.Role.Name is "HRAdmin" or "SuperAdmin");
            if (!isElevated) return Result<bool>.Fail("FORBIDDEN", "You may only decide shift swaps for your own reports.");
        }

        swap.Status = decision;
        swap.DecidedByEmployeeId = approverId;
        swap.DecidedAt = DateTime.UtcNow;
        swap.DecisionComment = comment;
        swap.UpdatedBy = approverId;

        await _db.SaveChangesAsync(ct);
        await _auditLogger.LogAsync(approverId, $"shift_swap.{decision.ToString().ToLowerInvariant()}", "ShiftSwapRequest", swap.Id.ToString(), comment, ct);

        foreach (var employeeId in new[] { swap.RequesterId, swap.CounterpartId })
        {
            await _notificationService.NotifyAsync(
                employeeId,
                $"Shift swap {decision.ToString().ToLowerInvariant()}",
                comment ?? string.Empty,
                "shift_swap",
                $"/shift-swap/{swap.Id}",
                ct);
        }

        return Result<bool>.Ok(true);
    }

    public async Task<Result<bool>> CancelAsync(Guid swapId, Guid requesterId, CancellationToken ct)
    {
        var swap = await _db.ShiftSwapRequests.FirstOrDefaultAsync(s => s.Id == swapId && s.RequesterId == requesterId, ct);
        if (swap is null) return Result<bool>.Fail("NOT_FOUND", "Shift swap request not found.");
        if (swap.Status is ShiftSwapStatus.Approved or ShiftSwapStatus.Rejected or ShiftSwapStatus.Cancelled)
        {
            return Result<bool>.Fail("INVALID_STATE", "This request can no longer be cancelled.");
        }

        swap.Status = ShiftSwapStatus.Cancelled;
        swap.UpdatedBy = requesterId;
        await _db.SaveChangesAsync(ct);

        return Result<bool>.Ok(true);
    }

    private static ShiftSwapRequestDto ToDto(Domain.Entities.ShiftSwapRequest swap, Employee requester, Employee counterpart) => new(
        swap.Id, swap.RequesterId, $"{requester.FirstName} {requester.LastName}",
        swap.CounterpartId, $"{counterpart.FirstName} {counterpart.LastName}",
        swap.RequesterDate, swap.CounterpartDate, swap.Reason, swap.Status, swap.DecisionComment);
}
