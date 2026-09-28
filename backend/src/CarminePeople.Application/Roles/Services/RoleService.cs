using CarminePeople.Application.Common.Interfaces;
using CarminePeople.Application.Common.Models;
using CarminePeople.Application.Roles.DTOs;
using CarminePeople.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CarminePeople.Application.Roles.Services;

public class RoleService
{
    private readonly IApplicationDbContext _db;
    private readonly IAuditLogger _auditLogger;

    public RoleService(IApplicationDbContext db, IAuditLogger auditLogger)
    {
        _db = db;
        _auditLogger = auditLogger;
    }

    public async Task<List<PermissionDto>> GetPermissionsAsync(CancellationToken ct) =>
        await _db.Permissions.OrderBy(p => p.Code)
            .Select(p => new PermissionDto(p.Id, p.Code, p.Description))
            .ToListAsync(ct);

    public async Task<List<RoleDto>> GetRolesAsync(CancellationToken ct)
    {
        var roles = await _db.Roles
            .Include(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
            .OrderBy(r => r.Name)
            .ToListAsync(ct);

        return roles.Select(r => new RoleDto(r.Id, r.Name, r.Description, r.RolePermissions.Select(rp => rp.Permission.Code).OrderBy(c => c).ToList())).ToList();
    }

    public async Task<Result<RoleDto>> CreateRoleAsync(CreateRoleRequest request, Guid actorId, CancellationToken ct)
    {
        if (await _db.Roles.AnyAsync(r => r.Name == request.Name, ct))
        {
            return Result<RoleDto>.Fail("NAME_TAKEN", "A role with this name already exists.");
        }

        var role = new Role { Name = request.Name, Description = request.Description, CreatedBy = actorId };
        _db.Roles.Add(role);
        await _db.SaveChangesAsync(ct);
        await _auditLogger.LogAsync(actorId, "role.created", "Role", role.Id.ToString(), null, ct);
        return Result<RoleDto>.Ok(new RoleDto(role.Id, role.Name, role.Description, new List<string>()));
    }

    public async Task<Result<RoleDto>> SetPermissionsAsync(Guid roleId, SetRolePermissionsRequest request, Guid actorId, CancellationToken ct)
    {
        var role = await _db.Roles.Include(r => r.RolePermissions).FirstOrDefaultAsync(r => r.Id == roleId, ct);
        if (role is null) return Result<RoleDto>.Fail("NOT_FOUND", "Role not found.");

        var validPermissionIds = await _db.Permissions.Where(p => request.PermissionIds.Contains(p.Id)).Select(p => p.Id).ToListAsync(ct);

        var existing = role.RolePermissions.ToList();
        foreach (var rp in existing.Where(rp => !validPermissionIds.Contains(rp.PermissionId)))
        {
            _db.RolePermissions.Remove(rp);
        }
        var existingIds = existing.Select(rp => rp.PermissionId).ToHashSet();
        foreach (var permissionId in validPermissionIds.Where(id => !existingIds.Contains(id)))
        {
            _db.RolePermissions.Add(new RolePermission { RoleId = roleId, PermissionId = permissionId });
        }

        await _db.SaveChangesAsync(ct);
        await _auditLogger.LogAsync(actorId, "role.permissions_changed", "Role", roleId.ToString(), new { PermissionIds = validPermissionIds }, ct);

        var updated = await _db.Roles.Include(r => r.RolePermissions).ThenInclude(rp => rp.Permission).FirstAsync(r => r.Id == roleId, ct);
        return Result<RoleDto>.Ok(new RoleDto(updated.Id, updated.Name, updated.Description, updated.RolePermissions.Select(rp => rp.Permission.Code).OrderBy(c => c).ToList()));
    }
}
