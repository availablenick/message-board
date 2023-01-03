using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using MessageBoard.Data;
using MessageBoard.Models;

namespace MessageBoard.Controllers;

[Route("sections")]
public class SectionController : Controller
{
    private readonly MessageBoardDbContext _context;

    public SectionController(MessageBoardDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    [Authorize(Roles = "Moderator")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string name)
    {
        var newSection = await MakeSection(name);
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
    [Authorize(Roles = "Moderator")]
    [ValidateAntiForgeryToken]
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

    private async Task<Section> MakeSection(string name)
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
