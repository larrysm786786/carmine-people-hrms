using CarminePeople.Domain.Constants;
using CarminePeople.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CarminePeople.Infrastructure.Persistence.Seed;

// Runs on every startup (unlike DevelopmentSeeder, which only seeds an empty DB).
// Adds any permission codes introduced since the DB was first seeded and assigns
// them to roles per the same rules DevelopmentSeeder uses, without touching
// existing role-permission assignments or duplicating anything.
public static class PermissionSyncSeeder
{
    // Sourced from the shared Permissions constants (not a hand-maintained copy) so this
    // list can't silently drift out of sync with what DevelopmentSeeder/controllers grant —
    // it previously missed leave_type.manage and face.enroll, meaning any database seeded
    // before those permissions were added would never receive them on later startups.
    private static readonly string[] AllPermissionCodes = Permissions.All;

    private static readonly Dictionary<string, string[]> RoleGrants = new()
    {
        ["HRAdmin"] = new[] { Permissions.HolidayManage, Permissions.DocumentManage, Permissions.LeaveTypeManage, Permissions.FaceEnroll, Permissions.AuditView, Permissions.OvertimeApprove, Permissions.ShiftSwapApprove, Permissions.PayrollManage, Permissions.PayrollView },
        ["SuperAdmin"] = AllPermissionCodes,
        ["Manager"] = new[] { Permissions.OvertimeApprove, Permissions.ShiftSwapApprove, Permissions.OvertimeApply, Permissions.ShiftSwapApply, Permissions.PayrollView, Permissions.AnnouncementComment },
        ["Employee"] = new[] { Permissions.OvertimeApply, Permissions.ShiftSwapApply, Permissions.PayrollView, Permissions.AnnouncementComment },
    };

    // Department/location/shift management is SuperAdmin-only per spec §7. Earlier seed data
    // granted HRAdmin department.manage too broadly (it also unlocks shifts/locations); this
    // revokes it from any DB seeded before that was corrected. Safe to run every startup —
    // a no-op once already revoked.
    private static readonly Dictionary<string, string[]> RoleRevokes = new()
    {
        ["HRAdmin"] = new[] { "department.manage" },
    };

    public static async Task SyncAsync(ApplicationDbContext db)
    {
        var existingCodes = await db.Permissions.Select(p => p.Code).ToListAsync();
        var missingCodes = AllPermissionCodes.Except(existingCodes).ToList();
        if (missingCodes.Count > 0)
        {
            var newPermissions = missingCodes.Select(code => new Permission { Code = code }).ToList();
            db.Permissions.AddRange(newPermissions);
            await db.SaveChangesAsync();
        }

        var roles = await db.Roles.Include(r => r.RolePermissions).ToListAsync();
        var allPermissions = await db.Permissions.ToListAsync();
        var changed = false;

        foreach (var (roleName, grantedCodes) in RoleGrants)
        {
            var role = roles.FirstOrDefault(r => r.Name == roleName);
            if (role is null) continue;

            var alreadyGranted = role.RolePermissions.Select(rp => rp.PermissionId).ToHashSet();
            foreach (var code in grantedCodes)
            {
                var permission = allPermissions.First(p => p.Code == code);
                if (!alreadyGranted.Contains(permission.Id))
                {
                    db.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = permission.Id });
                    changed = true;
                }
            }
        }

        foreach (var (roleName, revokedCodes) in RoleRevokes)
        {
            var role = roles.FirstOrDefault(r => r.Name == roleName);
            if (role is null) continue;

            var revokedIds = allPermissions.Where(p => revokedCodes.Contains(p.Code)).Select(p => p.Id).ToHashSet();
            var toRemove = role.RolePermissions.Where(rp => revokedIds.Contains(rp.PermissionId)).ToList();
            foreach (var rp in toRemove)
            {
                db.RolePermissions.Remove(rp);
                changed = true;
            }
        }

        if (changed) await db.SaveChangesAsync();
    }
}
