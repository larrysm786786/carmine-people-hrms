using CarminePeople.Application.Common.Interfaces;
using CarminePeople.Application.Common.Models;
using CarminePeople.Application.Employees.DTOs;
using CarminePeople.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CarminePeople.Application.Employees.Services;

public class EmployeeService
{
    private readonly IApplicationDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAuditLogger _auditLogger;

    public EmployeeService(IApplicationDbContext db, IPasswordHasher passwordHasher, IAuditLogger auditLogger)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _auditLogger = auditLogger;
    }

    public async Task<PagedResult<EmployeeListItemDto>> GetListAsync(EmployeeQuery query, CancellationToken ct)
    {
        // No .Include() needed: every field read below comes from the final .Select()
        // projection, which EF Core translates into the necessary SQL joins on its own —
        // eagerly Include()ing the full navigation entities here would pull columns the
        // projection never uses.
        var q = _db.Employees.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            q = q.Where(e => e.FirstName.Contains(term) || e.LastName.Contains(term) || e.Email.Contains(term) || e.EmployeeCode.Contains(term));
        }
        if (query.DepartmentId.HasValue) q = q.Where(e => e.DepartmentId == query.DepartmentId);
        if (query.LocationId.HasValue) q = q.Where(e => e.LocationId == query.LocationId);
        if (query.Status.HasValue) q = q.Where(e => e.EmploymentStatus == query.Status);
        if (query.ManagerId.HasValue) q = q.Where(e => e.ManagerId == query.ManagerId);

        var total = await q.CountAsync(ct);
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var descending = string.Equals(query.SortDirection, "desc", StringComparison.OrdinalIgnoreCase);
        q = query.SortBy?.ToLowerInvariant() switch
        {
            "employeecode" => descending ? q.OrderByDescending(e => e.EmployeeCode) : q.OrderBy(e => e.EmployeeCode),
            "department" => descending ? q.OrderByDescending(e => e.Department.Name) : q.OrderBy(e => e.Department.Name),
            "designation" => descending ? q.OrderByDescending(e => e.Designation.Title) : q.OrderBy(e => e.Designation.Title),
            "location" => descending ? q.OrderByDescending(e => e.Location.Name) : q.OrderBy(e => e.Location.Name),
            "role" => descending ? q.OrderByDescending(e => e.Role.Name) : q.OrderBy(e => e.Role.Name),
            "status" => descending ? q.OrderByDescending(e => e.EmploymentStatus) : q.OrderBy(e => e.EmploymentStatus),
            _ => descending
                ? q.OrderByDescending(e => e.FirstName).ThenByDescending(e => e.LastName)
                : q.OrderBy(e => e.FirstName).ThenBy(e => e.LastName),
        };

        var items = await q
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(e => new EmployeeListItemDto(e.Id, e.EmployeeCode, e.FirstName, e.LastName, e.Email,
                e.Department.Name, e.Designation.Title, e.Location.Name, e.Role.Name, e.EmploymentStatus))
            .ToListAsync(ct);

        return new PagedResult<EmployeeListItemDto> { Items = items, Page = page, PageSize = pageSize, Total = total };
    }

    public async Task<Result<EmployeeDetailDto>> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var e = await _db.Employees.AsNoTracking()
            .Include(x => x.Department).Include(x => x.Designation).Include(x => x.Location)
            .Include(x => x.Role).Include(x => x.Manager)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        if (e is null) return Result<EmployeeDetailDto>.Fail("NOT_FOUND", "Employee not found.");
        return Result<EmployeeDetailDto>.Ok(ToDetailDto(e));
    }

    public async Task<Result<EmployeeDetailDto>> CreateAsync(CreateEmployeeRequest request, Guid actorId, CancellationToken ct)
    {
        if (await _db.Employees.AnyAsync(e => e.Email == request.Email, ct))
        {
            return Result<EmployeeDetailDto>.Fail("EMAIL_TAKEN", "An employee with this email already exists.");
        }
        if (await _db.Employees.AnyAsync(e => e.EmployeeCode == request.EmployeeCode, ct))
        {
            return Result<EmployeeDetailDto>.Fail("CODE_TAKEN", "An employee with this employee code already exists.");
        }

        var employee = new Employee
        {
            EmployeeCode = request.EmployeeCode,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            PasswordHash = _passwordHasher.Hash(request.Password),
            PhoneNumber = request.PhoneNumber,
            DepartmentId = request.DepartmentId,
            DesignationId = request.DesignationId,
            LocationId = request.LocationId,
            ManagerId = request.ManagerId,
            RoleId = request.RoleId,
            DateOfJoining = request.DateOfJoining,
            DateOfBirth = request.DateOfBirth,
            CreatedBy = actorId
        };
        _db.Employees.Add(employee);
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // The AnyAsync checks above are a courtesy, not a guarantee — two concurrent
            // creates for the same email/code can both pass them before either commits. The
            // (now IsDeleted-filtered) unique indexes on Email/EmployeeCode catch the loser
            // here instead of surfacing a raw 500.
            return Result<EmployeeDetailDto>.Fail("EMAIL_OR_CODE_TAKEN", "An employee with this email or employee code already exists.");
        }
        await _auditLogger.LogAsync(actorId, "employee.created", "Employee", employee.Id.ToString(), new { employee.Email, employee.RoleId }, ct);

        return await GetByIdAsync(employee.Id, ct);
    }

    public async Task<Result<EmployeeDetailDto>> UpdateAsync(Guid id, UpdateEmployeeRequest request, Guid actorId, CancellationToken ct)
    {
        var employee = await _db.Employees.FirstOrDefaultAsync(e => e.Id == id, ct);
        if (employee is null) return Result<EmployeeDetailDto>.Fail("NOT_FOUND", "Employee not found.");

        if (request.ManagerId is Guid managerId)
        {
            if (managerId == id)
            {
                return Result<EmployeeDetailDto>.Fail("INVALID_MANAGER", "An employee cannot be their own manager.");
            }
            if (await CreatesManagerCycleAsync(id, managerId, ct))
            {
                return Result<EmployeeDetailDto>.Fail("INVALID_MANAGER", "This manager assignment would create a circular reporting chain.");
            }
        }

        var previousRoleId = employee.RoleId;

        employee.FirstName = request.FirstName;
        employee.LastName = request.LastName;
        employee.PhoneNumber = request.PhoneNumber;
        employee.DepartmentId = request.DepartmentId;
        employee.DesignationId = request.DesignationId;
        employee.LocationId = request.LocationId;
        employee.ManagerId = request.ManagerId;
        employee.RoleId = request.RoleId;
        employee.DateOfBirth = request.DateOfBirth;
        employee.EmploymentStatus = request.EmploymentStatus;
        employee.UpdatedBy = actorId;

        await _db.SaveChangesAsync(ct);
        await _auditLogger.LogAsync(actorId, "employee.updated", "Employee", employee.Id.ToString(), new { employee.EmploymentStatus }, ct);
        if (previousRoleId != request.RoleId)
        {
            await _auditLogger.LogAsync(actorId, "employee.role_changed", "Employee", employee.Id.ToString(), new { From = previousRoleId, To = request.RoleId }, ct);
        }

        return await GetByIdAsync(employee.Id, ct);
    }

    // Walks up the proposed manager's own chain of command looking for the employee being
    // updated; if found, assigning managerId would close a loop (either a direct A<->B swap
    // or a longer cycle through several managers). Capped at 50 hops as a defensive backstop
    // in case bad data already has a cycle from before this validation existed.
    private async Task<bool> CreatesManagerCycleAsync(Guid employeeId, Guid proposedManagerId, CancellationToken ct)
    {
        var currentId = (Guid?)proposedManagerId;
        for (var hops = 0; hops < 50 && currentId is not null; hops++)
        {
            if (currentId == employeeId) return true;
            currentId = await _db.Employees.Where(e => e.Id == currentId).Select(e => e.ManagerId).FirstOrDefaultAsync(ct);
        }
        return false;
    }

    public async Task<Result<EmployeeDetailDto>> UpdateSelfAsync(Guid employeeId, UpdateSelfProfileRequest request, CancellationToken ct)
    {
        var employee = await _db.Employees.FirstOrDefaultAsync(e => e.Id == employeeId, ct);
        if (employee is null) return Result<EmployeeDetailDto>.Fail("NOT_FOUND", "Employee not found.");

        employee.PhoneNumber = request.PhoneNumber;
        employee.DateOfBirth = request.DateOfBirth;
        employee.EmergencyContactName = request.EmergencyContactName;
        employee.EmergencyContactPhone = request.EmergencyContactPhone;
        employee.EmergencyContactRelation = request.EmergencyContactRelation;
        employee.UpdatedBy = employeeId;

        await _db.SaveChangesAsync(ct);
        await _auditLogger.LogAsync(employeeId, "employee.self_updated", "Employee", employee.Id.ToString(), null, ct);

        return await GetByIdAsync(employeeId, ct);
    }

    // Called after a client uploads a photo via the documents pipeline and gets back a
    // document id — this just points ProfileImageUrl at that opaque document id so it can
    // be resolved through the existing authenticated document-download endpoint.
    public async Task<Result<EmployeeDetailDto>> UpdateProfileImageAsync(Guid employeeId, UpdateProfileImageRequest request, CancellationToken ct)
    {
        var employee = await _db.Employees.FirstOrDefaultAsync(e => e.Id == employeeId, ct);
        if (employee is null) return Result<EmployeeDetailDto>.Fail("NOT_FOUND", "Employee not found.");

        employee.ProfileImageUrl = request.ProfileImageUrl;
        employee.UpdatedBy = employeeId;
        await _db.SaveChangesAsync(ct);
        await _auditLogger.LogAsync(employeeId, "employee.profile_image_updated", "Employee", employee.Id.ToString(), null, ct);

        return await GetByIdAsync(employeeId, ct);
    }

    public async Task<Result<bool>> SoftDeleteAsync(Guid id, Guid actorId, CancellationToken ct)
    {
        var employee = await _db.Employees.FirstOrDefaultAsync(e => e.Id == id, ct);
        if (employee is null) return Result<bool>.Fail("NOT_FOUND", "Employee not found.");

        employee.IsDeleted = true;
        employee.UpdatedBy = actorId;
        await _db.SaveChangesAsync(ct);
        await _auditLogger.LogAsync(actorId, "employee.deleted", "Employee", employee.Id.ToString(), null, ct);
        return Result<bool>.Ok(true);
    }

    private static EmployeeDetailDto ToDetailDto(Employee e) => new(
        e.Id, e.EmployeeCode, e.FirstName, e.LastName, e.Email, e.PhoneNumber,
        e.DepartmentId, e.Department.Name, e.DesignationId, e.Designation.Title,
        e.LocationId, e.Location.Name, e.ManagerId, e.Manager is null ? null : $"{e.Manager.FirstName} {e.Manager.LastName}",
        e.RoleId, e.Role.Name,
        e.DateOfJoining, e.DateOfBirth, e.EmploymentStatus, e.ProfileImageUrl,
        e.EmergencyContactName, e.EmergencyContactPhone, e.EmergencyContactRelation);

    // Builds the whole reporting tree starting from the given root (or every
    // manager-less employee if rootId is null) — fetched flat once, then
    // assembled in memory rather than N+1 querying per level.
    public async Task<List<OrgChartNodeDto>> GetOrgChartAsync(Guid? rootId, CancellationToken ct)
    {
        var all = await _db.Employees.AsNoTracking()
            .Include(e => e.Designation)
            .Where(e => e.EmploymentStatus == Domain.Enums.EmploymentStatus.Active)
            .ToListAsync(ct);

        OrgChartNodeDto Build(Employee e) => new(
            e.Id, e.FirstName, e.LastName, e.Designation.Title, e.ProfileImageUrl,
            all.Where(r => r.ManagerId == e.Id).Select(Build).ToList());

        var roots = rootId.HasValue
            ? all.Where(e => e.Id == rootId)
            : all.Where(e => e.ManagerId == null);

        return roots.Select(Build).ToList();
    }

    // "Upcoming" is a rolling window from today, wrapping year-end (e.g. checking the next
    // 30 days in late December must include early-January dates) — computed in memory since
    // comparing month/day across arbitrary birth years isn't expressible as a simple SQL range.
    public async Task<List<UpcomingCelebrationDto>> GetUpcomingCelebrationsAsync(int withinDays, CancellationToken ct)
    {
        var employees = await _db.Employees.AsNoTracking()
            .Where(e => e.EmploymentStatus == Domain.Enums.EmploymentStatus.Active)
            .ToListAsync(ct);

        var today = DateTime.UtcNow.Date;
        var results = new List<UpcomingCelebrationDto>();

        void AddIfUpcoming(Employee e, DateTime? date, string kind, Func<int> yearsCount)
        {
            if (date is null) return;
            var thisYear = new DateTime(today.Year, date.Value.Month, date.Value.Day);
            var next = thisYear >= today ? thisYear : thisYear.AddYears(1);
            if ((next - today).Days <= withinDays)
            {
                results.Add(new UpcomingCelebrationDto(e.Id, $"{e.FirstName} {e.LastName}", e.ProfileImageUrl, next, kind, yearsCount()));
            }
        }

        foreach (var e in employees)
        {
            AddIfUpcoming(e, e.DateOfBirth, "Birthday", () => today.Year - e.DateOfBirth!.Value.Year + (new DateTime(today.Year, e.DateOfBirth!.Value.Month, e.DateOfBirth!.Value.Day) >= today ? 0 : 1));
            AddIfUpcoming(e, e.DateOfJoining, "WorkAnniversary", () => today.Year - e.DateOfJoining.Year + (new DateTime(today.Year, e.DateOfJoining.Month, e.DateOfJoining.Day) >= today ? 0 : 1));
        }

        return results.OrderBy(r => r.Date).ToList();
    }
}
