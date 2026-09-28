using CarminePeople.Application.Common.Interfaces;
using CarminePeople.Application.Common.Models;
using CarminePeople.Application.Organization.DTOs;
using CarminePeople.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CarminePeople.Application.Organization.Services;

public class OrganizationService
{
    private readonly IApplicationDbContext _db;
    private readonly IAuditLogger _auditLogger;

    public OrganizationService(IApplicationDbContext db, IAuditLogger auditLogger)
    {
        _db = db;
        _auditLogger = auditLogger;
    }

    // Departments
    public async Task<List<DepartmentDto>> GetDepartmentsAsync(CancellationToken ct)
    {
        var departments = await _db.Departments.Include(d => d.Manager).OrderBy(d => d.Name).ToListAsync(ct);
        return departments.Select(ToDepartmentDto).ToList();
    }

    public async Task<Result<DepartmentDto>> CreateDepartmentAsync(CreateDepartmentRequest request, Guid actorId, CancellationToken ct)
    {
        if (request.ManagerId.HasValue && !await _db.Employees.AnyAsync(e => e.Id == request.ManagerId, ct))
        {
            return Result<DepartmentDto>.Fail("MANAGER_NOT_FOUND", "Manager not found.");
        }
        var department = new Department
        {
            Name = request.Name, Code = request.Code, Description = request.Description,
            ManagerId = request.ManagerId, IsActive = request.IsActive, CreatedBy = actorId
        };
        _db.Departments.Add(department);
        await _db.SaveChangesAsync(ct);
        await _auditLogger.LogAsync(actorId, "department.created", "Department", department.Id.ToString(), null, ct);
        return Result<DepartmentDto>.Ok(await GetDepartmentDtoAsync(department.Id, ct));
    }

    public async Task<Result<DepartmentDto>> UpdateDepartmentAsync(Guid id, UpdateDepartmentRequest request, Guid actorId, CancellationToken ct)
    {
        var department = await _db.Departments.FirstOrDefaultAsync(d => d.Id == id, ct);
        if (department is null) return Result<DepartmentDto>.Fail("NOT_FOUND", "Department not found.");
        if (request.ManagerId.HasValue && !await _db.Employees.AnyAsync(e => e.Id == request.ManagerId, ct))
        {
            return Result<DepartmentDto>.Fail("MANAGER_NOT_FOUND", "Manager not found.");
        }

        department.Name = request.Name;
        department.Code = request.Code;
        department.Description = request.Description;
        department.ManagerId = request.ManagerId;
        department.IsActive = request.IsActive;
        department.UpdatedBy = actorId;
        await _db.SaveChangesAsync(ct);
        await _auditLogger.LogAsync(actorId, "department.updated", "Department", department.Id.ToString(), null, ct);
        return Result<DepartmentDto>.Ok(await GetDepartmentDtoAsync(department.Id, ct));
    }

    private async Task<DepartmentDto> GetDepartmentDtoAsync(Guid id, CancellationToken ct)
    {
        var department = await _db.Departments.Include(d => d.Manager).FirstAsync(d => d.Id == id, ct);
        return ToDepartmentDto(department);
    }

    private static DepartmentDto ToDepartmentDto(Department d) => new(
        d.Id, d.Name, d.Code, d.Description, d.ManagerId,
        d.Manager is null ? null : $"{d.Manager.FirstName} {d.Manager.LastName}", d.IsActive);

    public async Task<Result<bool>> DeleteDepartmentAsync(Guid id, Guid actorId, CancellationToken ct)
    {
        var department = await _db.Departments.FirstOrDefaultAsync(d => d.Id == id, ct);
        if (department is null) return Result<bool>.Fail("NOT_FOUND", "Department not found.");
        if (await _db.Employees.AnyAsync(e => e.DepartmentId == id, ct))
        {
            return Result<bool>.Fail("IN_USE", "Cannot delete a department that still has employees assigned.");
        }

        department.IsDeleted = true;
        department.UpdatedBy = actorId;
        await _db.SaveChangesAsync(ct);
        await _auditLogger.LogAsync(actorId, "department.deleted", "Department", department.Id.ToString(), null, ct);
        return Result<bool>.Ok(true);
    }

    // Designations
    public async Task<List<DesignationDto>> GetDesignationsAsync(Guid? departmentId, CancellationToken ct)
    {
        var q = _db.Designations.Include(d => d.Department).AsQueryable();
        if (departmentId.HasValue) q = q.Where(d => d.DepartmentId == departmentId);
        return await q.OrderBy(d => d.Title)
            .Select(d => new DesignationDto(d.Id, d.Title, d.Description, d.DepartmentId, d.Department.Name, d.IsActive))
            .ToListAsync(ct);
    }

