using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using MessageBoard.Auth;
using MessageBoard.Data;
using MessageBoard.Models;

namespace MessageBoard.Controllers;

[Route("complaints")]
[ValidateAntiForgeryToken]
public class ComplaintController : Controller
{
    private readonly MessageBoardDbContext _context;

    public ComplaintController(MessageBoardDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create(int targetId, string reason)
    {
        var target = await _context.Rateables.FindAsync(targetId);
        if (target == null)
        {
            return NotFound();
        }

        var complaint = await MakeComplaint(target, reason);
        if (!complaint.IsValid())
        {
            return UnprocessableEntity();
        }

        await _context.Complaints.AddAsync(complaint);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete]
    [Route("{id}")]
    [Authorize(Roles = "Moderator")]
    public async Task<IActionResult> Delete(int id)
    {
        var complaint = await _context.Complaints.FindAsync(id);
        if (complaint == null)
        {
            return NotFound();
        }

        _context.Complaints.Remove(complaint);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private async Task<Complaint> MakeComplaint(Rateable target, string reason)
    {
        var now = DateTime.Now;
        var complaint = new Complaint
        {
            Reason = reason,
            CreatedAt = now,
            UpdatedAt = now,
            Author = await UserHandler.GetAuthenticatedUser(User, _context),
            Target = target,
        };

        return complaint;
    }
}
