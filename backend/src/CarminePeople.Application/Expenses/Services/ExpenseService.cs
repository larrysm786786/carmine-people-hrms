using CarminePeople.Application.Common.Interfaces;
using CarminePeople.Application.Common.Models;
using CarminePeople.Application.Expenses.DTOs;
using CarminePeople.Application.Notifications.Services;
using CarminePeople.Domain.Entities;
using CarminePeople.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CarminePeople.Application.Expenses.Services;

public class ExpenseService
{
    private readonly IApplicationDbContext _db;
    private readonly NotificationService _notificationService;
    private readonly IAuditLogger _auditLogger;

    public ExpenseService(IApplicationDbContext db, NotificationService notificationService, IAuditLogger auditLogger)
    {
        _db = db;
        _notificationService = notificationService;
        _auditLogger = auditLogger;
    }

    public async Task<Result<ExpenseDto>> SubmitAsync(Guid employeeId, SubmitExpenseRequest request, CancellationToken ct)
    {
        var employee = await _db.Employees.FirstOrDefaultAsync(e => e.Id == employeeId, ct);
        if (employee is null) return Result<ExpenseDto>.Fail("NOT_FOUND", "Employee not found.");

        var expense = new Expense
        {
            EmployeeId = employeeId,
            Category = request.Category,
            Amount = request.Amount,
            ExpenseDate = request.ExpenseDate,
            Description = request.Description,
            ReceiptUrl = request.ReceiptUrl,
            CreatedBy = employeeId,
        };
        _db.Expenses.Add(expense);
        await _db.SaveChangesAsync(ct);
        await _auditLogger.LogAsync(employeeId, "expense.submitted", "Expense", expense.Id.ToString(), null, ct);

        if (employee.ManagerId.HasValue)
        {
            await _notificationService.NotifyAsync(
                employee.ManagerId.Value,
                "New expense claim awaiting approval",
                $"{employee.FirstName} {employee.LastName} submitted a {request.Category} expense of {request.Amount:0.00}.",
                "expense",
                $"/expenses/{expense.Id}",
                ct);
        }

        return Result<ExpenseDto>.Ok(ToDto(expense, $"{employee.FirstName} {employee.LastName}".Trim(), null));
    }

    // Approvals screen — a plain manager sees only their direct reports' pending claims;
    // HR/SuperAdmin (unscopedVisibility) see every pending claim company-wide.
    public async Task<PagedResult<ExpenseDto>> GetTeamPendingAsync(Guid managerId, bool unscopedVisibility, int page, int pageSize, CancellationToken ct)
    {
        var q = _db.Expenses.AsNoTracking()
            .Where(e => (unscopedVisibility || e.Employee.ManagerId == managerId) && e.Status == ExpenseStatus.Pending);

        var total = await q.CountAsync(ct);
        var safePage = Math.Max(1, page);
        var safePageSize = Math.Clamp(pageSize, 1, 100);

        var items = await q.OrderBy(e => e.ExpenseDate)
            .Skip((safePage - 1) * safePageSize).Take(safePageSize)
            .Select(e => new ExpenseDto(
                e.Id, e.EmployeeId, e.Employee.FirstName + " " + e.Employee.LastName, e.Category, e.Amount, e.ExpenseDate,
                e.Description, e.ReceiptUrl, e.Status,
                e.DecidedBy == null ? null : e.DecidedBy.FirstName + " " + e.DecidedBy.LastName, e.DecidedAt, e.DecisionComment, e.ReimbursedAt, e.CreatedAt))
            .ToListAsync(ct);

        return new PagedResult<ExpenseDto> { Items = items, Page = safePage, PageSize = safePageSize, Total = total };
    }

