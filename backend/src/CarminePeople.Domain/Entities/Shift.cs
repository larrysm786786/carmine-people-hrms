using CarminePeople.Domain.Common;
using CarminePeople.Domain.Enums;

namespace CarminePeople.Domain.Entities;

public class Shift : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public ShiftType Type { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public int GracePeriodMinutes { get; set; }
    public int BreakMinutes { get; set; }

    // Minimum minutes an employee must actually work in a day on this shift (e.g. 9 hours =
    // 540) for the day to count as a full Present day — falling short marks the day HalfDay
    // even if check-in was on time. 0 means no minimum is enforced (legacy/unconfigured shifts).
    public int RequiredWorkMinutes { get; set; }
}
