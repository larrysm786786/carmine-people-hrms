using CarminePeople.Domain.Entities;

namespace CarminePeople.Application.Common.Interfaces;

public record TokenPair(string AccessToken, string RefreshToken, DateTime RefreshTokenExpiresAt);

public interface IJwtTokenService
{
    string GenerateAccessToken(Employee employee, IEnumerable<string> permissionCodes);
    string GenerateRefreshToken();
}
