using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace FlowBoardApi.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static int GetUserId(this ClaimsPrincipal user)
    {
        var sub = user.FindFirstValue(JwtRegisteredClaimNames.Sub)
                  ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.Parse(sub ?? throw new InvalidOperationException("No user id claim present."));
    }
}
