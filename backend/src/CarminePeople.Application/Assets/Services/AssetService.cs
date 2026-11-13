using CarminePeople.Application.Assets.DTOs;
using CarminePeople.Application.Common.Interfaces;
using CarminePeople.Application.Common.Models;
using CarminePeople.Domain.Entities;
using CarminePeople.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CarminePeople.Application.Assets.Services;

public class AssetService
{
    private readonly IApplicationDbContext _db;
    private readonly IAuditLogger _auditLogger;

    public AssetService(IApplicationDbContext db, IAuditLogger auditLogger)
    {
        _db = db;
        _auditLogger = auditLogger;
    }

    public async Task<List<AssetDto>> GetListAsync(AssetQuery query, CancellationToken ct)
    {
        var q = _db.Assets.Include(a => a.AssignedToEmployee).AsQueryable();
        if (!string.IsNullOrWhiteSpace(query.Category)) q = q.Where(a => a.Category == query.Category);
        if (query.Status.HasValue) q = q.Where(a => a.Status == query.Status);
        if (query.EmployeeId.HasValue) q = q.Where(a => a.AssignedToEmployeeId == query.EmployeeId);

        var assets = await q.OrderBy(a => a.Category).ThenBy(a => a.Name).ToListAsync(ct);
        return assets.Select(ToDto).ToList();
    }

    public async Task<Result<AssetDto>> CreateAsync(CreateAssetRequest request, Guid actorId, CancellationToken ct)
    {
        var asset = new Asset
        {
            Name = request.Name,
            Category = request.Category,
            SerialNumber = request.SerialNumber,
            Notes = request.Notes,
            CreatedBy = actorId,
        };
        _db.Assets.Add(asset);
        await _db.SaveChangesAsync(ct);
        await _auditLogger.LogAsync(actorId, "asset.created", "Asset", asset.Id.ToString(), null, ct);
        return Result<AssetDto>.Ok(ToDto(asset));
    }

    public async Task<Result<AssetDto>> UpdateAsync(Guid id, UpdateAssetRequest request, Guid actorId, CancellationToken ct)
    {
        var asset = await _db.Assets.Include(a => a.AssignedToEmployee).FirstOrDefaultAsync(a => a.Id == id, ct);
        if (asset is null) return Result<AssetDto>.Fail("NOT_FOUND", "Asset not found.");

        asset.Name = request.Name;
        asset.Category = request.Category;
        asset.SerialNumber = request.SerialNumber;
        asset.Notes = request.Notes;
        asset.Status = request.Status;
        asset.UpdatedBy = actorId;

        // Manually setting status away from Assigned (e.g. straight to Retired) should also
        // release whoever it was assigned to — otherwise the asset shows as both "Retired" and
        // still pointing at an employee, which the Assign/Unassign actions alone never produce.
        if (request.Status != AssetStatus.Assigned)
        {
            asset.AssignedToEmployeeId = null;
            asset.AssignedDate = null;
        }

        await _db.SaveChangesAsync(ct);
        await _auditLogger.LogAsync(actorId, "asset.updated", "Asset", asset.Id.ToString(), null, ct);
        return Result<AssetDto>.Ok(ToDto(asset));
    }

    public async Task<Result<AssetDto>> AssignAsync(Guid id, AssignAssetRequest request, Guid actorId, CancellationToken ct)
    {
        var asset = await _db.Assets.Include(a => a.AssignedToEmployee).FirstOrDefaultAsync(a => a.Id == id, ct);
        if (asset is null) return Result<AssetDto>.Fail("NOT_FOUND", "Asset not found.");
        if (!await _db.Employees.AnyAsync(e => e.Id == request.EmployeeId, ct))
        {
            return Result<AssetDto>.Fail("EMPLOYEE_NOT_FOUND", "Employee not found.");
        }
        if (asset.Status == AssetStatus.Retired)
        {
            return Result<AssetDto>.Fail("ASSET_RETIRED", "Cannot assign a retired asset.");
        }

        asset.AssignedToEmployeeId = request.EmployeeId;
        asset.AssignedDate = DateTime.UtcNow;
        asset.Status = AssetStatus.Assigned;
        asset.UpdatedBy = actorId;
        await _db.SaveChangesAsync(ct);
        await _auditLogger.LogAsync(actorId, "asset.assigned", "Asset", asset.Id.ToString(), new { request.EmployeeId }, ct);

        var updated = await _db.Assets.Include(a => a.AssignedToEmployee).FirstAsync(a => a.Id == id, ct);
        return Result<AssetDto>.Ok(ToDto(updated));
    }

    public async Task<Result<AssetDto>> UnassignAsync(Guid id, Guid actorId, CancellationToken ct)
    {
        var asset = await _db.Assets.FirstOrDefaultAsync(a => a.Id == id, ct);
        if (asset is null) return Result<AssetDto>.Fail("NOT_FOUND", "Asset not found.");

        asset.AssignedToEmployeeId = null;
        asset.AssignedDate = null;
        asset.Status = AssetStatus.Available;
        asset.UpdatedBy = actorId;
        await _db.SaveChangesAsync(ct);
        await _auditLogger.LogAsync(actorId, "asset.unassigned", "Asset", asset.Id.ToString(), null, ct);
        return Result<AssetDto>.Ok(ToDto(asset));
    }

    public async Task<Result<bool>> DeleteAsync(Guid id, Guid actorId, CancellationToken ct)
    {
        var asset = await _db.Assets.FirstOrDefaultAsync(a => a.Id == id, ct);
        if (asset is null) return Result<bool>.Fail("NOT_FOUND", "Asset not found.");

        asset.IsDeleted = true;
        asset.UpdatedBy = actorId;
        await _db.SaveChangesAsync(ct);
        await _auditLogger.LogAsync(actorId, "asset.deleted", "Asset", asset.Id.ToString(), null, ct);
        return Result<bool>.Ok(true);
    }

    private static AssetDto ToDto(Asset a) => new(
        a.Id, a.Name, a.Category, a.SerialNumber,
        a.AssignedToEmployeeId, a.AssignedToEmployee is null ? null : $"{a.AssignedToEmployee.FirstName} {a.AssignedToEmployee.LastName}",
        a.AssignedDate, a.Status, a.Notes);
}
