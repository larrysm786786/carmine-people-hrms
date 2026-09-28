namespace CarminePeople.Domain.Entities;

public class DeviceToken
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    public string Token { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty; // android | ios
    public DateTime RegisteredAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
}
