namespace CarminePeople.Domain.Entities;

public class AnnouncementComment
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid AnnouncementId { get; set; }
    public Announcement Announcement { get; set; } = null!;

    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    public string Body { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
