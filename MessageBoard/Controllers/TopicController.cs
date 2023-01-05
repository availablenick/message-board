using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using MessageBoard.Auth;
using MessageBoard.Data;
using MessageBoard.Models;

namespace MessageBoard.Controllers;

[Authorize]
[ValidateAntiForgeryToken]
public class TopicController : Controller
{
    public class TopicDTO
    {
        public string Title { get; set; }
        public string Content { get; set; }
    }

    public class TopicStatusDTO
    {
        public bool? IsPinned { get; set; }
        public bool? IsOpen { get; set; }
    }

    private readonly MessageBoardDbContext _context;

    public TopicController(MessageBoardDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    [Route("sections/{sectionId}/topics")]
    public async Task<IActionResult> Create(int sectionId, TopicDTO topicDTO)
    {
        var section = await _context.Sections.FindAsync(sectionId);
        if (section == null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return UnprocessableEntity();
        }

        var topic = await MakeTopic(section, topicDTO);
        await _context.Topics.AddAsync(topic);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpPut]
    [Route("topics/{id}")]
    public async Task<IActionResult> Update(int id, TopicDTO topicDTO)
    {
        var topic = _context.Topics.Include(t => t.Author)
            .FirstOrDefault(t => t.Id == id);
        if (topic == null)
        {
            return NotFound();
        }

        if (!ResourceHandler.IsAuthorized(User, topic.Author.Id) &&
            !User.IsInRole("Moderator"))
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
    [Route("topics/{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var topic = await _context.Topics.FindAsync(id);
        if (topic == null)
        {
            return NotFound();
        }

        _context.Entry(topic).Reference(t => t.Author).Load();
        _context.Entry(topic).Collection(t => t.Posts).Load();
        if (!ResourceHandler.IsAuthorized(User, topic.Author.Id) &&
            !User.IsInRole("Moderator"))
        {
            return Forbid();
        }

        _context.Rateables.RemoveRange(topic.Posts);
        _context.Topics.Remove(topic);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpPut]
    [Route("topics/{id}/status")]
    [Authorize(Roles = "Moderator")]
    public async Task<IActionResult> Update(int id, TopicStatusDTO topicDTO)
    {
        var topic = _context.Topics.Include(t => t.Author)
            .FirstOrDefault(t => t.Id == id);
        if (topic == null)
        {
            return NotFound();
        }

        topic.IsPinned = topicDTO.IsPinned ?? topic.IsPinned;
        topic.IsOpen = topicDTO.IsOpen ?? topic.IsOpen;

        _context.Topics.Update(topic);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private async Task<Topic> MakeTopic(Section section, TopicDTO topicDTO)
    {
        var now = DateTime.Now;
        var topic = new Topic
        {
            Title = topicDTO.Title,
            Content = topicDTO.Content,
            IsPinned = false,
            IsOpen = true,
            CreatedAt = now,
            UpdatedAt = now,
            Author = await UserHandler.GetAuthenticatedUser(User, _context),
            Section = section,
        };

        return topic;
    }
}
