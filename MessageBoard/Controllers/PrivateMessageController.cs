using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

using MessageBoard.Auth;
using MessageBoard.Data;
using MessageBoard.Models;

namespace MessageBoard.Controllers;

[Route("messages")]
public class PrivateMessageController : Controller
{
    public class PrivateMessageDTO
    {
        public string Title { get; set; }
        public string Content { get; set; }
        public string[] Usernames { get; set; }
    }

    private readonly MessageBoardDbContext _context;

    public PrivateMessageController(MessageBoardDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PrivateMessageDTO privateMessageDTO)
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
        PrivateMessageDTO privateMessageDTO)
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
