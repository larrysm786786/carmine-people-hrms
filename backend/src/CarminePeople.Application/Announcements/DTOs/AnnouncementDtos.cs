using CarminePeople.Domain.Enums;

namespace CarminePeople.Application.Announcements.DTOs;

public record CreateAnnouncementRequest(
    string Title, string Body, AnnouncementAudienceType AudienceType,
    List<Guid>? AudienceIds, DateTime PublishAt, DateTime? ExpiresAt,
    Guid? ImageDocumentId, Guid? AttachmentDocumentId);

public record AnnouncementDto(
    Guid Id, string Title, string Body, AnnouncementAudienceType AudienceType,
    DateTime PublishAt, DateTime? ExpiresAt, Guid CreatedByEmployeeId,
    Guid? ImageDocumentId, Guid? AttachmentDocumentId);

public record AddCommentRequest(string Body);

public record CommentDto(Guid Id, Guid AnnouncementId, Guid EmployeeId, string EmployeeName, string Body, DateTime CreatedAt);
