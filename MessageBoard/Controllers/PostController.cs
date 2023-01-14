using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using MessageBoard.Auth;
using MessageBoard.Data;
using MessageBoard.Models;

namespace MessageBoard.Controllers;

[Authorize]
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
    [Route("discussions/{discussionId}/posts", Name = "PostCreate")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int discussionId, PostDTO postDTO)
    {
        var discussion = await _context.Discussions.FindAsync(discussionId);
        if (discussion == null)
        {
            return NotFound();
        }

        if (!discussion.CanBePostedOn())
        {
            return UnprocessableEntity();
        }

        if (!ModelState.IsValid)
        {
            if (discussion is Topic)
            {
                _context.Entry(discussion).Reference(d => d.Author).Query()
                    .Include(u => u.Posts).Load();

                _context.Entry(discussion).Collection(d => d.Posts).Query()
                    .Include(p => p.Author).ThenInclude(u => u.Posts).Load();

                return View("/Views/Topic/Show.cshtml", discussion);
            }
        }

        var post = await MakePost(discussion, postDTO);
        await _context.Posts.AddAsync(post);
        await _context.SaveChangesAsync();
        if (discussion is Topic)
        {
            return RedirectToRoute("TopicShow", new { id = discussion.Id });
        }

        return NoContent();
    }

    [HttpGet]
    [Route("posts/{id}/edit", Name = "PostEdit")]
    public async Task<IActionResult> Edit(int id)
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

        return View("Edit", post);
    }

    [HttpPut]
    [Route("posts/{id}", Name = "PostUpdate")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int id, PostDTO postDTO)
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

        if (!ModelState.IsValid)
        {
            return View("Edit", post);
        }

        _context.Entry(post).Reference(p => p.Discussion).Load();
        post.Content = postDTO.Content;
        post.UpdatedAt = DateTime.Now;

        _context.Posts.Update(post);
        await _context.SaveChangesAsync();
        if (post.Discussion is Topic)
        {
            return RedirectToRoute("TopicShow", new { id = post.Discussion.Id });
        }

        return NoContent();
    }

    [HttpDelete]
    [Route("posts/{id}", Name = "PostDelete")]
    [ValidateAntiForgeryToken]
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

        _context.Entry(post).Reference(p => p.Discussion).Load();
        _context.Posts.Remove(post);
        await _context.SaveChangesAsync();
        if (post.Discussion is Topic)
        {
            return RedirectToRoute("TopicShow", new { id = post.Discussion.Id });
        }

        return NoContent();
    }

    private async Task<Post> MakePost(Discussion discussion, PostDTO postDTO)
    {
        var now = DateTime.Now;
        var post = new Post
        {
            Content = postDTO.Content,
            CreatedAt = now,
            UpdatedAt = now,
            Author = await UserHandler.GetAuthenticatedUser(User, _context),
            Discussion = discussion,
        };

        return post;
    }
}
