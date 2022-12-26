using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

using MessageBoard.Data;
using MessageBoard.Models;

namespace MessageBoard.Controllers;

[Route("topics/{topicId}/posts")]
public class PostController : Controller
{
    public class PostDTO
    {
        public string Content { get; set; }
    }

    private readonly MessageBoardDbContext _context;

    public PostController(MessageBoardDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int topicId, PostDTO postDTO)
    {
        if (!ModelState.IsValid)
        {
            return UnprocessableEntity();
        }

        var user = await GetAuthenticatedUser();
        var topic = await _context.Topics.FindAsync(topicId);
        var now = DateTime.Now;
        var post = new Post
        {
            Content = postDTO.Content,
            CreatedAt = now,
            UpdatedAt = now,
            Author = user,
            Topic = topic,
        };

        await _context.Posts.AddAsync(post);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private async Task<User> GetAuthenticatedUser()
    {
        Claim userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        int id = Convert.ToInt32(userIdClaim.Value);
        return await _context.Users.FindAsync(id);
    }
}
