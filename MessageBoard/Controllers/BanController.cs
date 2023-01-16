using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using MessageBoard.Data;
using MessageBoard.Models;

namespace MessageBoard.Controllers;

[Route("bans")]
[Authorize(Roles = "Moderator")]
public class BanController : Controller
{
    public class BanCreationDTO
    {
        public string Username { get; set; }
        public string Reason { get; set; }
        public DateTime ExpiresAt { get; set; }
    }

    public class BanUpdateDTO
    {
        public string Reason { get; set; }
        public DateTime ExpiresAt { get; set; }
    }

    private readonly MessageBoardDbContext _context;

    public BanController(MessageBoardDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [Route("new", Name = "BanNew")]
    public IActionResult Create()
    {
        return View("Create");
    }

    [HttpPost]
    [Route("", Name = "BanCreate")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BanCreationDTO banDTO)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Username == banDTO.Username);
        if (user == null)
        {
            ModelState.Clear();
            ModelState.AddModelError("Username", "User does not exist");
            return View("Create");
        }

        if (!ModelState.IsValid)
        {
            return View("Create");
        }

        if (!Ban.ExpirationTimeIsValid(banDTO.ExpiresAt))
        {
            ModelState.AddModelError("ExpiresAt", "Expiration time cannot be in the past");
            return View("Create");
        }

        _context.Entry(user).Reference(u => u.Ban).Load();
        if (user.HasActiveBan())
        {
            ModelState.Clear();
            ModelState.AddModelError("Uniqueness", "User is currently banned");
            return View("Create");
        }

        var ban = MakeBan(user, banDTO);
        await _context.Bans.AddAsync(ban);
        await _context.SaveChangesAsync();
        return RedirectToRoute("UserIndex");
    }

    [HttpGet]
    [Route("{id}/edit", Name = "BanEdit")]
    public async Task<IActionResult> Edit(int id)
    {
        var ban = await _context.Bans.FindAsync(id);
        if (ban == null)
        {
            return NotFound();
        }

        return View("Edit", ban);
    }

    [HttpPut]
    [Route("{id}", Name = "BanUpdate")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int id, BanUpdateDTO banDTO)
    {
        var ban = await _context.Bans.FindAsync(id);
        if (ban == null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View("Edit", ban);
        }

        if (!Ban.ExpirationTimeIsValid(banDTO.ExpiresAt))
        {
            ModelState.AddModelError("ExpiresAt", "Expiration time cannot be in the past");
            return View("Edit", ban);
        }

        ban.Reason = banDTO.Reason;
        ban.ExpiresAt = banDTO.ExpiresAt;
        ban.UpdatedAt = DateTime.Now;

        _context.Bans.Update(ban);
        await _context.SaveChangesAsync();
        return RedirectToRoute("UserIndex");
    }

    [HttpDelete]
    [Route("{id}", Name = "BanDelete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var ban = await _context.Bans.FindAsync(id);
        if (ban == null)
        {
            return NotFound();
        }

        _context.Bans.Remove(ban);
        await _context.SaveChangesAsync();
        return RedirectToRoute("UserIndex");
    }

    private Ban MakeBan(User user, BanCreationDTO banDTO)
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
