using CarminePeople.Domain.Common;

namespace CarminePeople.Domain.Entities;

public class PulseSurvey : AuditableEntity
{
    public string Question { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public ICollection<PulseSurveyResponse> Responses { get; set; } = new List<PulseSurveyResponse>();
}

public class PulseSurveyResponse : AuditableEntity
{
    public Guid SurveyId { get; set; }
    public PulseSurvey Survey { get; set; } = null!;

    // Always the real responder, even for an anonymous response — needed to enforce
    // "one response per employee per survey". IsAnonymous alone controls what admins see.
    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    public int Rating { get; set; }
    public string? Comment { get; set; }
    public bool IsAnonymous { get; set; }
}
