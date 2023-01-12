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

    [HttpGet]
    [Route("login", Name = "AuthLogin")]
    public IActionResult LogIn()
    {
        if (User.Identity.IsAuthenticated)
        {
            return Redirect("/");
        }

        return View("Login");
    }

    [HttpPost]
    [Route("login", Name = "AuthAuthenticate")]
    public async Task<IActionResult> Authenticate(Credentials credentials)
    {
        if (User.Identity.IsAuthenticated)
        {
            return Forbid();
        }

        var user = _context.Users
            .FirstOrDefault(u => u.Username == credentials.Username);

        if (user == null)
        {
            ModelState.AddModelError("", "Username and password did not match");
            return View("Login");
        }

        var passwordHasher = new PasswordHasher<User>();
        var result = passwordHasher.VerifyHashedPassword(
            user, user.PasswordHash, credentials.Password);

        if (result != PasswordVerificationResult.Success)
        {
            ModelState.AddModelError("", "Username and password did not match");
            return View("Login");
        }

        _context.Entry(user).Reference(u => u.Ban).Load();
        if (user.Ban != null)
        {
            ModelState.AddModelError("", $"This user is banned. The ban will be lifted at {user.Ban.ExpiresAt.ToString()}");
            return View("Login");
        }

        var claims = new List<Claim> {
            new Claim(ClaimTypes.Name, user.Username.ToString()),
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
    [Route("logout", Name = "AuthLogout")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LogOut()
    {
        await HttpContext.SignOutAsync(
            CookieAuthenticationDefaults.AuthenticationScheme);

        return Redirect("/");
    }
}
