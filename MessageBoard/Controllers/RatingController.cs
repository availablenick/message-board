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
    public class RatingDTO
    {
        public int Value { get; set; }
        public int TargetId { get; set; }
    }

    private readonly MessageBoardDbContext _context;

    public RatingController(MessageBoardDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    public async Task<IActionResult> Create(RatingDTO ratingDTO)
    {
        var rating = await MakeRating(ratingDTO);
        if (!rating.IsValid())
        {
            return UnprocessableEntity();
        }

        await _context.Ratings.AddAsync(rating);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpPut]
    [Route("{id}")]
    public async Task<IActionResult> Update(int id, int value)
    {
        var rating = await _context.Ratings.FindAsync(id);
        if (rating == null)
        {
            return NotFound();
        }

        _context.Entry(rating)
            .Reference(r => r.Owner)
            .Load();

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
        return NoContent();
    }

    [HttpDelete]
    [Route("{id}")]
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

        _context.Ratings.Remove(rating);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private async Task<Rating> MakeRating(RatingDTO ratingDTO)
    {
        var user = await UserHandler.GetAuthenticatedUser(User, _context);
        var target = await _context.Rateables.FindAsync(ratingDTO.TargetId);
        var now = DateTime.Now;
        var rating = new Rating
        {
            Value = ratingDTO.Value,
            CreatedAt = now,
            UpdatedAt = now,
            Owner = user,
            Target = target,
        };

        return rating;
    }
}
