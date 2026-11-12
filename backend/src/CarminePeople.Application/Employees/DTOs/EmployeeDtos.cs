using CarminePeople.Domain.Enums;

namespace CarminePeople.Application.Employees.DTOs;

public record EmployeeListItemDto(
    Guid Id, string EmployeeCode, string FirstName, string LastName, string Email,
    string DepartmentName, string DesignationTitle, string LocationName, string RoleName,
    EmploymentStatus EmploymentStatus);

public record EmployeeDetailDto(
    Guid Id, string EmployeeCode, string FirstName, string LastName, string Email, string? PhoneNumber,
    Guid DepartmentId, string DepartmentName, Guid DesignationId, string DesignationTitle,
    Guid LocationId, string LocationName, Guid? ManagerId, string? ManagerName, Guid RoleId, string RoleName,
    DateTime DateOfJoining, DateTime? DateOfBirth, EmploymentStatus EmploymentStatus, string? ProfileImageUrl,
    string? EmergencyContactName, string? EmergencyContactPhone, string? EmergencyContactRelation);

public record CreateEmployeeRequest(
    string EmployeeCode, string FirstName, string LastName, string Email, string Password, string? PhoneNumber,
    Guid DepartmentId, Guid DesignationId, Guid LocationId, Guid? ManagerId, Guid RoleId,
    DateTime DateOfJoining, DateTime? DateOfBirth);

public record UpdateEmployeeRequest(
    string EmployeeCode, string FirstName, string LastName, string Email, string? PhoneNumber,
    Guid DepartmentId, Guid DesignationId, Guid LocationId, Guid? ManagerId, Guid RoleId,
    DateTime DateOfJoining, DateTime? DateOfBirth, EmploymentStatus EmploymentStatus);

public record SetEmployeePasswordRequest(string NewPassword);

// Limited self-service update: an employee may update their own contact details,
// date of birth, and emergency contact, but not org fields (department/role/etc.)
// which require employee.manage.
public record UpdateSelfProfileRequest(
    string? PhoneNumber, DateTime? DateOfBirth,
    string? EmergencyContactName, string? EmergencyContactPhone, string? EmergencyContactRelation);

public record UpdateProfileImageRequest(string ProfileImageUrl);

public record EmployeeQuery(
    string? Search, Guid? DepartmentId, Guid? LocationId, EmploymentStatus? Status, Guid? ManagerId = null,
    int Page = 1, int PageSize = 20, string? SortBy = null, string? SortDirection = null);

// Recursive tree node: Reports is populated to full depth (an org chart with tens of
// employees is small enough to build eagerly rather than paginate/lazy-load per level).
public record OrgChartNodeDto(
    Guid Id, string FirstName, string LastName, string DesignationTitle, string? ProfileImageUrl,
    List<OrgChartNodeDto> Reports);

// Both DateOfBirth and DateOfJoining anniversaries share one shape — Kind distinguishes them
// so the client can label "Birthday" vs "Work Anniversary" without a second endpoint.
public record UpcomingCelebrationDto(Guid EmployeeId, string EmployeeName, string? ProfileImageUrl, DateTime Date, string Kind, int YearsCount);