    public async Task<Result<DesignationDto>> CreateDesignationAsync(CreateDesignationRequest request, Guid actorId, CancellationToken ct)
    {
        var department = await _db.Departments.FirstOrDefaultAsync(d => d.Id == request.DepartmentId, ct);
        if (department is null) return Result<DesignationDto>.Fail("DEPARTMENT_NOT_FOUND", "Department not found.");

        var designation = new Designation
        {
            Title = request.Title, Description = request.Description, DepartmentId = request.DepartmentId,
            IsActive = request.IsActive, CreatedBy = actorId
        };
        _db.Designations.Add(designation);
        await _db.SaveChangesAsync(ct);
        await _auditLogger.LogAsync(actorId, "designation.created", "Designation", designation.Id.ToString(), null, ct);
        return Result<DesignationDto>.Ok(new DesignationDto(designation.Id, designation.Title, designation.Description, designation.DepartmentId, department.Name, designation.IsActive));
    }

    public async Task<Result<DesignationDto>> UpdateDesignationAsync(Guid id, UpdateDesignationRequest request, Guid actorId, CancellationToken ct)
    {
        var designation = await _db.Designations.FirstOrDefaultAsync(d => d.Id == id, ct);
        if (designation is null) return Result<DesignationDto>.Fail("NOT_FOUND", "Designation not found.");

        var department = await _db.Departments.FirstOrDefaultAsync(d => d.Id == request.DepartmentId, ct);
        if (department is null) return Result<DesignationDto>.Fail("DEPARTMENT_NOT_FOUND", "Department not found.");

        designation.Title = request.Title;
        designation.Description = request.Description;
        designation.DepartmentId = request.DepartmentId;
        designation.IsActive = request.IsActive;
        designation.UpdatedBy = actorId;
        await _db.SaveChangesAsync(ct);
        await _auditLogger.LogAsync(actorId, "designation.updated", "Designation", designation.Id.ToString(), null, ct);
        return Result<DesignationDto>.Ok(new DesignationDto(designation.Id, designation.Title, designation.Description, designation.DepartmentId, department.Name, designation.IsActive));
    }

    public async Task<Result<bool>> DeleteDesignationAsync(Guid id, Guid actorId, CancellationToken ct)
    {
        var designation = await _db.Designations.FirstOrDefaultAsync(d => d.Id == id, ct);
        if (designation is null) return Result<bool>.Fail("NOT_FOUND", "Designation not found.");
        if (await _db.Employees.AnyAsync(e => e.DesignationId == id, ct))
        {
            return Result<bool>.Fail("IN_USE", "Cannot delete a designation that still has employees assigned.");
        }

        designation.IsDeleted = true;
        designation.UpdatedBy = actorId;
        await _db.SaveChangesAsync(ct);
        await _auditLogger.LogAsync(actorId, "designation.deleted", "Designation", designation.Id.ToString(), null, ct);
        return Result<bool>.Ok(true);
    }

    // Locations
    public async Task<List<LocationDto>> GetLocationsAsync(CancellationToken ct) =>
        await _db.Locations.OrderBy(l => l.Name)
            .Select(l => new LocationDto(l.Id, l.Name, l.Address, l.Latitude, l.Longitude, l.GeofenceRadiusMeters, l.IsActive, l.OfficeWifiSsid))
            .ToListAsync(ct);

    public async Task<LocationDto> CreateLocationAsync(CreateLocationRequest request, Guid actorId, CancellationToken ct)
    {
        var location = new Location
        {
            Name = request.Name,
            Address = request.Address,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            GeofenceRadiusMeters = request.GeofenceRadiusMeters,
            IsActive = request.IsActive,
            OfficeWifiSsid = request.OfficeWifiSsid,
            CreatedBy = actorId
        };
        _db.Locations.Add(location);
        await _db.SaveChangesAsync(ct);
        await _auditLogger.LogAsync(actorId, "location.created", "Location", location.Id.ToString(), null, ct);
        return new LocationDto(location.Id, location.Name, location.Address, location.Latitude, location.Longitude, location.GeofenceRadiusMeters, location.IsActive, location.OfficeWifiSsid);
    }

    public async Task<Result<LocationDto>> UpdateLocationAsync(Guid id, UpdateLocationRequest request, Guid actorId, CancellationToken ct)
    {
        var location = await _db.Locations.FirstOrDefaultAsync(l => l.Id == id, ct);
        if (location is null) return Result<LocationDto>.Fail("NOT_FOUND", "Location not found.");

        location.Name = request.Name;
        location.Address = request.Address;
        location.Latitude = request.Latitude;
        location.Longitude = request.Longitude;
        location.GeofenceRadiusMeters = request.GeofenceRadiusMeters;
        location.IsActive = request.IsActive;
        location.OfficeWifiSsid = request.OfficeWifiSsid;
        location.UpdatedBy = actorId;
        await _db.SaveChangesAsync(ct);
        await _auditLogger.LogAsync(actorId, "location.updated", "Location", location.Id.ToString(), null, ct);
        return Result<LocationDto>.Ok(new LocationDto(location.Id, location.Name, location.Address, location.Latitude, location.Longitude, location.GeofenceRadiusMeters, location.IsActive, location.OfficeWifiSsid));
    }

