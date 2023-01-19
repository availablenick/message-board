using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using MessageBoard.Data;
using MessageBoard.Models;

namespace MessageBoard.Controllers;

[Route("sections")]
[Authorize(Roles = "Moderator")]
public class SectionController : Controller
{
    public class SectionDTO
    {
        public string Name { get; set; }
        public string Description { get; set; }
    }

    private readonly MessageBoardDbContext _context;

    public SectionController(MessageBoardDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [Route("/", Name = "Home")]
    [AllowAnonymous]
    public async Task<IActionResult> Index()
    {
        var sections = await _context.Sections
            .Include(s => s.Topics)
            .ToListAsync();

        return View("Index", sections);
    }

    [HttpGet]
    [Route("{id}", Name = "SectionShow")]
    [AllowAnonymous]
    public async Task<IActionResult> Show(int id)
    {
        var section = await _context.Sections.FindAsync(id);
        if (section == null)
        {
            return NotFound();
        }

        _context.Entry(section)
            .Collection(s => s.Topics)
            .Query()
            .Include(t => t.Author)
            .Include(t => t.Posts)
            .Load();

        return View("Show", section);
    }

    [HttpGet]
    [Route("new", Name = "SectionNew")]
    public IActionResult Create()
    {
        return View("Create");
    }

    [HttpGet]
    [Route("{id}/edit", Name = "SectionEdit")]
    public async Task<IActionResult> Edit(int id)
    {
        var section = await _context.Sections.FindAsync(id);
        if (section == null)
        {
            return NotFound();
        }

        return View("Edit", section);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route("", Name = "SectionCreate")]
    public async Task<IActionResult> Create(SectionDTO sectionDTO)
    {
        if (!ModelState.IsValid)
        {
            return View("Create");
        }

        var section = _context.Sections.Where(s => s.Name == sectionDTO.Name)
            .FirstOrDefault();

        if (section != null)
        {
            ModelState.AddModelError("Name", "Name is already in use");
            return View("Create");
        }

        var newSection = MakeSection(sectionDTO);

        await _context.Sections.AddAsync(newSection);
        await _context.SaveChangesAsync();
        return Redirect("/");
    }

    [HttpPut]
    [Route("{id}", Name = "SectionUpdate")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int id, SectionDTO sectionDTO)
    {
        var section = await _context.Sections.FindAsync(id);
        if (section == null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View("Edit", section);
        }

        var existingSection = _context.Sections
            .Where(s => s.Id != section.Id && s.Name == sectionDTO.Name)
            .FirstOrDefault();

        if (existingSection != null)
        {
            ModelState.AddModelError("Name", "Name is already in use");
            return View("Edit", section);
        }

        section.Name = sectionDTO.Name;
        section.Description = sectionDTO.Description;
        section.UpdatedAt = DateTime.Now;

        _context.Sections.Update(section);
        await _context.SaveChangesAsync();
        return Redirect("/");
    }

    [HttpDelete]
    [Route("{id}", Name = "SectionDelete")]
    [ValidateAntiForgeryToken]
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
        return Redirect("/");
    }

    private Section MakeSection(SectionDTO sectionDTO)
    {
        var now = DateTime.Now;
        var section = new Section
        {
            Name = sectionDTO.Name,
            Description = sectionDTO.Description,
            CreatedAt = now,
            UpdatedAt = now,
        };

        return section;
    }
}
