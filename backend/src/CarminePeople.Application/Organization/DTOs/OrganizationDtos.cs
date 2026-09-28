using CarminePeople.Domain.Enums;

namespace CarminePeople.Application.Organization.DTOs;

public record DepartmentDto(Guid Id, string Name, string? Code, string? Description, Guid? ManagerId, string? ManagerName, bool IsActive);
public record CreateDepartmentRequest(string Name, string? Code, string? Description, Guid? ManagerId, bool IsActive = true);
public record UpdateDepartmentRequest(string Name, string? Code, string? Description, Guid? ManagerId, bool IsActive);

public record DesignationDto(Guid Id, string Title, string? Description, Guid DepartmentId, string DepartmentName, bool IsActive);
public record CreateDesignationRequest(string Title, string? Description, Guid DepartmentId, bool IsActive = true);
public record UpdateDesignationRequest(string Title, string? Description, Guid DepartmentId, bool IsActive);

public record LocationDto(Guid Id, string Name, string? Address, double Latitude, double Longitude, int GeofenceRadiusMeters, bool IsActive, string? OfficeWifiSsid);
public record CreateLocationRequest(string Name, string? Address, double Latitude, double Longitude, int GeofenceRadiusMeters, bool IsActive = true, string? OfficeWifiSsid = null);
public record UpdateLocationRequest(string Name, string? Address, double Latitude, double Longitude, int GeofenceRadiusMeters, bool IsActive, string? OfficeWifiSsid);

// WorkingMinutes is derived (End - Start - Break), not stored separately, so it can
// never drift out of sync with the fields it's computed from.
public record ShiftDto(Guid Id, string Name, ShiftType Type, TimeSpan StartTime, TimeSpan EndTime, int GracePeriodMinutes, int BreakMinutes, int WorkingMinutes, int RequiredWorkMinutes);
public record CreateShiftRequest(string Name, ShiftType Type, TimeSpan StartTime, TimeSpan EndTime, int GracePeriodMinutes, int BreakMinutes, int RequiredWorkMinutes);
public record UpdateShiftRequest(string Name, ShiftType Type, TimeSpan StartTime, TimeSpan EndTime, int GracePeriodMinutes, int BreakMinutes, int RequiredWorkMinutes);

public record AssignShiftRequest(Guid EmployeeId, Guid ShiftId, DateTime EffectiveFrom, DateTime? EffectiveTo);
public record EmployeeShiftDto(Guid Id, Guid EmployeeId, Guid ShiftId, string ShiftName, DateTime EffectiveFrom, DateTime? EffectiveTo);
