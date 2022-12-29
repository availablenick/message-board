using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

using MessageBoard.Auth;
using MessageBoard.Data;
using MessageBoard.Models;

namespace MessageBoard.Controllers;

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
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int topicId, PostDTO postDTO)
    {
        if (!ModelState.IsValid)
        {
            return UnprocessableEntity();
        }

        var user = await UserHandler.GetAuthenticatedUser(User, _context);
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

    [HttpPut]
    [Route("posts/{id}")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int id, PostDTO postDTO)
    {
        var post = await _context.Posts.FindAsync(id);
        if (post == null)
        {
            return NotFound();
        }

        if (!(await IsActionAllowed(id)))
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
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var post = await _context.Posts.FindAsync(id);
        if (post == null)
        {
            return NotFound();
        }

        if (!(await IsActionAllowed(id)))
        {
            return Forbid();
        }

        _context.Posts.Remove(post);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private async Task<bool> IsActionAllowed(int postId)
    {
        var user = await UserHandler.GetAuthenticatedUser(User, _context);
        if (user == null)
        {
            return false;
        }
    
        _context.Entry(user).Collection(u => u.Posts).Load();
        return user.Posts.Exists(p => p.Id == postId);
    }
}
