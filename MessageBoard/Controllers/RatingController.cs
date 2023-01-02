using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using MessageBoard.Auth;
using MessageBoard.Data;
using MessageBoard.Models;

namespace MessageBoard.Controllers;

[Route("ratings")]
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
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int targetId, RatingDTO ratingDTO)
    {
        var rating = await MakeRating(targetId, ratingDTO);
        if (!rating.IsValid())
        {
            return UnprocessableEntity();
        }

        await _context.Ratings.AddAsync(rating);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private async Task<Rating> MakeRating(int targetId, RatingDTO ratingDTO)
    {
        var user = await UserHandler.GetAuthenticatedUser(User, _context);
        var target = await _context.Rateables.FindAsync(targetId);
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
