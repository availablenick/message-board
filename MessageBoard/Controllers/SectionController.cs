using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using MessageBoard.Data;
using MessageBoard.Models;

namespace MessageBoard.Controllers;

[Route("sections")]
[Authorize(Roles = "Moderator")]
[ValidateAntiForgeryToken]
public class SectionController : Controller
{
    private readonly MessageBoardDbContext _context;

    public SectionController(MessageBoardDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    public async Task<IActionResult> Create(string name)
    {
        var newSection = MakeSection(name);
        if (!newSection.IsValid())
        {
            return UnprocessableEntity();
        }

        var section = _context.Sections.Where(s => s.Name == name).FirstOrDefault();
        if (section != null)
        {
            return UnprocessableEntity();
        }

        await _context.Sections.AddAsync(newSection);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpPut]
    [Route("{id}")]
    public async Task<IActionResult> Update(int id, string name)
    {
        var section = await _context.Sections.FindAsync(id);
        if (section == null)
        {
            return NotFound();
        }

        section.Name = name;
        section.UpdatedAt = DateTime.Now;
        if (!section.IsValid())
        {
            return UnprocessableEntity();
        }

        _context.Sections.Update(section);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete]
    [Route("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var section = await _context.Sections.FindAsync(id);
        if (section == null)
        {
            return NotFound();
        }

        _context.Entry(section)
            .Collection(s => s.Topics)
            .Query()
            .Include(t => t.Posts)
            .Load();

        foreach (var topic in section.Topics)
        {
            _context.Rateables.RemoveRange(topic.Posts);
        }

        _context.Rateables.RemoveRange(section.Topics);
        _context.Sections.Remove(section);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private Section MakeSection(string name)
    {
        var now = DateTime.Now;
        var section = new Section
        {
            Name = name,
            CreatedAt = now,
            UpdatedAt = now,
        };

        return section;
    }
}
