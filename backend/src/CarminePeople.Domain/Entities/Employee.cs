using CarminePeople.Domain.Common;
using CarminePeople.Domain.Enums;

namespace CarminePeople.Domain.Entities;

public class Employee : AuditableEntity
{
    public string EmployeeCode { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }

    public Guid DepartmentId { get; set; }
    public Department Department { get; set; } = null!;

    public Guid DesignationId { get; set; }
    public Designation Designation { get; set; } = null!;

    public Guid LocationId { get; set; }
    public Location Location { get; set; } = null!;

    public Guid? ManagerId { get; set; }
    public Employee? Manager { get; set; }

    public Guid RoleId { get; set; }
    public Role Role { get; set; } = null!;

    public DateTime DateOfJoining { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public EmploymentStatus EmploymentStatus { get; set; } = EmploymentStatus.Active;
    public string? ProfileImageUrl { get; set; }

    // Full monthly salary before attendance-based proration. Only settable via the
    // payroll-permission-gated endpoint, never through the general employee-update endpoint —
    // see EmployeesController vs a dedicated salary endpoint.
    public decimal? MonthlySalary { get; set; }

    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }
    public string? EmergencyContactRelation { get; set; }

    // JSON-serialized float array (128 or 192-dim), computed on-device by the mobile app.
    public string? FaceEmbedding { get; set; }
    public DateTime? FaceEnrolledAt { get; set; }
}
