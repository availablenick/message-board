using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using MessageBoard.Data;
using MessageBoard.Models;

namespace MessageBoard.Controllers;

public class DiscussionController : Controller
{
    private readonly MessageBoardDbContext _context;

    public DiscussionController(MessageBoardDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [Route("discussions/{id}", Name = "DiscussionShow")]
    public async Task<IActionResult> Show(int id)
    {
        var discussion = await _context.Discussions.FindAsync(id);
        if (discussion is Topic)
        {
            return RedirectToRoute("TopicShow", new { id = id });
        }

        return Redirect($"/messages/{discussion.Id}");
    }
}
