using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

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

        if (!MessageBoard.Models.User.DataIsValidForCreation(userDTO.Username,
                userDTO.Email, userDTO.Password, userDTO.Avatar?.FileName) ||
            !DataIsUnique(userDTO.Username, userDTO.Email))
        {
            return UnprocessableEntity();
        }

        var newUser = MakeUser(userDTO);
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

        if (!MessageBoard.Models.User.DataIsValidForUpdate(userDTO.Username,
                userDTO.Email, userDTO.Avatar?.FileName) ||
            !DataIsUnique(userDTO.Username, userDTO.Email, id))
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

    private bool DataIsUnique(string username, string email, int? userId = null)
    {
        var user = _context.Users.Where(u => u.Username == username && u.Id != userId).FirstOrDefault();
        if (user != null)
        {
            return false;
        }

        user = _context.Users.Where(u => u.Email == email && u.Id != userId).FirstOrDefault();
        if (user != null)
        {
            return false;
        }

        return true;
    }

    private User MakeUser(UserCreationDTO userDTO)
    {
        var passwordHasher = new PasswordHasher<User>();
        var user = new User { Username = userDTO.Username, Email = userDTO.Email };
        user.PasswordHash = passwordHasher.HashPassword(user, userDTO.Password);
        user.IsDeleted = false;
        var now = DateTime.Now;
        user.CreatedAt = now;
        user.UpdatedAt = now;

        return user;
    }
}
