using System.Security.Claims;

namespace CarminePeople.Api.Common;

public static class CurrentUserExtensions
{
    public static Guid GetEmployeeId(this ClaimsPrincipal user)
    {
        var sub = user.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)
                  ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(sub, out var id) ? id : Guid.Empty;
    }
}
