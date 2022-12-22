using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

using MessageBoard.Data;
using MessageBoard.Models;

namespace MessageBoard.Controllers;

[Route("topics")]
public class TopicController : Controller
{
    public class TopicDTO
    {
        public string Title { get; set; }
        public string Content { get; set; }
    }

    private readonly MessageBoardDbContext _context;

    public TopicController(MessageBoardDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TopicDTO topicDTO)
    {
        if (!ModelState.IsValid)
        {
            return UnprocessableEntity();
        }

        var now = DateTime.Now;
        var topic = new Topic
        {
            Title = topicDTO.Title,
            Content = topicDTO.Content,
            Author = await GetAuthenticatedUser(),
            CreatedAt = now,
            UpdatedAt = now,
        };

        await _context.Topics.AddAsync(topic);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpPut]
    [Route("{id}")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int id, TopicDTO topicDTO)
    {
        var user = await GetAuthenticatedUser();
        _context.Entry(user).Collection(u => u.Topics).Load();
        if (!user.Topics.Exists(t => t.Id == id))
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            return UnprocessableEntity();
        }

        var topic = await _context.Topics.FindAsync(id);
        topic.Title = topicDTO.Title;
        topic.Content = topicDTO.Content;
        topic.UpdatedAt = DateTime.Now;
        _context.Topics.Update(topic);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private async Task<User> GetAuthenticatedUser()
    {
        Claim userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        int id = Convert.ToInt32(userIdClaim.Value);
        return await _context.Users.FindAsync(id);
    }
}
