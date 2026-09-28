using CarminePeople.Domain.Common;
using CarminePeople.Domain.Enums;

namespace CarminePeople.Domain.Entities;

public class AttendanceRecord : AuditableEntity
{
    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    public DateOnly Date { get; set; }

    public DateTime? CheckInAt { get; set; }
    public double? CheckInLat { get; set; }
    public double? CheckInLng { get; set; }
    public double? CheckInAccuracyMeters { get; set; }
    public string? CheckInDeviceInfo { get; set; }
    public CheckInMethod CheckInMethod { get; set; } = CheckInMethod.Gps;
    public bool FaceVerified { get; set; } = false;

    public DateTime? CheckOutAt { get; set; }
    public double? CheckOutLat { get; set; }
    public double? CheckOutLng { get; set; }
    public double? CheckOutAccuracyMeters { get; set; }

    // True while the employee is currently on-site for the day. CheckInAt always stays the
    // day's FIRST check-in and CheckOutAt always holds the MOST RECENT check-out — re-entering
    // the office (e.g. after lunch, a vendor visit) flips this back to true via CheckInAsync
    // without touching CheckInAt, and each subsequent check-out just advances CheckOutAt.
    public bool IsCheckedIn { get; set; }

    public AttendanceStatus Status { get; set; } = AttendanceStatus.Present;

    // Server-computed only. Never accepted directly from a client request.
    public int WorkingMinutes { get; set; }

    public Guid LocationId { get; set; }
    public Location Location { get; set; } = null!;

    public ICollection<Break> Breaks { get; set; } = new List<Break>();
}
