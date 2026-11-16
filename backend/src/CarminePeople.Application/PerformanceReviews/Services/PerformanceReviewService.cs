using CarminePeople.Application.Common.Interfaces;
using CarminePeople.Application.Common.Models;
using CarminePeople.Application.PerformanceReviews.DTOs;
using CarminePeople.Domain.Entities;
using CarminePeople.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CarminePeople.Application.PerformanceReviews.Services;

public class PerformanceReviewService
{
    private readonly IApplicationDbContext _db;
    private readonly IAuditLogger _auditLogger;

    public PerformanceReviewService(IApplicationDbContext db, IAuditLogger auditLogger)
    {
        _db = db;
        _auditLogger = auditLogger;
    }

    public async Task<Result<PerformanceReviewDto>> CreateAsync(CreatePerformanceReviewRequest request, Guid actorId, CancellationToken ct)
    {
        var employee = await _db.Employees.FirstOrDefaultAsync(e => e.Id == request.EmployeeId, ct);
        if (employee is null) return Result<PerformanceReviewDto>.Fail("NOT_FOUND", "Employee not found.");

        var review = new PerformanceReview { EmployeeId = request.EmployeeId, Period = request.Period, CreatedBy = actorId };
        _db.PerformanceReviews.Add(review);
        await _db.SaveChangesAsync(ct);
        await _auditLogger.LogAsync(actorId, "performance_review.created", "PerformanceReview", review.Id.ToString(), null, ct);
        return Result<PerformanceReviewDto>.Ok(ToDto(review, $"{employee.FirstName} {employee.LastName}".Trim(), null));
    }

    public async Task<PagedResult<PerformanceReviewDto>> GetListAsync(PerformanceReviewQuery query, CancellationToken ct)
    {
        var q = _db.PerformanceReviews.AsNoTracking().AsQueryable();
        if (query.EmployeeId.HasValue) q = q.Where(r => r.EmployeeId == query.EmployeeId);

        var total = await q.CountAsync(ct);
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var items = await q.OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(r => new PerformanceReviewDto(
                r.Id, r.EmployeeId, r.Employee.FirstName + " " + r.Employee.LastName, r.Period,
                r.SelfRating, r.SelfComments, r.SelfSubmittedAt,
                r.ManagerRating, r.ManagerComments, r.ManagerSubmittedAt,
                r.ReviewedBy == null ? null : r.ReviewedBy.FirstName + " " + r.ReviewedBy.LastName,
                r.Status, r.CreatedAt))
            .ToListAsync(ct);

        return new PagedResult<PerformanceReviewDto> { Items = items, Page = page, PageSize = pageSize, Total = total };
    }

    public async Task<PagedResult<PerformanceReviewDto>> GetTeamPendingAsync(Guid managerId, bool unscopedVisibility, int page, int pageSize, CancellationToken ct)
    {
        var q = _db.PerformanceReviews.AsNoTracking()
            .Where(r => (unscopedVisibility || r.Employee.ManagerId == managerId) && r.Status == PerformanceReviewStatus.PendingManagerReview);

        var total = await q.CountAsync(ct);
        var safePage = Math.Max(1, page);
        var safePageSize = Math.Clamp(pageSize, 1, 100);

        var items = await q.OrderBy(r => r.SelfSubmittedAt)
            .Skip((safePage - 1) * safePageSize).Take(safePageSize)
            .Select(r => new PerformanceReviewDto(
                r.Id, r.EmployeeId, r.Employee.FirstName + " " + r.Employee.LastName, r.Period,
                r.SelfRating, r.SelfComments, r.SelfSubmittedAt,
                r.ManagerRating, r.ManagerComments, r.ManagerSubmittedAt, null,
                r.Status, r.CreatedAt))
            .ToListAsync(ct);

        return new PagedResult<PerformanceReviewDto> { Items = items, Page = safePage, PageSize = safePageSize, Total = total };
    }

    public async Task<Result<bool>> SubmitSelfReviewAsync(Guid id, Guid employeeId, SubmitSelfReviewRequest request, CancellationToken ct)
    {
        var review = await _db.PerformanceReviews.FirstOrDefaultAsync(r => r.Id == id, ct);
        if (review is null) return Result<bool>.Fail("NOT_FOUND", "Review not found.");
        if (review.EmployeeId != employeeId) return Result<bool>.Fail("FORBIDDEN", "This is not your review.");
        if (review.Status != PerformanceReviewStatus.PendingSelfReview)
        {
            return Result<bool>.Fail("INVALID_STATE", "This review is not awaiting your self-assessment.");
        }

        review.SelfRating = request.Rating;
        review.SelfComments = request.Comments;
        review.SelfSubmittedAt = DateTime.UtcNow;
        review.Status = PerformanceReviewStatus.PendingManagerReview;
        review.UpdatedBy = employeeId;
        await _db.SaveChangesAsync(ct);
        await _auditLogger.LogAsync(employeeId, "performance_review.self_submitted", "PerformanceReview", review.Id.ToString(), null, ct);
        return Result<bool>.Ok(true);
    }

    public async Task<Result<bool>> SubmitManagerReviewAsync(Guid id, Guid actorId, SubmitManagerReviewRequest request, CancellationToken ct)
    {
        var review = await _db.PerformanceReviews.FirstOrDefaultAsync(r => r.Id == id, ct);
        if (review is null) return Result<bool>.Fail("NOT_FOUND", "Review not found.");
        if (review.Status != PerformanceReviewStatus.PendingManagerReview)
        {
            return Result<bool>.Fail("INVALID_STATE", "This review is not awaiting a manager assessment.");
        }

        review.ManagerRating = request.Rating;
        review.ManagerComments = request.Comments;
        review.ManagerSubmittedAt = DateTime.UtcNow;
        review.ReviewedById = actorId;
        review.Status = PerformanceReviewStatus.Completed;
        review.UpdatedBy = actorId;
        await _db.SaveChangesAsync(ct);
        await _auditLogger.LogAsync(actorId, "performance_review.manager_submitted", "PerformanceReview", review.Id.ToString(), null, ct);
        return Result<bool>.Ok(true);
    }

    private static PerformanceReviewDto ToDto(PerformanceReview r, string employeeName, string? reviewedByName) => new(
        r.Id, r.EmployeeId, employeeName, r.Period,
        r.SelfRating, r.SelfComments, r.SelfSubmittedAt,
        r.ManagerRating, r.ManagerComments, r.ManagerSubmittedAt, reviewedByName,
        r.Status, r.CreatedAt);
}
