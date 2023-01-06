using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using MessageBoard.Data;
using MessageBoard.Models;

namespace MessageBoard.Controllers;

[Route("bans")]
public class BanController : Controller
{
    public class BanDTO
    {
        public string Reason { get; set; }
        public DateTime ExpiresAt { get; set; }
    }

    private readonly MessageBoardDbContext _context;

    public BanController(MessageBoardDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    [Authorize(Roles = "Moderator")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string username, BanDTO banDTO)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (user == null)
        {
            return NotFound();
        }

        var ban = MakeBan(user, banDTO);
        if (!ban.IsValid())
        {
            return UnprocessableEntity();
        }

        await _context.Bans.AddAsync(ban);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private Ban MakeBan(User user, BanDTO banDTO)
    {
        var now = DateTime.Now;
        var ban = new Ban
        {
            Reason = banDTO.Reason,
            ExpiresAt = banDTO.ExpiresAt,
            CreatedAt = now,
            UpdatedAt = now,
            User = user,
        };

        return ban;
    }
}
