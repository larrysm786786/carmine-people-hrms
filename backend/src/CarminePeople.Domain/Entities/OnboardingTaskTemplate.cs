using CarminePeople.Domain.Common;

namespace CarminePeople.Domain.Entities;

// Company-wide reusable checklist items — HR applies these to a new hire's
// OnboardingTask list instead of retyping the same items for every employee.
public class OnboardingTaskTemplate : AuditableEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int SortOrder { get; set; }
}
