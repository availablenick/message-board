using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;

using MessageBoard.Data;
using MessageBoard.Models;

namespace MessageBoard.Tests.PostTests;

[Collection("Sync")]
public class PostDeleteTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public PostDeleteTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Test");
    }

    [Fact]
    public async Task PostCanBeDeleted()
    {
        Post post;
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
            dbContext.Database.EnsureDeleted();
            dbContext.Database.Migrate();
            post = await DataFactory.CreatePost(dbContext);
        }

        _client.DefaultRequestHeaders.Add("UserId", post.Author.Id.ToString());
        _client.DefaultRequestHeaders.Add("X-XSRF-TOKEN", await Utilities.GetCSRFToken(_client));
        var response = await _client.DeleteAsync($"/posts/{post.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
            var postRecord = from p in dbContext.Posts
                            where p.Id == post.Id
                            select p;

            Assert.Null(postRecord.FirstOrDefault());
            var freshUser = await dbContext.Users.Include(u => u.Posts).FirstAsync(u => u.Id == post.Author.Id);
            var freshTopic = await dbContext.Topics.Include(t => t.Posts).FirstAsync(t => t.Id == post.Topic.Id);
            Assert.Equal(0, freshUser.Posts.Count);
            Assert.Equal(0, freshTopic.Posts.Count);
        }
    }

    [Fact]
    public async Task PostCanOnlyBeDeletedByItsAuthor()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
        dbContext.Database.EnsureDeleted();
        dbContext.Database.Migrate();
        var user = await DataFactory.CreateUser(dbContext);
        var newPost = await DataFactory.CreatePost(dbContext);

        _client.DefaultRequestHeaders.Add("UserId", user.Id.ToString());
        _client.DefaultRequestHeaders.Add("X-XSRF-TOKEN", await Utilities.GetCSRFToken(_client));
        var response = await _client.DeleteAsync($"/posts/{newPost.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var postRecord = from p in dbContext.Posts
                        where p.Id == newPost.Id
                        select p;

        Assert.NotNull(postRecord.FirstOrDefault());
    }

    [Fact]
    public async Task PostCannotBeDeletedByUnauthenticatedUser()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
        dbContext.Database.EnsureDeleted();
        dbContext.Database.Migrate();
        var newPost = await DataFactory.CreatePost(dbContext);

        _client.DefaultRequestHeaders.Remove("Authorization");
        var response = await _client.DeleteAsync($"/posts/{newPost.Id}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var postRecord = from p in dbContext.Posts
                        where p.Id == newPost.Id
                        select p;

        Assert.NotNull(postRecord.FirstOrDefault());
    }

    [Fact]
    public async Task UserCannotDeleteNonExistentPost()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
        dbContext.Database.EnsureDeleted();
        dbContext.Database.Migrate();
        var user = await DataFactory.CreateUser(dbContext);

        _client.DefaultRequestHeaders.Add("UserId", user.Id.ToString());
        _client.DefaultRequestHeaders.Add("X-XSRF-TOKEN", await Utilities.GetCSRFToken(_client));
        var response = await _client.DeleteAsync("/posts/1");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CSRFProtectionIsActive()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
        dbContext.Database.EnsureDeleted();
        dbContext.Database.Migrate();
        var newPost = await DataFactory.CreatePost(dbContext);

        _client.DefaultRequestHeaders.Add("UserId", newPost.Author.Id.ToString());
        var response = await _client.DeleteAsync($"/posts/{newPost.Id}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var postRecord = from p in dbContext.Posts
                        where p.Id == newPost.Id
                        select p;

        Assert.NotNull(postRecord.FirstOrDefault());
    }

    [Fact]
    public async Task HTTPMethodOverrideCanBeUsed()
    {
        Post post;
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
            dbContext.Database.EnsureDeleted();
            dbContext.Database.Migrate();
            post = await DataFactory.CreatePost(dbContext);
        }

        _client.DefaultRequestHeaders.Add("UserId", post.Author.Id.ToString());
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "_method", "DELETE" },
            { "_token", await Utilities.GetCSRFToken(_client) },
        });

        var response = await _client.PostAsync($"/posts/{post.Id}", content);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
            var postRecord = from p in dbContext.Posts
                            where p.Id == post.Id
                            select p;

            Assert.Null(postRecord.FirstOrDefault());
            var freshUser = await dbContext.Users.Include(u => u.Posts).FirstAsync(u => u.Id == post.Author.Id);
            var freshTopic = await dbContext.Topics.Include(t => t.Posts).FirstAsync(t => t.Id == post.Topic.Id);
            Assert.Equal(0, freshUser.Posts.Count);
            Assert.Equal(0, freshTopic.Posts.Count);
        }
    }
}
