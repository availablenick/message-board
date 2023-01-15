using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using MessageBoard.Data;
using MessageBoard.Models;

namespace MessageBoard.Controllers;

public class RateableController : Controller
{
    private readonly MessageBoardDbContext _context;

    public RateableController(MessageBoardDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [Route("rateables/{id}", Name = "RateableShow")]
    public async Task<IActionResult> Show(int id)
    {
        var rateable = await _context.Rateables.FindAsync(id);
        if (rateable == null)
        {
            return NotFound();
        }

        int discussionId = rateable.Id;
        if (rateable is Post)
        {
            _context.Entry(rateable).Reference(p => ((Post) p).Discussion).Load();
            discussionId = ((Post) rateable).Discussion.Id;
        }

        return RedirectToRoute("DiscussionShow", new { id = discussionId });
    }
}
