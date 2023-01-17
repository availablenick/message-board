using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;

using MessageBoard.Data;
using MessageBoard.Models;

namespace MessageBoard.Tests.Integration.PostTests;

[Collection("Sync")]
public class TopicPostCreationTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public TopicPostCreationTests(CustomWebApplicationFactory<Program> factory)
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
    public async Task PostCanBeCreated()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
        dbContext.Database.EnsureDeleted();
        dbContext.Database.Migrate();
        var topic = await DataFactory.CreateTopic(dbContext);

        _client.DefaultRequestHeaders.Add("UserId", topic.Author.Id.ToString());
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "_token", await Utilities.GetCSRFToken(_client) },
            { "content", "test_content" },
        });

        DateTime timeBeforeResponse = DateTime.Now;
        var response = await _client.PostAsync($"/topics/{topic.Id}/posts", content);
        DateTime timeAfterResponse = DateTime.Now;

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal($"/topics/{topic.Id}", response.Headers.Location.OriginalString);
        var postRecord = from p in dbContext.Posts
                        where p.Content == "test_content"
                        select p;

        var post = postRecord.FirstOrDefault();
        Assert.NotNull(post);
        Assert.Equal(topic.Author.Id, post.Author.Id);
        Assert.Equal(topic.Id, post.Discussion.Id);
        Assert.True(topic.Author.Posts.Exists(p => p.Id == post.Id));
        Assert.True(topic.Posts.Exists(p => p.Id == post.Id));
        Assert.True(post.CreatedAt.CompareTo(timeBeforeResponse) >= 0);
        Assert.True(post.CreatedAt.CompareTo(timeAfterResponse) <= 0);
        Assert.True(post.CreatedAt.CompareTo(post.UpdatedAt) == 0);
    }

    [Fact]
    public async Task PostCannotBeCreatedWithoutContent()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
        dbContext.Database.EnsureDeleted();
        dbContext.Database.Migrate();
        var topic = await DataFactory.CreateTopic(dbContext);

        _client.DefaultRequestHeaders.Add("UserId", topic.Author.Id.ToString());
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "_token", await Utilities.GetCSRFToken(_client) },
        });

        var response = await _client.PostAsync($"/topics/{topic.Id}/posts", content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(0, await dbContext.Posts.CountAsync());
    }

    [Fact]
    public async Task PostCannotBeCreatedInNonExistentTopic()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
        dbContext.Database.EnsureDeleted();
        dbContext.Database.Migrate();
        var user = await DataFactory.CreateUser(dbContext);

        _client.DefaultRequestHeaders.Add("UserId", user.Id.ToString());
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "_token", await Utilities.GetCSRFToken(_client) },
            { "content", "test_content" },
        });

        var response = await _client.PostAsync("/topics/1/posts", content);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(0, await dbContext.Posts.CountAsync());
    }

    [Fact]
    public async Task PostCannotBeCreatedInClosedTopic()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
        dbContext.Database.EnsureDeleted();
        dbContext.Database.Migrate();
        var topic = await DataFactory.CreateTopic(dbContext, isOpen: false);

        _client.DefaultRequestHeaders.Add("UserId", topic.Author.Id.ToString());
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "_token", await Utilities.GetCSRFToken(_client) },
            { "content", "test_content" },
        });

        var response = await _client.PostAsync($"/topics/{topic.Id}/posts", content);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(0, await dbContext.Posts.CountAsync());
    }

    [Fact]
    public async Task PostCannotBeCreatedByUnauthenticatedUser()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
        dbContext.Database.EnsureDeleted();
        dbContext.Database.Migrate();
        var topic = await DataFactory.CreateTopic(dbContext);

        _client.DefaultRequestHeaders.Remove("Authorization");
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "content", "test_content" },
        });

        var response = await _client.PostAsync($"/topics/{topic.Id}/posts", content);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(0, await dbContext.Posts.CountAsync());
    }

    [Fact]
    public async Task CSRFProtectionIsActive()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
        dbContext.Database.EnsureDeleted();
        dbContext.Database.Migrate();
        var topic = await DataFactory.CreateTopic(dbContext);

        _client.DefaultRequestHeaders.Add("UserId", topic.Author.Id.ToString());
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "content", "test_content" },
        });

        var response = await _client.PostAsync($"/topics/{topic.Id}/posts", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, await dbContext.Posts.CountAsync());
    }
}
