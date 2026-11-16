namespace CarminePeople.Application.Onboarding.DTOs;

public record OnboardingTaskTemplateDto(Guid Id, string Title, string? Description, int SortOrder);

public record CreateOnboardingTaskTemplateRequest(string Title, string? Description, int SortOrder);

public record UpdateOnboardingTaskTemplateRequest(string Title, string? Description, int SortOrder);

public record OnboardingTaskDto(
    Guid Id, Guid EmployeeId, string EmployeeName, string Title, string? Description, int SortOrder,
    bool IsCompleted, DateTime? CompletedAt);

public record CreateOnboardingTaskRequest(string Title, string? Description, int SortOrder);
