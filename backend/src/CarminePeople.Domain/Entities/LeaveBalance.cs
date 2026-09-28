namespace CarminePeople.Domain.Entities;

public class LeaveBalance
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    public Guid LeaveTypeId { get; set; }
    public LeaveType LeaveType { get; set; } = null!;

    public int Year { get; set; }
    public decimal Available { get; set; }
    public decimal Used { get; set; }
    public decimal Pending { get; set; }

    // Concurrency token so two simultaneous leave applications for the same employee/type/year
    // can't both pass the "Available - Pending >= daysCount" check before either commits and
    // overdraw the balance — the second writer's SaveChangesAsync throws
    // DbUpdateConcurrencyException instead of silently overwriting the first writer's update.
    public byte[] RowVersion { get; set; } = null!;
}
