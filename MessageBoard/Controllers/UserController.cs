using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

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

        [Compare("Password")]
        [Display(Name = "Password confirmation")]
        public string PasswordConfirmation { get; set; }
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

    [HttpGet]
    [Route("", Name = "UserIndex")]
    public async Task<IActionResult> Index()
    {
        var users = await _context.Users.Where(u => !u.IsDeleted).ToListAsync();
        return View("Index", users);
    }

    [HttpGet]
    [Route("{id}", Name = "UserShow")]
    public async Task<IActionResult> Show(int id)
    {
        var user = await _context.Users.FindAsync(id);
        _context.Entry(user).Collection(u => u.Topics).Load();
        _context.Entry(user).Collection(u => u.Posts).Load();
        return View("Details", user);
    }

    [HttpGet]
    [Route("new", Name = "UserNew")]
    public IActionResult Create()
    {
        if (User.Identity.IsAuthenticated)
        {
            return Redirect("/");
        }

        return View("Create");
    }

    [HttpPost]
    [Route("", Name = "UserCreate")]
    public async Task<IActionResult> Create(UserCreationDTO userDTO)
    {
        if (User.Identity.IsAuthenticated)
        {
            return Redirect("/");
        }

        if (!FileIsValid(userDTO.Avatar) ||
            !ModelState.IsValid ||
            !DataIsUnique(userDTO.Username, userDTO.Email))
        {
            return View("Create");
        }

        var newUser = MakeUser(userDTO);
        if (userDTO.Avatar != null)
        {
            newUser.Avatar = _fileHandler.StoreFile(userDTO.Avatar);
        }

        await _context.Users.AddAsync(newUser);
        await _context.SaveChangesAsync();
        return Redirect("/login");
    }

    [HttpGet]
    [Route("{id}/edit", Name = "UserEdit")]
    [Authorize]
    public async Task<IActionResult> Edit(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            return View("Views/Error/404.cshtml");
        }

        if (!ResourceHandler.IsAuthorized(User, id))
        {
            return Forbid();
        }

        return View("Edit", user);
    }

    [HttpPut]
    [Route("{id}", Name = "UserUpdate")]
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

        if (!FileIsValid(userDTO.Avatar) ||
            !ModelState.IsValid ||
            !DataIsUnique(userDTO.Username, userDTO.Email, id))
        {
            return View("Edit", user);
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
        return RedirectToAction(nameof(Show), new { id = id });
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
            ModelState.AddModelError("Avatar", "File is not valid");
            return false;
        }

        return true;
    }

    private bool DataIsUnique(string username, string email, int? userId = null)
    {
        bool isUnique = true;
        var user = _context.Users.Where(u => u.Username == username && u.Id != userId).FirstOrDefault();
        if (user != null)
        {
            ModelState.AddModelError("Username", "Username is already in use");
            isUnique = false;
        }

        user = _context.Users.Where(u => u.Email == email && u.Id != userId).FirstOrDefault();
        if (user != null)
        {
            ModelState.AddModelError("Email", "Email is already in use");
            isUnique = false;
        }

        return isUnique;
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
