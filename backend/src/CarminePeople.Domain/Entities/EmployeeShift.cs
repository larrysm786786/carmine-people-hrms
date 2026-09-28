namespace CarminePeople.Domain.Entities;

public class EmployeeShift
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    public Guid ShiftId { get; set; }
    public Shift Shift { get; set; } = null!;

    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
}
