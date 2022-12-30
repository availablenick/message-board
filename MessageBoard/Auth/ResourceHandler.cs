using Microsoft.AspNetCore.Http;
using System.Security.Claims;

using MessageBoard.Data;

namespace MessageBoard.Auth;

public class ResourceHandler
{
    public static bool IsAuthorized(ClaimsPrincipal user, int resourceOwnerId)
    {
        Claim userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier);
        return userIdClaim != null && userIdClaim.Value == resourceOwnerId.ToString();
    }
}
