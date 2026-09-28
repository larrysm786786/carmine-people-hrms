using CarminePeople.Domain.Enums;

namespace CarminePeople.Domain.Entities;

public class NotificationPreference
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    public NotificationChannel Channel { get; set; }
    public string Category { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
}
