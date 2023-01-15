using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using MessageBoard.Auth;
using MessageBoard.Data;
using MessageBoard.Models;

namespace MessageBoard.Controllers;

public class ComplaintController : Controller
{
    public class ComplaintDTO
    {
        public string Reason { get; set; }
    }

    private readonly MessageBoardDbContext _context;

    public ComplaintController(MessageBoardDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [Authorize]
    [Route("rateables/{targetId}/complaints/new", Name = "ComplaintNew")]
    public async Task<IActionResult> Create(int targetId)
    {
        var target = await _context.Rateables.FindAsync(targetId);
        return View("Create", target);
    }

    [HttpPost]
    [Authorize]
    [Route("rateables/{targetId}/complaints", Name = "ComplaintCreate")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int targetId, ComplaintDTO complaintDTO)
    {
        var target = await _context.Rateables.FindAsync(targetId);
        if (target == null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View("Create", target);
        }

        var complaint = await MakeComplaint(target, complaintDTO);
        await _context.Complaints.AddAsync(complaint);
        await _context.SaveChangesAsync();
        return RedirectToRoute("RateableShow", new { id = target.Id });
    }

    [HttpDelete]
    [Route("complaints/{id}")]
    [Authorize(Roles = "Moderator")]
    [ValidateAntiForgeryToken]
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

    private async Task<Complaint> MakeComplaint(Rateable target,
        ComplaintDTO complaintDTO)
    {
        var now = DateTime.Now;
        var complaint = new Complaint
        {
            Reason = complaintDTO.Reason,
            CreatedAt = now,
            UpdatedAt = now,
            Author = await UserHandler.GetAuthenticatedUser(User, _context),
            Target = target,
        };

        return complaint;
    }
}