    public async Task<Result<bool>> DeleteLocationAsync(Guid id, Guid actorId, CancellationToken ct)
    {
        var location = await _db.Locations.FirstOrDefaultAsync(l => l.Id == id, ct);
        if (location is null) return Result<bool>.Fail("NOT_FOUND", "Location not found.");
        if (await _db.Employees.AnyAsync(e => e.LocationId == id, ct))
        {
            return Result<bool>.Fail("IN_USE", "Cannot delete a location that still has employees assigned.");
        }

        location.IsDeleted = true;
        location.UpdatedBy = actorId;
        await _db.SaveChangesAsync(ct);
        await _auditLogger.LogAsync(actorId, "location.deleted", "Location", location.Id.ToString(), null, ct);
        return Result<bool>.Ok(true);
    }

    // Shifts
    public async Task<List<ShiftDto>> GetShiftsAsync(CancellationToken ct)
    {
        var shifts = await _db.Shifts.OrderBy(s => s.Name).ToListAsync(ct);
        return shifts.Select(ToShiftDto).ToList();
    }

    public async Task<ShiftDto> CreateShiftAsync(CreateShiftRequest request, Guid actorId, CancellationToken ct)
    {
        var shift = new Shift
        {
            Name = request.Name,
            Type = request.Type,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            GracePeriodMinutes = request.GracePeriodMinutes,
            BreakMinutes = request.BreakMinutes,
            RequiredWorkMinutes = request.RequiredWorkMinutes,
            CreatedBy = actorId
        };
        _db.Shifts.Add(shift);
        await _db.SaveChangesAsync(ct);
        await _auditLogger.LogAsync(actorId, "shift.created", "Shift", shift.Id.ToString(), null, ct);
        return ToShiftDto(shift);
    }

    public async Task<Result<ShiftDto>> UpdateShiftAsync(Guid id, UpdateShiftRequest request, Guid actorId, CancellationToken ct)
    {
        var shift = await _db.Shifts.FirstOrDefaultAsync(s => s.Id == id, ct);
        if (shift is null) return Result<ShiftDto>.Fail("NOT_FOUND", "Shift not found.");

        shift.Name = request.Name;
        shift.Type = request.Type;
        shift.StartTime = request.StartTime;
        shift.EndTime = request.EndTime;
        shift.GracePeriodMinutes = request.GracePeriodMinutes;
        shift.BreakMinutes = request.BreakMinutes;
        shift.RequiredWorkMinutes = request.RequiredWorkMinutes;
        shift.UpdatedBy = actorId;
        await _db.SaveChangesAsync(ct);
        await _auditLogger.LogAsync(actorId, "shift.updated", "Shift", shift.Id.ToString(), null, ct);
        return Result<ShiftDto>.Ok(ToShiftDto(shift));
    }

    public async Task<Result<EmployeeShiftDto>> AssignShiftAsync(AssignShiftRequest request, Guid actorId, CancellationToken ct)
    {
        var employee = await _db.Employees.FirstOrDefaultAsync(e => e.Id == request.EmployeeId, ct);
        if (employee is null) return Result<EmployeeShiftDto>.Fail("EMPLOYEE_NOT_FOUND", "Employee not found.");

        var shift = await _db.Shifts.FirstOrDefaultAsync(s => s.Id == request.ShiftId, ct);
        if (shift is null) return Result<EmployeeShiftDto>.Fail("SHIFT_NOT_FOUND", "Shift not found.");

        var assignment = new EmployeeShift
        {
            EmployeeId = request.EmployeeId,
            ShiftId = request.ShiftId,
            EffectiveFrom = request.EffectiveFrom,
            EffectiveTo = request.EffectiveTo
        };
        _db.EmployeeShifts.Add(assignment);
        await _db.SaveChangesAsync(ct);
        await _auditLogger.LogAsync(actorId, "shift.assigned", "Employee", employee.Id.ToString(), new { ShiftId = shift.Id }, ct);

        return Result<EmployeeShiftDto>.Ok(new EmployeeShiftDto(assignment.Id, assignment.EmployeeId, assignment.ShiftId, shift.Name, assignment.EffectiveFrom, assignment.EffectiveTo));
    }

    public async Task<List<EmployeeShiftDto>> GetEmployeeShiftsAsync(CancellationToken ct) =>
        await _db.EmployeeShifts.Include(es => es.Shift).OrderByDescending(es => es.EffectiveFrom)
            .Select(es => new EmployeeShiftDto(es.Id, es.EmployeeId, es.ShiftId, es.Shift.Name, es.EffectiveFrom, es.EffectiveTo))
            .ToListAsync(ct);

    private static ShiftDto ToShiftDto(Shift s)
    {
        var workingMinutes = (int)(s.EndTime - s.StartTime).TotalMinutes - s.BreakMinutes;
        return new ShiftDto(s.Id, s.Name, s.Type, s.StartTime, s.EndTime, s.GracePeriodMinutes, s.BreakMinutes, Math.Max(0, workingMinutes), s.RequiredWorkMinutes);
    }
}
