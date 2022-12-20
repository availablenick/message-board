using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

using MessageBoard.Data;
using MessageBoard.Models;

namespace MessageBoard.Controllers;

[Route("users")]
public class UserController : Controller
{
    public class UserCreationDTO
    {
        public string Username { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public IFormFile? Avatar { get; set; }
    }

    public class UserUpdateDTO
    {
        public string Username { get; set; }
        public string Email { get; set; }
        public IFormFile? Avatar { get; set; }
    }

    private readonly MessageBoardDbContext _context;
    private readonly IWebHostEnvironment _env;

    public UserController(MessageBoardDbContext context, IWebHostEnvironment env)
    {
        _context = context;
        _env = env;
    }

    [HttpPost]
    public async Task<IActionResult> Create(UserCreationDTO userDTO)
    {
        if (User.Identity.IsAuthenticated)
        {
            return Redirect("/");
        }

        if (!ModelState.IsValid)
        {
            return UnprocessableEntity();
        }

        if (!FileIsValid(userDTO.Avatar))
        {
            return UnprocessableEntity();
        }

        var user = CreateUser(userDTO);
        if (userDTO.Avatar != null)
        {
            user.Avatar = await StoreFile(userDTO.Avatar);
        }

        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpPut]
    [Route("{id}")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int id, UserUpdateDTO userDTO)
    {
        var userIdClaim = User.FindFirst(claim => claim.Type == ClaimTypes.NameIdentifier);
        if (userIdClaim == null || Convert.ToInt32(userIdClaim.Value) != id)
        {
            return Forbid();
        }

        var user = await _context.Users.FindAsync(id);
        if (user == null) {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return UnprocessableEntity();
        }

        if (!FileIsValid(userDTO.Avatar))
        {
            return UnprocessableEntity();
        }

        user.Username = userDTO.Username;
        user.Email = userDTO.Email;
        if (userDTO.Avatar != null)
        {
            user.Avatar = await StoreFile(userDTO.Avatar);
        }

        _context.Users.Update(user);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private bool FileIsValid(IFormFile file)
    {
        if (file == null)
        {
            return true;
        }

        string[] allowedExtensions = new string[] { ".jpg", ".jpeg", ".png" };
        string extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (string.IsNullOrEmpty(extension) || !allowedExtensions.Contains(extension))
        {
            return false;
        }

        return true;
    }

    private async Task<string> StoreFile(IFormFile file)
    {
        string imageDirectory = $"{_env.ContentRootPath}Storage/Images";
        if (!Directory.Exists(imageDirectory))
        {
            Directory.CreateDirectory(imageDirectory);
        }

        string fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
        string fileSubpath = $"Storage/Images/{Guid.NewGuid().ToString()}{fileExtension}";
        string filepath = $"{_env.ContentRootPath}{fileSubpath}";
        using (var fileStream = System.IO.File.Create(filepath))
        {
            await file.CopyToAsync(fileStream);
        }

        return fileSubpath;
    }

    private User CreateUser(UserCreationDTO userDTO)
    {
        var passwordHasher = new PasswordHasher<User>();
        var user = new User { Username = userDTO.Username, Email = userDTO.Email };
        user.PasswordHash = passwordHasher.HashPassword(user, userDTO.Password);

        return user;
    }
}
