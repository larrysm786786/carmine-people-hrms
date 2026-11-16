using CarminePeople.Application.Common.Interfaces;
using CarminePeople.Application.Common.Models;
using CarminePeople.Application.Recruitment.DTOs;
using CarminePeople.Domain.Entities;
using CarminePeople.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CarminePeople.Application.Recruitment.Services;

public class RecruitmentService
{
    private readonly IApplicationDbContext _db;
    private readonly IAuditLogger _auditLogger;

    public RecruitmentService(IApplicationDbContext db, IAuditLogger auditLogger)
    {
        _db = db;
        _auditLogger = auditLogger;
    }

    public async Task<JobPostingDto> CreateJobPostingAsync(CreateJobPostingRequest request, Guid actorId, CancellationToken ct)
    {
        var job = new JobPosting { Title = request.Title, Department = request.Department, Description = request.Description, CreatedBy = actorId };
        _db.JobPostings.Add(job);
        await _db.SaveChangesAsync(ct);
        await _auditLogger.LogAsync(actorId, "job_posting.created", "JobPosting", job.Id.ToString(), null, ct);
        return new JobPostingDto(job.Id, job.Title, job.Department, job.Description, job.Status, 0, job.CreatedAt);
    }

    public async Task<List<JobPostingDto>> GetJobPostingsAsync(CancellationToken ct) =>
        await _db.JobPostings.AsNoTracking()
            .OrderByDescending(j => j.CreatedAt)
            .Select(j => new JobPostingDto(j.Id, j.Title, j.Department, j.Description, j.Status, j.Candidates.Count, j.CreatedAt))
            .ToListAsync(ct);

    public async Task<Result<bool>> CloseJobPostingAsync(Guid id, Guid actorId, CancellationToken ct)
    {
        var job = await _db.JobPostings.FirstOrDefaultAsync(j => j.Id == id, ct);
        if (job is null) return Result<bool>.Fail("NOT_FOUND", "Job posting not found.");

        job.Status = JobPostingStatus.Closed;
        job.UpdatedBy = actorId;
        await _db.SaveChangesAsync(ct);
        await _auditLogger.LogAsync(actorId, "job_posting.closed", "JobPosting", job.Id.ToString(), null, ct);
        return Result<bool>.Ok(true);
    }

    public async Task<Result<List<CandidateDto>>> GetCandidatesAsync(Guid jobPostingId, CancellationToken ct)
    {
        if (!await _db.JobPostings.AnyAsync(j => j.Id == jobPostingId, ct))
        {
            return Result<List<CandidateDto>>.Fail("NOT_FOUND", "Job posting not found.");
        }

        var candidates = await _db.Candidates.AsNoTracking()
            .Where(c => c.JobPostingId == jobPostingId)
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new CandidateDto(c.Id, c.JobPostingId, c.FullName, c.Email, c.Phone, c.ResumeUrl, c.Notes, c.Stage, c.CreatedAt))
            .ToListAsync(ct);

        return Result<List<CandidateDto>>.Ok(candidates);
    }

    public async Task<Result<CandidateDto>> AddCandidateAsync(Guid jobPostingId, CreateCandidateRequest request, Guid actorId, CancellationToken ct)
    {
        if (!await _db.JobPostings.AnyAsync(j => j.Id == jobPostingId, ct))
        {
            return Result<CandidateDto>.Fail("NOT_FOUND", "Job posting not found.");
        }

        var candidate = new Candidate
        {
            JobPostingId = jobPostingId,
            FullName = request.FullName,
            Email = request.Email,
            Phone = request.Phone,
            ResumeUrl = request.ResumeUrl,
            Notes = request.Notes,
            CreatedBy = actorId,
        };
        _db.Candidates.Add(candidate);
        await _db.SaveChangesAsync(ct);
        await _auditLogger.LogAsync(actorId, "candidate.added", "Candidate", candidate.Id.ToString(), null, ct);
        return Result<CandidateDto>.Ok(new CandidateDto(candidate.Id, candidate.JobPostingId, candidate.FullName, candidate.Email, candidate.Phone, candidate.ResumeUrl, candidate.Notes, candidate.Stage, candidate.CreatedAt));
    }

    public async Task<Result<CandidateDto>> UpdateStageAsync(Guid candidateId, UpdateCandidateStageRequest request, Guid actorId, CancellationToken ct)
    {
        var candidate = await _db.Candidates.FirstOrDefaultAsync(c => c.Id == candidateId, ct);
        if (candidate is null) return Result<CandidateDto>.Fail("NOT_FOUND", "Candidate not found.");

        candidate.Stage = request.Stage;
        if (request.Notes != null) candidate.Notes = request.Notes;
        candidate.UpdatedBy = actorId;
        await _db.SaveChangesAsync(ct);
        await _auditLogger.LogAsync(actorId, "candidate.stage_updated", "Candidate", candidate.Id.ToString(), new { request.Stage }, ct);
        return Result<CandidateDto>.Ok(new CandidateDto(candidate.Id, candidate.JobPostingId, candidate.FullName, candidate.Email, candidate.Phone, candidate.ResumeUrl, candidate.Notes, candidate.Stage, candidate.CreatedAt));
    }

    public async Task<Result<bool>> DeleteCandidateAsync(Guid candidateId, Guid actorId, CancellationToken ct)
    {
        var candidate = await _db.Candidates.FirstOrDefaultAsync(c => c.Id == candidateId, ct);
        if (candidate is null) return Result<bool>.Fail("NOT_FOUND", "Candidate not found.");

        candidate.IsDeleted = true;
        candidate.UpdatedBy = actorId;
        await _db.SaveChangesAsync(ct);
        await _auditLogger.LogAsync(actorId, "candidate.deleted", "Candidate", candidate.Id.ToString(), null, ct);
        return Result<bool>.Ok(true);
    }
}
