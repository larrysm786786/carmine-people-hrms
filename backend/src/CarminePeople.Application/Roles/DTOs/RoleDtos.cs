namespace CarminePeople.Application.Roles.DTOs;

public record PermissionDto(Guid Id, string Code, string? Description);

public record RoleDto(Guid Id, string Name, string? Description, List<string> PermissionCodes);

public record CreateRoleRequest(string Name, string? Description);

public record SetRolePermissionsRequest(List<Guid> PermissionIds);
