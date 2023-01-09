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
[ValidateAntiForgeryToken]
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

    [HttpPost]
    public async Task<IActionResult> Create(PrivateMessageCreationDTO privateMessageDTO)
    {
        var message = await MakePrivateMessage(privateMessageDTO);
        if (!message.IsValid())
        {
            return UnprocessableEntity();
        }

        await _context.PrivateMessages.AddAsync(message);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpPut]
    [Route("{id}")]
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

        _context.Entry(message).Collection(m => m.Users).Load();
        message.Title = privateMessageDTO.Title;
        message.Content = privateMessageDTO.Content;
        message.UpdatedAt = DateTime.Now;
        if (!message.IsValid())
        {
            return UnprocessableEntity();
        }

        _context.PrivateMessages.Update(message);
        await _context.SaveChangesAsync();
        return NoContent();
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

    private async Task<PrivateMessage> MakePrivateMessage(
        PrivateMessageCreationDTO privateMessageDTO)
    {
        var author = await UserHandler.GetAuthenticatedUser(User, _context);
        var users = GetUsers(privateMessageDTO.Usernames);
        if (users != null)
        {
            users.Add(author);
        }

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
