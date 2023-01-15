using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using MessageBoard.Auth;
using MessageBoard.Data;
using MessageBoard.Models;

namespace MessageBoard.Controllers;

[Authorize]
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

    [HttpGet]
    [Route("topics/{id}", Name = "TopicShow")]
    [AllowAnonymous]
    public async Task<IActionResult> Show(int id)
    {
        var topic = await _context.Topics.FindAsync(id);
        if (topic == null)
        {
            return NotFound();
        }

        _context.Entry(topic).Reference(t => t.Author).Query()
            .Include(u => u.Posts).Load();

        _context.Entry(topic).Collection(t => t.Posts).Query()
            .Include(p => p.Ratings).Include(p => p.Author).ThenInclude(u => u.Posts)
            .Load();

        _context.Entry(topic).Collection(t => t.Ratings).Query()
            .Include(r => r.Owner).Load();

        return View("Show", topic);
    }

    [HttpGet]
    [Route("sections/{sectionId}/topics/new", Name = "TopicNew")]
    public async Task<IActionResult> Create(int sectionId)
    {
        var section = await _context.Sections.FindAsync(sectionId);
        if (section == null)
        {
            return NotFound();
        }

        return View("Create", section);
    }

    [HttpPost]
    [Route("sections/{sectionId}/topics", Name = "TopicCreate")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int sectionId, TopicDTO topicDTO)
    {
        var section = await _context.Sections.FindAsync(sectionId);
        if (section == null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View("Create", section);
        }

        var topic = await MakeTopic(section, topicDTO);
        await _context.Topics.AddAsync(topic);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Show), new { id = topic.Id });
    }

    [HttpGet]
    [Route("topics/{id}/edit", Name = "TopicEdit")]
    public async Task<IActionResult> Edit(int id)
    {
        var topic = await _context.Topics.FindAsync(id);
        if (topic == null)
        {
            return NotFound();
        }

        _context.Entry(topic).Reference(t => t.Author).Load();
        if (!ResourceHandler.IsAuthorized(User, topic.Author.Id) &&
            !User.IsInRole("Moderator"))
        {
            return Forbid();
        }

        return View("Edit", topic);
    }

    [HttpPut]
    [Route("topics/{id}", Name = "TopicUpdate")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int id, TopicDTO topicDTO)
    {
        var topic = await _context.Topics.FindAsync(id);
        if (topic == null)
        {
            return NotFound();
        }

        _context.Entry(topic).Reference(t => t.Author).Load();
        if (!ResourceHandler.IsAuthorized(User, topic.Author.Id) &&
            !User.IsInRole("Moderator"))
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            return View("Edit", topic);
        }

        topic.Title = topicDTO.Title;
        topic.Content = topicDTO.Content;
        topic.UpdatedAt = DateTime.Now;

        _context.Topics.Update(topic);
        await _context.SaveChangesAsync();
        return RedirectToRoute("TopicShow", new { id = topic.Id });
    }

    [HttpDelete]
    [Route("topics/{id}", Name = "TopicDelete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var topic = await _context.Topics.FindAsync(id);
        if (topic == null)
        {
            return NotFound();
        }

        _context.Entry(topic).Reference(t => t.Author).Load();
        if (!ResourceHandler.IsAuthorized(User, topic.Author.Id) &&
            !User.IsInRole("Moderator"))
        {
            return Forbid();
        }

        _context.Entry(topic).Reference(t => t.Section).Load();
        _context.Entry(topic).Collection(t => t.Posts).Load();
        _context.Rateables.RemoveRange(topic.Posts);
        _context.Topics.Remove(topic);
        await _context.SaveChangesAsync();
        return RedirectToRoute("SectionShow", new { id = topic.Section.Id });
    }

    [HttpPut]
    [Route("topics/{id}/status", Name = "TopicStatus")]
    [Authorize(Roles = "Moderator")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int id, TopicStatusDTO topicDTO)
    {
        var topic = await _context.Topics.FindAsync(id);
        if (topic == null)
        {
            return NotFound();
        }

        _context.Entry(topic).Reference(t => t.Section).Load();
        topic.IsPinned = topicDTO.IsPinned ?? topic.IsPinned;
        topic.IsOpen = topicDTO.IsOpen ?? topic.IsOpen;

        _context.Topics.Update(topic);
        await _context.SaveChangesAsync();
        return RedirectToRoute("SectionShow", new { id = topic.Section.Id });
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
