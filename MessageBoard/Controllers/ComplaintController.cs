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
    public class ComplaintDTO
    {
        public string Reason { get; set; }
        public int TargetId { get; set; }
    }

    private readonly MessageBoardDbContext _context;

    public ComplaintController(MessageBoardDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create(ComplaintDTO complaintDTO)
    {
        var complaint = await MakeComplaint(complaintDTO);
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

    private async Task<Complaint> MakeComplaint(ComplaintDTO complaintDTO)
    {
        var author = await UserHandler.GetAuthenticatedUser(User, _context);
        var target = await _context.Rateables.FindAsync(complaintDTO.TargetId);
        var now = DateTime.Now;
        var complaint = new Complaint
        {
            Reason = complaintDTO.Reason,
            CreatedAt = now,
            UpdatedAt = now,
            Author = author,
            Target = target,
        };

        return complaint;
    }
}
