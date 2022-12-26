using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;

using MessageBoard.Data;
using MessageBoard.Models;
using MessageBoard.Tests.Factories;

namespace MessageBoard.Tests.PostTests;

[Collection("Sync")]
public class PostUpdateTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public PostUpdateTests(CustomWebApplicationFactory<Program> factory)
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
    public async Task PostCanBeUpdated()
    {
        User user;
        Topic topic;
        Post newPost;
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
            dbContext.Database.EnsureDeleted();
            dbContext.Database.Migrate();
            user = await UserFactory.CreateUser(dbContext);
            topic = await TopicFactory.CreateTopic(user, dbContext);
            newPost = await PostFactory.CreatePost(user, topic, dbContext);
        }

        _client.DefaultRequestHeaders.Add("UserId", user.Id.ToString());
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "_token", await Utilities.GetCSRFToken(_client) },
            { "content", $"{newPost.Content}_edit" },
        });

        DateTime timeBeforeResponse = DateTime.Now;
        var response = await _client.PutAsync($"/posts/{newPost.Id}", content);
        DateTime timeAfterResponse = DateTime.Now;

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
            var postRecord = from p in dbContext.Posts
                            where p.Content == $"{newPost.Content}_edit"
                            select p;

            var post = postRecord.FirstOrDefault();
            Assert.NotNull(post);
            Assert.True(post.UpdatedAt.CompareTo(timeBeforeResponse) >= 0);
            Assert.True(post.UpdatedAt.CompareTo(timeAfterResponse) <= 0);
        }
    }

    [Fact]
    public async Task PostCanOnlyBeUpdatedByItsAuthor()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
        dbContext.Database.EnsureDeleted();
        dbContext.Database.Migrate();
        var user1 = await UserFactory.CreateUser(dbContext);
        var user2 = await UserFactory.CreateUser(dbContext);
        var topic = await TopicFactory.CreateTopic(user2, dbContext);
        var newPost = await PostFactory.CreatePost(user2, topic, dbContext);

        _client.DefaultRequestHeaders.Add("UserId", user1.Id.ToString());
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "_token", await Utilities.GetCSRFToken(_client) },
            { "content", $"{newPost.Content}_edit" },
        });

        var response = await _client.PutAsync($"/posts/{newPost.Id}", content);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var postRecord = from p in dbContext.Posts
                        where p.Content == $"{newPost.Content}_edit"
                        select p;

        Assert.Null(postRecord.FirstOrDefault());
    }

    [Fact]
    public async Task PostCannotBeUpdatedWithoutContent()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
        dbContext.Database.EnsureDeleted();
        dbContext.Database.Migrate();
        var user = await UserFactory.CreateUser(dbContext);
        var topic = await TopicFactory.CreateTopic(user, dbContext);
        var newPost = await PostFactory.CreatePost(user, topic, dbContext);

        _client.DefaultRequestHeaders.Add("UserId", user.Id.ToString());
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "_token", await Utilities.GetCSRFToken(_client) },
        });

        var response = await _client.PutAsync($"/posts/{newPost.Id}", content);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var postRecord = from p in dbContext.Posts
                        where p.Content == newPost.Content
                        select p;

        Assert.NotNull(postRecord.FirstOrDefault());
    }

    [Fact]
    public async Task PostCannotBeUpdatedByUnauthenticatedUser()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
        dbContext.Database.EnsureDeleted();
        dbContext.Database.Migrate();
        var user = await UserFactory.CreateUser(dbContext);
        var topic = await TopicFactory.CreateTopic(user, dbContext);
        var newPost = await PostFactory.CreatePost(user, topic, dbContext);

        _client.DefaultRequestHeaders.Remove("Authorization");
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "content", $"{newPost.Content}_edit" },
        });

        var response = await _client.PutAsync($"/posts/{newPost.Id}", content);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var postRecord = from p in dbContext.Posts
                        where p.Content == $"{newPost.Content}_edit"
                        select p;

        Assert.Null(postRecord.FirstOrDefault());
    }

    [Fact]
    public async Task UserCannotUpdateNonExistentPost()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
        dbContext.Database.EnsureDeleted();
        dbContext.Database.Migrate();
        var user = await UserFactory.CreateUser(dbContext);

        _client.DefaultRequestHeaders.Add("UserId", user.Id.ToString());
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "_token", await Utilities.GetCSRFToken(_client) },
            { "content", "test_content" },
        });

        var response = await _client.PutAsync("/posts/1", content);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CSRFProtectionIsActive()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
        dbContext.Database.EnsureDeleted();
        dbContext.Database.Migrate();
        var user = await UserFactory.CreateUser(dbContext);
        var topic = await TopicFactory.CreateTopic(user, dbContext);
        var newPost = await PostFactory.CreatePost(user, topic, dbContext);

        _client.DefaultRequestHeaders.Add("UserId", user.Id.ToString());
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "content", $"{newPost.Content}_edit" },
        });

        var response = await _client.PutAsync($"/posts/{newPost.Id}", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var postRecord = from p in dbContext.Posts
                        where p.Content == $"{newPost.Content}_edit"
                        select p;

        Assert.Null(postRecord.FirstOrDefault());
    }

    [Fact]
    public async Task HTTPMethodOverrideCanBeUsed()
    {
        User user;
        Topic topic;
        Post newPost;
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
            dbContext.Database.EnsureDeleted();
            dbContext.Database.Migrate();
            user = await UserFactory.CreateUser(dbContext);
            topic = await TopicFactory.CreateTopic(user, dbContext);
            newPost = await PostFactory.CreatePost(user, topic, dbContext);
        }

        _client.DefaultRequestHeaders.Add("UserId", user.Id.ToString());
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "_method", "PUT" },
            { "_token", await Utilities.GetCSRFToken(_client) },
            { "content", $"{newPost.Content}_edit" },
        });

        DateTime timeBeforeResponse = DateTime.Now;
        var response = await _client.PostAsync($"/posts/{newPost.Id}", content);
        DateTime timeAfterResponse = DateTime.Now;

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
            var postRecord = from p in dbContext.Posts
                            where p.Content == $"{newPost.Content}_edit"
                            select p;

            var post = postRecord.FirstOrDefault();
            Assert.NotNull(post);
            Assert.True(post.UpdatedAt.CompareTo(timeBeforeResponse) >= 0);
            Assert.True(post.UpdatedAt.CompareTo(timeAfterResponse) <= 0);
        }
    }
}
