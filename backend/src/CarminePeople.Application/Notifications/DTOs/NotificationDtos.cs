using CarminePeople.Domain.Enums;

namespace CarminePeople.Application.Notifications.DTOs;

public record NotificationDto(Guid Id, string Title, string Body, string Type, bool IsRead, DateTime SentAt, string? DeepLinkPath);

public record RegisterDeviceRequest(string Token, string Platform);

public record NotificationPreferenceDto(NotificationChannel Channel, string Category, bool IsEnabled);

public record SetPreferenceRequest(NotificationChannel Channel, string Category, bool IsEnabled);
