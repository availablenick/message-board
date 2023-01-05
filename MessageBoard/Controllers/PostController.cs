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
    private readonly MessageBoardDbContext _context;

    public PostController(MessageBoardDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    [Route("topics/{topicId}/posts")]
    public async Task<IActionResult> Create(int topicId, string content)
    {
        var topic = await _context.Topics.FindAsync(topicId);
        if (topic == null)
        {
            return NotFound();
        }

        if (!topic.IsOpen)
        {
            return UnprocessableEntity();
        }

        var post = await MakePost(topic, content);
        if (!post.IsValid())
        {
            return UnprocessableEntity();
        }

        await _context.Posts.AddAsync(post);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpPut]
    [Route("posts/{id}")]
    public async Task<IActionResult> Update(int id, string content)
    {
        var post = await _context.Posts.FindAsync(id);
        if (post == null)
        {
            return NotFound();
        }

        _context.Entry(post).Reference(p => p.Author).Load();
        if (!ResourceHandler.IsAuthorized(User, post.Author.Id) &&
            !User.IsInRole("Moderator"))
        {
            return Forbid();
        }

        post.Content = content;
        post.UpdatedAt = DateTime.Now;
        if (!post.IsValid())
        {
            return UnprocessableEntity();
        }

        _context.Posts.Update(post);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete]
    [Route("posts/{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var post = await _context.Posts.FindAsync(id);
        if (post == null)
        {
            return NotFound();
        }

        _context.Entry(post).Reference(p => p.Author).Load();
        if (!ResourceHandler.IsAuthorized(User, post.Author.Id) &&
            !User.IsInRole("Moderator"))
        {
            return Forbid();
        }

        _context.Posts.Remove(post);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private async Task<Post> MakePost(Topic topic, string content)
    {
        var now = DateTime.Now;
        var post = new Post
        {
            Content = content,
            CreatedAt = now,
            UpdatedAt = now,
            Author = await UserHandler.GetAuthenticatedUser(User, _context),
            Topic = topic,
        };

        return post;
    }
}
