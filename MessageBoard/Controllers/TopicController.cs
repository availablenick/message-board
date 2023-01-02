using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using MessageBoard.Auth;
using MessageBoard.Data;
using MessageBoard.Models;

namespace MessageBoard.Controllers;

[Route("topics")]
[Authorize]
[ValidateAntiForgeryToken]
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
            Author = await UserHandler.GetAuthenticatedUser(User, _context),
            CreatedAt = now,
            UpdatedAt = now,
        };

        await _context.Topics.AddAsync(topic);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpPut]
    [Route("{id}")]
    public async Task<IActionResult> Update(int id, TopicDTO topicDTO)
    {
        var topic = _context.Topics.Include(t => t.Author)
            .FirstOrDefault(t => t.Id == id);
        if (topic == null)
        {
            return NotFound();
        }

        if (!ResourceHandler.IsAuthorized(User, topic.Author.Id))
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            return UnprocessableEntity();
        }

        topic.Title = topicDTO.Title;
        topic.Content = topicDTO.Content;
        topic.UpdatedAt = DateTime.Now;
        _context.Topics.Update(topic);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete]
    [Route("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var topic = _context.Topics.Include(t => t.Author)
            .FirstOrDefault(t => t.Id == id);
        if (topic == null)
        {
            return NotFound();
        }

        if (!ResourceHandler.IsAuthorized(User, topic.Author.Id))
        {
            return Forbid();
        }

        _context.Topics.Remove(topic);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
