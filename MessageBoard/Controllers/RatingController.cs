using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using MessageBoard.Auth;
using MessageBoard.Data;
using MessageBoard.Models;

namespace MessageBoard.Controllers;

[Route("ratings")]
[Authorize]
[ValidateAntiForgeryToken]
public class RatingController : Controller
{
    private readonly MessageBoardDbContext _context;

    public RatingController(MessageBoardDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    [Route("", Name = "RatingCreate")]
    public async Task<IActionResult> Create(int targetId, int value)
    {
        var target = await _context.Rateables.FindAsync(targetId);
        if (target == null)
        {
            return NotFound();
        }

        var user = await UserHandler.GetAuthenticatedUser(User, _context);
        _context.Entry(target).Collection(t => t.Ratings).Query()
            .Include(r => r.Owner).Load();

        if (target.Ratings.Exists(r => r.Owner.Id == user.Id))
        {
            return UnprocessableEntity();
        }

        var rating = await MakeRating(target, value);
        if (!rating.IsValid())
        {
            return UnprocessableEntity();
        }

        await _context.Ratings.AddAsync(rating);
        await _context.SaveChangesAsync();
        return RedirectToRoute("RateableShow", new { id = targetId });
    }

    [HttpPut]
    [Route("{id}", Name = "RatingUpdate")]
    public async Task<IActionResult> Update(int id, int value)
    {
        var rating = await _context.Ratings.FindAsync(id);
        if (rating == null)
        {
            return NotFound();
        }

        _context.Entry(rating).Reference(r => r.Owner).Load();
        if (!ResourceHandler.IsAuthorized(User, rating.Owner.Id))
        {
            return Forbid();
        }

        rating.Value = value;
        rating.UpdatedAt = DateTime.Now;
        _context.Entry(rating)
            .Reference(r => r.Target)
            .Load();

        if (!rating.IsValid())
        {
            return UnprocessableEntity();
        }

        _context.Ratings.Update(rating);
        await _context.SaveChangesAsync();
        _context.Entry(rating).Reference(r => r.Target).Load();
        return RedirectToRoute("RateableShow", new { id = rating.Target.Id });
    }

    [HttpDelete]
    [Route("{id}", Name = "RatingDelete")]
    public async Task<IActionResult> Delete(int id)
    {
        var rating = await _context.Ratings.FindAsync(id);
        if (rating == null)
        {
            return NotFound();
        }

        _context.Entry(rating).Reference(r => r.Owner).Load();
        if (!ResourceHandler.IsAuthorized(User, rating.Owner.Id))
        {
            return Forbid();
        }

        _context.Entry(rating).Reference(r => r.Target).Load();
        _context.Ratings.Remove(rating);
        await _context.SaveChangesAsync();
        return RedirectToRoute("RateableShow", new { id = rating.Target.Id });
    }

    private async Task<Rating> MakeRating(Rateable target, int value)
    {
        var now = DateTime.Now;
        var rating = new Rating
        {
            Value = value,
            CreatedAt = now,
            UpdatedAt = now,
            Owner = await UserHandler.GetAuthenticatedUser(User, _context),
            Target = target,
        };

        return rating;
    }
}
