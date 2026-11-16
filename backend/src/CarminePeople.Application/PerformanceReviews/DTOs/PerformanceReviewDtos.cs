using CarminePeople.Domain.Enums;

namespace CarminePeople.Application.PerformanceReviews.DTOs;

public record PerformanceReviewDto(
    Guid Id, Guid EmployeeId, string EmployeeName, string Period,
    int? SelfRating, string? SelfComments, DateTime? SelfSubmittedAt,
    int? ManagerRating, string? ManagerComments, DateTime? ManagerSubmittedAt, string? ReviewedByName,
    PerformanceReviewStatus Status, DateTime CreatedAt);

public record CreatePerformanceReviewRequest(Guid EmployeeId, string Period);

public record SubmitSelfReviewRequest(int Rating, string? Comments);

public record SubmitManagerReviewRequest(int Rating, string? Comments);

public record PerformanceReviewQuery(Guid? EmployeeId, int Page = 1, int PageSize = 20);
