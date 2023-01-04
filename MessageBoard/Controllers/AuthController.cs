using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

using MessageBoard.Data;
using MessageBoard.Models;

namespace MessageBoard.Controllers;

public class AuthController : Controller
{
    public class Credentials
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    private readonly MessageBoardDbContext _context;

    public AuthController(MessageBoardDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    [Route("login")]
    public async Task<IActionResult> LogIn(Credentials credentials)
    {
        if (User.Identity.IsAuthenticated)
        {
            return Forbid();
        }

        var user = _context.Users.Where(u => u.Username == credentials.Username)
            .FirstOrDefault();

        if (user == null)
        {
            return UnprocessableEntity();
        }

        var passwordHasher = new PasswordHasher<User>();
        var result = passwordHasher.VerifyHashedPassword(
            user, user.PasswordHash, credentials.Password);

        if (result != PasswordVerificationResult.Success)
        {
            return UnprocessableEntity();
        }

        var claims = new List<Claim> {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        };

        if (user.Role != null)
        {
            claims.Add(new Claim(ClaimTypes.Role, user.Role));
        }

        var claimsIdentity = new ClaimsIdentity(
            claims, CookieAuthenticationDefaults.AuthenticationScheme);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity));

        return Redirect("/");
    }

    [HttpPost]
    [Route("logout")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LogOut()
    {
        await HttpContext.SignOutAsync(
            CookieAuthenticationDefaults.AuthenticationScheme);

        return Redirect("/");
    }
}
