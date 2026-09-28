using CarminePeople.Application.Common.Interfaces;
using CarminePeople.Application.Common.Models;
using CarminePeople.Application.Notifications.DTOs;
using CarminePeople.Domain.Entities;
using CarminePeople.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CarminePeople.Application.Notifications.Services;

public class NotificationService
{
    private readonly IApplicationDbContext _db;
    private readonly IPushNotificationSender _pushSender;

    public NotificationService(IApplicationDbContext db, IPushNotificationSender pushSender)
    {
        _db = db;
        _pushSender = pushSender;
    }

    public async Task<PagedResult<NotificationDto>> GetListAsync(Guid employeeId, int page, int pageSize, CancellationToken ct)
    {
        var q = _db.Notifications.Where(n => n.EmployeeId == employeeId);
        var total = await q.CountAsync(ct);
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var items = await q.OrderByDescending(n => n.SentAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(n => new NotificationDto(n.Id, n.Title, n.Body, n.Type, n.IsRead, n.SentAt, n.DeepLinkPath))
            .ToListAsync(ct);

        return new PagedResult<NotificationDto> { Items = items, Page = page, PageSize = pageSize, Total = total };
    }

    public async Task<Result<bool>> MarkReadAsync(Guid employeeId, Guid notificationId, CancellationToken ct)
    {
        var notification = await _db.Notifications.FirstOrDefaultAsync(n => n.Id == notificationId && n.EmployeeId == employeeId, ct);
        if (notification is null) return Result<bool>.Fail("NOT_FOUND", "Notification not found.");
        notification.IsRead = true;
        await _db.SaveChangesAsync(ct);
        return Result<bool>.Ok(true);
    }

    public async Task<List<NotificationPreferenceDto>> GetPreferencesAsync(Guid employeeId, CancellationToken ct) =>
        await _db.NotificationPreferences
            .Where(p => p.EmployeeId == employeeId)
            .Select(p => new NotificationPreferenceDto(p.Channel, p.Category, p.IsEnabled))
            .ToListAsync(ct);

    public async Task SetPreferenceAsync(Guid employeeId, SetPreferenceRequest request, CancellationToken ct)
    {
        var pref = await _db.NotificationPreferences.FirstOrDefaultAsync(
            p => p.EmployeeId == employeeId && p.Channel == request.Channel && p.Category == request.Category, ct);

        if (pref is null)
        {
            _db.NotificationPreferences.Add(new NotificationPreference
            {
                EmployeeId = employeeId,
                Channel = request.Channel,
                Category = request.Category,
                IsEnabled = request.IsEnabled
            });
        }
        else
        {
            pref.IsEnabled = request.IsEnabled;
        }
        await _db.SaveChangesAsync(ct);
    }

    public async Task RegisterDeviceAsync(Guid employeeId, RegisterDeviceRequest request, CancellationToken ct)
    {
        var existing = await _db.DeviceTokens.FirstOrDefaultAsync(d => d.Token == request.Token, ct);
        if (existing is not null)
        {
            existing.EmployeeId = employeeId;
            existing.LastUsedAt = DateTime.UtcNow;
        }
        else
        {
            _db.DeviceTokens.Add(new DeviceToken
            {
                EmployeeId = employeeId,
                Token = request.Token,
                Platform = request.Platform,
                RegisteredAt = DateTime.UtcNow
            });
        }
        await _db.SaveChangesAsync(ct);
    }

    // Creates an in-app notification record and best-effort dispatches a push
    // notification, respecting the employee's channel preference for the category
    // independently for each channel (an employee can mute push for a category while
    // still keeping it visible in-app, or vice versa).
    public async Task NotifyAsync(Guid employeeId, string title, string body, string type, string? deepLinkPath, CancellationToken ct)
    {
        var disabledChannels = await _db.NotificationPreferences
            .Where(p => p.EmployeeId == employeeId && p.Category == type && !p.IsEnabled)
            .Select(p => p.Channel)
            .ToListAsync(ct);

        var inAppEnabled = !disabledChannels.Contains(NotificationChannel.InApp);
        if (inAppEnabled)
        {
            _db.Notifications.Add(new Notification
            {
                EmployeeId = employeeId,
                Title = title,
                Body = body,
                Type = type,
                SentAt = DateTime.UtcNow,
                DeepLinkPath = deepLinkPath
            });
            await _db.SaveChangesAsync(ct);
        }

        var pushEnabled = !disabledChannels.Contains(NotificationChannel.Push);
        if (pushEnabled)
        {
            var tokens = await _db.DeviceTokens.Where(d => d.EmployeeId == employeeId).Select(d => d.Token).ToListAsync(ct);
            await _pushSender.SendAsync(tokens, title, body, type, deepLinkPath, ct);
        }
    }
}
