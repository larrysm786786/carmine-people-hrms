namespace CarminePeople.Domain.Entities;

public class Break
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid AttendanceRecordId { get; set; }
    public AttendanceRecord AttendanceRecord { get; set; } = null!;

    public DateTime BreakStartAt { get; set; }
    public DateTime? BreakEndAt { get; set; }

    // Server-computed only, once BreakEndAt is set.
    public int? DurationMinutes { get; set; }
}
