using CarminePeople.Domain.Enums;

namespace CarminePeople.Application.Recruitment.DTOs;

public record JobPostingDto(Guid Id, string Title, string Department, string? Description, JobPostingStatus Status, int CandidateCount, DateTime CreatedAt);

public record CreateJobPostingRequest(string Title, string Department, string? Description);

public record CandidateDto(
    Guid Id, Guid JobPostingId, string FullName, string Email, string? Phone, string? ResumeUrl,
    string? Notes, CandidateStage Stage, DateTime CreatedAt);

public record CreateCandidateRequest(string FullName, string Email, string? Phone, string? ResumeUrl, string? Notes);

public record UpdateCandidateStageRequest(CandidateStage Stage, string? Notes);
