using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using MessageBoard.Auth;
using MessageBoard.Data;
using MessageBoard.Models;

namespace MessageBoard.Controllers;

[Authorize]
[ValidateAntiForgeryToken]
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
    [Route("topics/{topicId}/posts")]
    public async Task<IActionResult> Create(int topicId, PostDTO postDTO)
    {
        if (!ModelState.IsValid)
        {
            return UnprocessableEntity();
        }

        var topic = await _context.Topics.FindAsync(topicId);
        if (topic == null)
        {
            return NotFound();
        }

        if (!topic.IsOpen)
        {
            return UnprocessableEntity();
        }

        var post = await MakePost(topic, postDTO);
        await _context.Posts.AddAsync(post);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpPut]
    [Route("posts/{id}")]
    public async Task<IActionResult> Update(int id, PostDTO postDTO)
    {
        var post = _context.Posts.Include(p => p.Author)
            .FirstOrDefault(p => p.Id == id);
        if (post == null)
        {
            return NotFound();
        }

        if (!ResourceHandler.IsAuthorized(User, post.Author.Id) &&
            !User.IsInRole("Moderator"))
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            return UnprocessableEntity();
        }

        post.Content = postDTO.Content;
        post.UpdatedAt = DateTime.Now;
        _context.Posts.Update(post);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete]
    [Route("posts/{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var post = _context.Posts.Include(p => p.Author)
            .FirstOrDefault(p => p.Id == id);
        if (post == null)
        {
            return NotFound();
        }

        if (!ResourceHandler.IsAuthorized(User, post.Author.Id) &&
            !User.IsInRole("Moderator"))
        {
            return Forbid();
        }

        _context.Posts.Remove(post);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private async Task<Post> MakePost(Topic topic, PostDTO postDTO)
    {
        var now = DateTime.Now;
        var post = new Post
        {
            Content = postDTO.Content,
            CreatedAt = now,
            UpdatedAt = now,
            Author = await UserHandler.GetAuthenticatedUser(User, _context),
            Topic = topic,
        };

        return post;
    }
}
