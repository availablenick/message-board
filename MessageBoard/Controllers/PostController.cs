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
    [Route("topics/{topicId}/posts", Name = "TopicPostCreate")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateInTopic(int topicId, PostDTO postDTO)
    {
        var topic = await _context.Topics.FindAsync(topicId);
        if (topic == null)
        {
            return NotFound();
        }

        if (!topic.IsOpen)
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            LoadDataForDiscussionShowView(topic);
            return View("/Views/Topic/Show.cshtml", topic);
        }

        var post = await MakePost(topic, postDTO);
        await _context.Posts.AddAsync(post);
        await _context.SaveChangesAsync();
        return RedirectToRoute("TopicShow", new { id = topic.Id });
    }

    [HttpPost]
    [Route("messages/{messageId}/posts", Name = "PrivateMessagePostCreate")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateInPrivateMessage(int messageId,
        PostDTO postDTO)
    {
        var message = await _context.PrivateMessages.FindAsync(messageId);
        if (message == null)
        {
            return NotFound();
        }

        _context.Entry(message).Collection(d => d.Users).Load();
        var user = await UserHandler.GetAuthenticatedUser(User, _context);
        if (!message.Users.Contains(user))
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            LoadDataForDiscussionShowView(message);
            return View("/Views/PrivateMessage/Show.cshtml", message);
        }

        var post = await MakePost(message, postDTO);
        await _context.Posts.AddAsync(post);
        await _context.SaveChangesAsync();
        return RedirectToRoute("PrivateMessageShow", new { id = message.Id });
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
        return RedirectToRoute("DiscussionShow", new { id = post.Discussion.Id });
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
        return RedirectToRoute("DiscussionShow", new { id = post.Discussion.Id });
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

    private void LoadDataForDiscussionShowView(Discussion discussion)
    {
        _context.Entry(discussion).Reference(d => d.Author).Query()
            .Include(u => u.Posts).Load();

        _context.Entry(discussion).Collection(d => d.Posts).Query()
            .Include(p => p.Ratings).Include(p => p.Author)
            .ThenInclude(u => u.Posts).Load();

        _context.Entry(discussion).Collection(d => d.Ratings).Query()
            .Include(r => r.Owner).Load();
    }
}
