using CarminePeople.Domain.Common;
using CarminePeople.Domain.Enums;

namespace CarminePeople.Domain.Entities;

public class JobPosting : AuditableEntity
{
    public string Title { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string? Description { get; set; }
    public JobPostingStatus Status { get; set; } = JobPostingStatus.Open;

    public ICollection<Candidate> Candidates { get; set; } = new List<Candidate>();
}

public class Candidate : AuditableEntity
{
    public Guid JobPostingId { get; set; }
    public JobPosting JobPosting { get; set; } = null!;

    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? ResumeUrl { get; set; }
    public string? Notes { get; set; }
    public CandidateStage Stage { get; set; } = CandidateStage.Applied;
}
