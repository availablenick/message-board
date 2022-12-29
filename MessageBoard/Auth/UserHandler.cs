using Microsoft.AspNetCore.Http;
using System.Security.Claims;

using MessageBoard.Data;
using MessageBoard.Models;

namespace MessageBoard.Auth;

public class UserHandler
{
    public static async Task<User> GetAuthenticatedUser(ClaimsPrincipal user,
        MessageBoardDbContext dbContext)
    {
        Claim userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier);
        int id = Convert.ToInt32(userIdClaim.Value);
        return await dbContext.Users.FindAsync(id);
    }
}
