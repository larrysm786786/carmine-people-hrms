using CarminePeople.Application.Announcements.DTOs;
using CarminePeople.Application.Common.Interfaces;
using CarminePeople.Application.Notifications.Services;
using CarminePeople.Domain.Entities;
using CarminePeople.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace CarminePeople.Application.Announcements.Services;

public class AnnouncementService
{
    private readonly IApplicationDbContext _db;
    private readonly NotificationService _notificationService;

    public AnnouncementService(IApplicationDbContext db, NotificationService notificationService)
    {
        _db = db;
        _notificationService = notificationService;
    }

    public async Task<AnnouncementDto> CreateAsync(Guid createdByEmployeeId, CreateAnnouncementRequest request, CancellationToken ct)
    {
        var announcement = new Announcement
        {
            Title = request.Title,
            Body = request.Body,
            AudienceType = request.AudienceType,
            AudienceFilterJson = request.AudienceIds is { Count: > 0 } ? JsonSerializer.Serialize(request.AudienceIds) : null,
            PublishAt = request.PublishAt,
            ExpiresAt = request.ExpiresAt,
            ImageDocumentId = request.ImageDocumentId,
            AttachmentDocumentId = request.AttachmentDocumentId,
            CreatedByEmployeeId = createdByEmployeeId
        };
        _db.Announcements.Add(announcement);
        await _db.SaveChangesAsync(ct);

        var recipientIds = await ResolveAudienceAsync(request.AudienceType, request.AudienceIds, ct);
        foreach (var employeeId in recipientIds)
        {
            await _notificationService.NotifyAsync(employeeId, request.Title, request.Body, "announcement", $"/announcements/{announcement.Id}", ct);
        }

        return ToDto(announcement);
    }

    public async Task<List<AnnouncementDto>> GetFeedAsync(Guid employeeId, CancellationToken ct)
    {
        var employee = await _db.Employees.FirstOrDefaultAsync(e => e.Id == employeeId, ct);
        if (employee is null) return [];

        var now = DateTime.UtcNow;
        var candidates = await _db.Announcements
            .Where(a => a.PublishAt <= now && (a.ExpiresAt == null || a.ExpiresAt >= now))
            .OrderByDescending(a => a.PublishAt)
            .ToListAsync(ct);

        var result = new List<AnnouncementDto>();
        foreach (var announcement in candidates)
        {
            if (MatchesAudience(announcement, employee))
            {
                result.Add(ToDto(announcement));
            }
        }
        return result;
    }

    private async Task<List<Guid>> ResolveAudienceAsync(AnnouncementAudienceType audienceType, List<Guid>? audienceIds, CancellationToken ct)
    {
        return audienceType switch
        {
            AnnouncementAudienceType.All => await _db.Employees.Select(e => e.Id).ToListAsync(ct),
            AnnouncementAudienceType.Department => await _db.Employees.Where(e => audienceIds!.Contains(e.DepartmentId)).Select(e => e.Id).ToListAsync(ct),
            AnnouncementAudienceType.Location => await _db.Employees.Where(e => audienceIds!.Contains(e.LocationId)).Select(e => e.Id).ToListAsync(ct),
            AnnouncementAudienceType.Specific => audienceIds ?? [],
            _ => []
        };
    }

    private static bool MatchesAudience(Announcement announcement, Employee employee)
    {
        if (announcement.AudienceType == AnnouncementAudienceType.All) return true;

        var ids = announcement.AudienceFilterJson is null
            ? []
            : JsonSerializer.Deserialize<List<Guid>>(announcement.AudienceFilterJson) ?? [];

        return announcement.AudienceType switch
        {
            AnnouncementAudienceType.Department => ids.Contains(employee.DepartmentId),
            AnnouncementAudienceType.Location => ids.Contains(employee.LocationId),
            AnnouncementAudienceType.Specific => ids.Contains(employee.Id),
            _ => false
        };
    }

    private static AnnouncementDto ToDto(Announcement a) => new(
        a.Id, a.Title, a.Body, a.AudienceType, a.PublishAt, a.ExpiresAt, a.CreatedByEmployeeId,
        a.ImageDocumentId, a.AttachmentDocumentId);

    public async Task<CommentDto> AddCommentAsync(Guid announcementId, Guid employeeId, string body, CancellationToken ct)
    {
        var comment = new AnnouncementComment
        {
            AnnouncementId = announcementId,
            EmployeeId = employeeId,
            Body = body,
            CreatedAt = DateTime.UtcNow
        };
        _db.AnnouncementComments.Add(comment);
        await _db.SaveChangesAsync(ct);

        var employee = await _db.Employees.FirstAsync(e => e.Id == employeeId, ct);
        return new CommentDto(comment.Id, comment.AnnouncementId, comment.EmployeeId, $"{employee.FirstName} {employee.LastName}", comment.Body, comment.CreatedAt);
    }

    public async Task<List<CommentDto>> GetCommentsAsync(Guid announcementId, CancellationToken ct)
    {
        return await _db.AnnouncementComments.AsNoTracking()
            .Where(c => c.AnnouncementId == announcementId)
            .OrderBy(c => c.CreatedAt)
            .Select(c => new CommentDto(c.Id, c.AnnouncementId, c.EmployeeId, c.Employee.FirstName + " " + c.Employee.LastName, c.Body, c.CreatedAt))
            .ToListAsync(ct);
    }
}