    public async Task<PagedResult<ExpenseDto>> GetListAsync(ExpenseQuery query, CancellationToken ct)
    {
        var q = _db.Expenses.AsNoTracking().AsQueryable();
        if (query.EmployeeId.HasValue) q = q.Where(e => e.EmployeeId == query.EmployeeId);
        if (query.Status.HasValue) q = q.Where(e => e.Status == query.Status);

        var total = await q.CountAsync(ct);
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var items = await q.OrderByDescending(e => e.ExpenseDate)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(e => new ExpenseDto(
                e.Id, e.EmployeeId, e.Employee.FirstName + " " + e.Employee.LastName, e.Category, e.Amount, e.ExpenseDate,
                e.Description, e.ReceiptUrl, e.Status,
                e.DecidedBy == null ? null : e.DecidedBy.FirstName + " " + e.DecidedBy.LastName, e.DecidedAt, e.DecisionComment, e.ReimbursedAt, e.CreatedAt))
            .ToListAsync(ct);

        return new PagedResult<ExpenseDto> { Items = items, Page = page, PageSize = pageSize, Total = total };
    }

    public async Task<Result<bool>> ApproveAsync(Guid id, Guid approverId, string? comment, CancellationToken ct) =>
        await DecideAsync(id, approverId, ExpenseStatus.Approved, comment, ct);

    public async Task<Result<bool>> RejectAsync(Guid id, Guid approverId, string? comment, CancellationToken ct) =>
        await DecideAsync(id, approverId, ExpenseStatus.Rejected, comment, ct);

    private async Task<Result<bool>> DecideAsync(Guid id, Guid approverId, ExpenseStatus decision, string? comment, CancellationToken ct)
    {
        var expense = await _db.Expenses.FirstOrDefaultAsync(e => e.Id == id, ct);
        if (expense is null) return Result<bool>.Fail("NOT_FOUND", "Expense claim not found.");
        if (expense.Status != ExpenseStatus.Pending)
        {
            return Result<bool>.Fail("INVALID_STATE", "Only pending expense claims can be approved or rejected.");
        }

        expense.Status = decision;
        expense.DecidedById = approverId;
        expense.DecidedAt = DateTime.UtcNow;
        expense.DecisionComment = comment;
        expense.UpdatedBy = approverId;
        await _db.SaveChangesAsync(ct);
        await _auditLogger.LogAsync(approverId, decision == ExpenseStatus.Approved ? "expense.approved" : "expense.rejected", "Expense", expense.Id.ToString(), null, ct);

        await _notificationService.NotifyAsync(
            expense.EmployeeId,
            decision == ExpenseStatus.Approved ? "Expense claim approved" : "Expense claim rejected",
            decision == ExpenseStatus.Approved
                ? $"Your {expense.Category} expense of {expense.Amount:0.00} was approved."
                : $"Your {expense.Category} expense of {expense.Amount:0.00} was rejected.",
            "expense",
            $"/expenses/{expense.Id}",
            ct);

        return Result<bool>.Ok(true);
    }

    public async Task<Result<bool>> ReimburseAsync(Guid id, Guid actorId, CancellationToken ct)
    {
        var expense = await _db.Expenses.FirstOrDefaultAsync(e => e.Id == id, ct);
        if (expense is null) return Result<bool>.Fail("NOT_FOUND", "Expense claim not found.");
        if (expense.Status != ExpenseStatus.Approved)
        {
            return Result<bool>.Fail("INVALID_STATE", "Only approved expense claims can be marked as reimbursed.");
        }

        expense.Status = ExpenseStatus.Reimbursed;
        expense.ReimbursedAt = DateTime.UtcNow;
        expense.UpdatedBy = actorId;
        await _db.SaveChangesAsync(ct);
        await _auditLogger.LogAsync(actorId, "expense.reimbursed", "Expense", expense.Id.ToString(), null, ct);

        await _notificationService.NotifyAsync(
            expense.EmployeeId,
            "Expense claim reimbursed",
            $"Your {expense.Category} expense of {expense.Amount:0.00} has been reimbursed.",
            "expense",
            $"/expenses/{expense.Id}",
            ct);

        return Result<bool>.Ok(true);
    }

    private static ExpenseDto ToDto(Expense e, string employeeName, string? decidedByName) => new(
        e.Id, e.EmployeeId, employeeName, e.Category, e.Amount, e.ExpenseDate,
        e.Description, e.ReceiptUrl, e.Status, decidedByName, e.DecidedAt, e.DecisionComment, e.ReimbursedAt, e.CreatedAt);
}
