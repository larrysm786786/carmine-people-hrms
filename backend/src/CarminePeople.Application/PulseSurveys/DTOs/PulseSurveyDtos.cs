namespace CarminePeople.Application.PulseSurveys.DTOs;

public record PulseSurveyDto(Guid Id, string Question, bool IsActive, int ResponseCount, double? AverageRating, DateTime CreatedAt);

public record CreatePulseSurveyRequest(string Question);

public record PulseSurveyResponseDto(Guid Id, string? EmployeeName, int Rating, string? Comment, DateTime CreatedAt);

public record SubmitPulseSurveyResponseRequest(int Rating, string? Comment, bool IsAnonymous);
