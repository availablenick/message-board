using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

using MessageBoard.Auth;
using MessageBoard.Data;
using MessageBoard.Models;

namespace MessageBoard.Controllers;

[Route("messages")]
[Authorize]
public class PrivateMessageController : Controller
{
    public class PrivateMessageCreationDTO
    {
        public string Title { get; set; }
        public string Content { get; set; }
        public string[] Usernames { get; set; }
    }

    public class PrivateMessageUpdateDTO
    {
        public string Title { get; set; }
        public string Content { get; set; }
    }

    private readonly MessageBoardDbContext _context;

    public PrivateMessageController(MessageBoardDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [Route("", Name = "PrivateMessageIndex")]
    public async Task<IActionResult> Index()
    {
        var user = await UserHandler.GetAuthenticatedUser(User, _context);
        var messages = await _context.PrivateMessages
            .Where(m => m.Users.Contains(user))
            .Include(m => m.Author)
            .Include(m => m.Posts)
            .Include(m => m.Users).ToListAsync();
        return View("Index", messages);
    }

    [HttpGet]
    [Route("{id}", Name = "PrivateMessageShow")]
    public async Task<IActionResult> Show(int id)
    {
        var message = await _context.PrivateMessages.FindAsync(id);
        if (message == null)
        {
            return NotFound();
        }

        var user = await UserHandler.GetAuthenticatedUser(User, _context);
        _context.Entry(message).Collection(m => m.Users).Load();
        if (!message.Users.Contains(user))
        {
            return Forbid();
        }

        _context.Entry(message).Reference(m => m.Author).Query()
            .Include(u => u.Posts).Load();

        _context.Entry(message).Collection(m => m.Posts).Query()
            .Include(p => p.Ratings).ThenInclude(r => r.Owner)
            .Include(p => p.Author).ThenInclude(u => u.Posts)
            .Load();

        _context.Entry(message).Collection(m => m.Ratings).Query()
            .Include(r => r.Owner).Load();
        
        return View("Show", message);
    }

    [HttpGet]
    [Route("new", Name = "PrivateMessageNew")]
    public IActionResult Create()
    {
        return View("Create");
    }

    [HttpPost]
    [Route("", Name = "PrivateMessageCreate")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PrivateMessageCreationDTO privateMessageDTO)
    {
        var users = GetUsers(privateMessageDTO.Usernames);
        if (users == null)
        {
            ModelState.AddModelError("Usernames", "Some of the usernames do not exist");
            return View("Create");
        }

        if (!ModelState.IsValid)
        {
            return View("Create");
        }

        var message = await MakePrivateMessage(users, privateMessageDTO);
        await _context.PrivateMessages.AddAsync(message);
        await _context.SaveChangesAsync();
        return RedirectToRoute("PrivateMessageShow", new { id = message.Id });
    }

    [HttpGet]
    [Route("{id}/edit", Name = "PrivateMessageEdit")]
    public async Task<IActionResult> Edit(int id)
    {
        var message = await _context.PrivateMessages.FindAsync(id);
        if (message == null)
        {
            return NotFound();
        }

        _context.Entry(message).Reference(m => m.Author).Load();
        if (!ResourceHandler.IsAuthorized(User, message.Author.Id))
        {
            return Forbid();
        }

        return View("Edit", message);
    }

    [HttpPut]
    [Route("{id}", Name = "PrivateMessageUpdate")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int id,
        PrivateMessageUpdateDTO privateMessageDTO)
    {
        var message = await _context.PrivateMessages.FindAsync(id);
        if (message == null)
        {
            return NotFound();
        }

        _context.Entry(message).Reference(m => m.Author).Load();
        if (!ResourceHandler.IsAuthorized(User, message.Author.Id))
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            return View("Edit", message);
        }

        message.Title = privateMessageDTO.Title;
        message.Content = privateMessageDTO.Content;
        message.UpdatedAt = DateTime.Now;

        _context.PrivateMessages.Update(message);
        await _context.SaveChangesAsync();
        return RedirectToRoute("PrivateMessageShow", new { id = id });
    }

    private List<User> GetUsers(string[] usernames)
    {
        if (usernames == null)
        {
            return null;
        }

        var users = new List<User>();
        var usernameDict = _context.Users.ToDictionary(u => u.Username);
        foreach (var username in usernames)
        {
            if (!usernameDict.ContainsKey(username))
            {
                return null;
            }

            users.Add(usernameDict[username]);
        }

        return users;
    }

    private async Task<PrivateMessage> MakePrivateMessage(List<User> users,
        PrivateMessageCreationDTO privateMessageDTO)
    {
        var author = await UserHandler.GetAuthenticatedUser(User, _context);
        users.Add(author);
        var now = DateTime.Now;
        var message = new PrivateMessage
        {
            Title = privateMessageDTO.Title,
            Content = privateMessageDTO.Content,
            CreatedAt = now,
            UpdatedAt = now,
            Author = author,
            Users = users,
        };

        return message;
    }
}
