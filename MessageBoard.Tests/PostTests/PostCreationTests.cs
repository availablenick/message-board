using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;

using MessageBoard.Data;
using MessageBoard.Models;

namespace MessageBoard.Tests.PostTests;

[Collection("Sync")]
public class PostCreationTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public PostCreationTests(CustomWebApplicationFactory<Program> factory)
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

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var postRecord = from p in dbContext.Posts
                        where p.Content == "test_content"
                        select p;

        var post = postRecord.FirstOrDefault();
        Assert.NotNull(post);
        Assert.Equal(topic.Author.Id, post.Author.Id);
        Assert.Equal(topic.Id, post.Topic.Id);
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

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var postRecord = from p in dbContext.Posts
                        where p.Content == "test_content"
                        select p;

        Assert.Null(postRecord.FirstOrDefault());
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
        var postRecord = from p in dbContext.Posts
                        where p.Content == "test_content"
                        select p;

        Assert.Null(postRecord.FirstOrDefault());
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
        var postRecord = from p in dbContext.Posts
                        where p.Content == "test_content"
                        select p;

        Assert.Null(postRecord.FirstOrDefault());
    }
}
