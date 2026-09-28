using CarminePeople.Application.Auth.DTOs;
using CarminePeople.Application.Common.Interfaces;
using CarminePeople.Application.Common.Models;
using CarminePeople.Domain.Entities;
using CarminePeople.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CarminePeople.Application.Auth.Services;

public class AuthService
{
    private const int RefreshTokenDays = 7;

    private readonly IApplicationDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IAuditLogger _auditLogger;

    public AuthService(IApplicationDbContext db, IPasswordHasher passwordHasher, IJwtTokenService jwtTokenService, IAuditLogger auditLogger)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _auditLogger = auditLogger;
    }

    public async Task<Result<LoginResponse>> LoginAsync(LoginRequest request, string? ipAddress, CancellationToken ct)
    {
        var employee = await _db.Employees
            .Include(e => e.Role).ThenInclude(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(e => e.Email == request.Email && !e.IsDeleted, ct);

        if (employee is null || employee.EmploymentStatus != EmploymentStatus.Active
            || !_passwordHasher.Verify(request.Password, employee.PasswordHash))
        {
            await _auditLogger.LogAsync(employee?.Id, "login.failed", "Employee", employee?.Id.ToString(), new { request.Email, ipAddress }, ct);
            return Result<LoginResponse>.Fail("INVALID_CREDENTIALS", "Email or password is incorrect.");
        }

        var permissionCodes = employee.Role.RolePermissions.Select(rp => rp.Permission.Code).ToList();
        var response = await IssueTokensAsync(employee, permissionCodes, ipAddress, ct);
        await _auditLogger.LogAsync(employee.Id, "login.succeeded", "Employee", employee.Id.ToString(), new { ipAddress }, ct);
        return Result<LoginResponse>.Ok(response);
    }

    public async Task<Result<LoginResponse>> RefreshAsync(string refreshToken, string? ipAddress, CancellationToken ct)
    {
        var tokenHash = HashToken(refreshToken);
        var stored = await _db.RefreshTokens
            .Include(rt => rt.Employee).ThenInclude(e => e.Role).ThenInclude(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash, ct);

        if (stored is null || !stored.IsActive)
        {
            return Result<LoginResponse>.Fail("INVALID_REFRESH_TOKEN", "Refresh token is invalid or expired.");
        }

        stored.RevokedAt = DateTime.UtcNow;

        var permissionCodes = stored.Employee.Role.RolePermissions.Select(rp => rp.Permission.Code).ToList();
        var response = await IssueTokensAsync(stored.Employee, permissionCodes, ipAddress, ct);

        var newHash = HashToken(response.RefreshToken);
        var newStored = await _db.RefreshTokens.FirstOrDefaultAsync(rt => rt.TokenHash == newHash, ct);
        if (newStored is not null)
        {
            stored.ReplacedByTokenId = newStored.Id;
        }

        await _db.SaveChangesAsync(ct);
        return Result<LoginResponse>.Ok(response);
    }

    public async Task LogoutAsync(string refreshToken, string? ipAddress, CancellationToken ct)
    {
        var tokenHash = HashToken(refreshToken);
        var stored = await _db.RefreshTokens.FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash, ct);
        if (stored is not null && stored.IsActive)
        {
            stored.RevokedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
            await _auditLogger.LogAsync(stored.EmployeeId, "logout", "Employee", stored.EmployeeId.ToString(), new { ipAddress }, ct);
        }
    }

    // Always completes silently (no indication of whether the email exists) to avoid
    // leaking account existence to an unauthenticated caller.
    public async Task ForgotPasswordAsync(string email, CancellationToken ct)
    {
        var employee = await _db.Employees.FirstOrDefaultAsync(e => e.Email == email && !e.IsDeleted, ct);
        if (employee is null) return;

        var plainToken = _jwtTokenService.GenerateRefreshToken();
        _db.PasswordResetTokens.Add(new PasswordResetToken
        {
            EmployeeId = employee.Id,
            TokenHash = HashToken(plainToken),
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(ct);

        // TODO(Phase 8 follow-up): dispatch plainToken via the email delivery service once
        // one is wired up. Intentionally not logged/returned here — it is a live credential.
    }

    public async Task<Result<bool>> ResetPasswordAsync(string token, string newPassword, CancellationToken ct)
    {
        var tokenHash = HashToken(token);
        var stored = await _db.PasswordResetTokens.Include(t => t.Employee).FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);
        if (stored is null || !stored.IsActive)
        {
            return Result<bool>.Fail("INVALID_TOKEN", "This password reset link is invalid or has expired.");
        }

        stored.Employee.PasswordHash = _passwordHasher.Hash(newPassword);
        stored.UsedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        await _auditLogger.LogAsync(stored.EmployeeId, "password.reset", "Employee", stored.EmployeeId.ToString(), null, ct);

        return Result<bool>.Ok(true);
    }

    private async Task<LoginResponse> IssueTokensAsync(Employee employee, List<string> permissionCodes, string? ipAddress, CancellationToken ct)
    {
        var accessToken = _jwtTokenService.GenerateAccessToken(employee, permissionCodes);
        var refreshTokenPlain = _jwtTokenService.GenerateRefreshToken();
        var expiresAt = DateTime.UtcNow.AddDays(RefreshTokenDays);

        _db.RefreshTokens.Add(new RefreshToken
        {
            EmployeeId = employee.Id,
            TokenHash = HashToken(refreshTokenPlain),
            ExpiresAt = expiresAt,
            CreatedAt = DateTime.UtcNow,
            CreatedByIp = ipAddress
        });
        await _db.SaveChangesAsync(ct);

        var profile = new EmployeeProfileDto(
            employee.Id, employee.EmployeeCode, employee.FirstName, employee.LastName, employee.Email,
            employee.Role.Name, employee.DepartmentId, employee.DesignationId, employee.LocationId, permissionCodes);

        return new LoginResponse(accessToken, refreshTokenPlain, expiresAt, profile);
    }

    private static string HashToken(string token)
    {
        var bytes = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(bytes);
    }
}
