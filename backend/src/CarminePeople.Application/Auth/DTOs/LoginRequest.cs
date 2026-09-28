namespace CarminePeople.Application.Auth.DTOs;

public record LoginRequest(string Email, string Password);

public record LoginResponse(string AccessToken, string RefreshToken, DateTime RefreshTokenExpiresAt, EmployeeProfileDto Employee);

public record RefreshRequest(string RefreshToken);

public record LogoutRequest(string RefreshToken);

public record ForgotPasswordRequest(string Email);

public record ResetPasswordRequest(string Token, string NewPassword);

public record EmployeeProfileDto(
    Guid Id,
    string EmployeeCode,
    string FirstName,
    string LastName,
    string Email,
    string RoleName,
    Guid DepartmentId,
    Guid DesignationId,
    Guid LocationId,
    IReadOnlyList<string> Permissions);
