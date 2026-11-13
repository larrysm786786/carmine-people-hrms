using CarminePeople.Domain.Enums;

namespace CarminePeople.Application.Assets.DTOs;

public record AssetDto(
    Guid Id, string Name, string Category, string? SerialNumber,
    Guid? AssignedToEmployeeId, string? AssignedToEmployeeName, DateTime? AssignedDate,
    AssetStatus Status, string? Notes);

public record CreateAssetRequest(string Name, string Category, string? SerialNumber, string? Notes);

public record UpdateAssetRequest(string Name, string Category, string? SerialNumber, string? Notes, AssetStatus Status);

public record AssignAssetRequest(Guid EmployeeId);

public record AssetQuery(string? Category, AssetStatus? Status, Guid? EmployeeId);
