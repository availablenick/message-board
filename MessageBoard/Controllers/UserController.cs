using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

using MessageBoard.Auth;
using MessageBoard.Data;
using MessageBoard.Filesystem;
using MessageBoard.Models;

namespace MessageBoard.Controllers;

[Route("users")]
public class UserController : Controller
{
    public class UserCreationDTO
    {
        public string Username { get; set; }

        [EmailAddress]
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

    private readonly IFileHandler _fileHandler;
    private readonly MessageBoardDbContext _context;

    public UserController(IFileHandler fileHandler, MessageBoardDbContext context)
    {
        _fileHandler = fileHandler;
        _context = context;
    }

    [HttpPost]
    public async Task<IActionResult> Create(UserCreationDTO userDTO)
    {
        if (User.Identity.IsAuthenticated)
        {
            return Redirect("/");
        }

        if (!DataIsValid(userDTO.Username, userDTO.Email, userDTO.Avatar))
        {
            return UnprocessableEntity();
        }

        var newUser = CreateUser(userDTO);
        if (userDTO.Avatar != null)
        {
            newUser.Avatar = _fileHandler.StoreFile(userDTO.Avatar);
        }

        await _context.Users.AddAsync(newUser);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpPut]
    [Route("{id}")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int id, UserUpdateDTO userDTO)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        if (!ResourceHandler.IsAuthorized(User, id))
        {
            return Forbid();
        }

        if (!DataIsValid(userDTO.Username, userDTO.Email, userDTO.Avatar, id))
        {
            return UnprocessableEntity();
        }

        user.Username = userDTO.Username;
        user.Email = userDTO.Email;
        user.UpdatedAt = DateTime.Now;
        if (userDTO.Avatar != null)
        {
            user.Avatar = _fileHandler.StoreFile(userDTO.Avatar);
        }

        _context.Users.Update(user);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete]
    [Route("{id}")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        if (!ResourceHandler.IsAuthorized(User, id))
        {
            return Forbid();
        }

        user.IsDeleted = true;
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private bool DataIsValid(string username, string email, IFormFile? avatar, int? id = null)
    {
        if (!ModelState.IsValid)
        {
            return false;
        }

        var user = _context.Users.Where(u => u.Username == username && u.Id != id).FirstOrDefault();
        if (user != null)
        {
            return false;
        }

        user = _context.Users.Where(u => u.Email == email && u.Id != id).FirstOrDefault();
        if (user != null)
        {
            return false;
        }

        if (!FileIsValid(avatar))
        {
            return false;
        }

        return true;
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

    private User CreateUser(UserCreationDTO userDTO)
    {
        var passwordHasher = new PasswordHasher<User>();
        var user = new User { Username = userDTO.Username, Email = userDTO.Email };
        user.PasswordHash = passwordHasher.HashPassword(user, userDTO.Password);

        var now = DateTime.Now;
        user.CreatedAt = now;
        user.UpdatedAt = now;

        return user;
    }
}
