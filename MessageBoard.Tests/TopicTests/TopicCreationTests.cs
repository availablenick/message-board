using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;

using MessageBoard.Data;
using MessageBoard.Models;
using MessageBoard.Tests.Factories;

namespace MessageBoard.Tests.TopicTests;

[Collection("Sync")]
public class TopicCreationTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly string _mainProjectPath;
    private readonly CustomWebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public TopicCreationTests(CustomWebApplicationFactory<Program> factory)
    {
        _mainProjectPath = $"{Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.Parent.FullName}/MessageBoard/";
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Test");
    }

    [Fact]
    public async Task TopicCanBeCreated()
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
            { "title", "test_title" },
            { "content", "test_content" },
        });

        DateTime timeBeforeResponse = DateTime.Now;
        var response = await _client.PostAsync("/topics", content);
        DateTime timeAfterResponse = DateTime.Now;

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var topicRecord = from t in dbContext.Topics
                        where t.Title == "test_title" &&
                                t.Content == "test_content"
                        select t;

        var topic = topicRecord.FirstOrDefault();
        Assert.NotNull(topic);
        Assert.Equal(user.Id, topic.Author.Id);
        Assert.True(user.Topics.Exists(t => t.Id == topic.Id));
        Assert.True(topic.CreatedAt.CompareTo(timeBeforeResponse) >= 0);
        Assert.True(topic.CreatedAt.CompareTo(timeAfterResponse) <= 0);
        Assert.True(topic.CreatedAt.CompareTo(topic.UpdatedAt) == 0);
    }

    [Fact]
    public async Task TopicCannotBeCreatedWithoutTitle()
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

        var response = await _client.PostAsync("/topics", content);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal(0, await dbContext.Topics.CountAsync());
    }

    [Fact]
    public async Task TopicCannotBeCreatedWithoutContent()
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
            { "title", "test_title" },
        });

        var response = await _client.PostAsync("/topics", content);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal(0, await dbContext.Topics.CountAsync());
    }

    [Fact]
    public async Task TopicCannotBeCreatedByUnauthenticatedUser()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
        dbContext.Database.EnsureDeleted();
        dbContext.Database.Migrate();
        var user = await UserFactory.CreateUser(dbContext);

        _client.DefaultRequestHeaders.Remove("Authorization");
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "title", "test_title" },
            { "content", "test_content" },
        });

        var response = await _client.PostAsync("/topics", content);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var topicRecord = from t in dbContext.Topics
                        where t.Title == "test_title" &&
                                t.Content == "test_content"
                        select t;

        Assert.Null(topicRecord.FirstOrDefault());
    }

    [Fact]
    public async Task CSRFProtectionIsActive()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
        dbContext.Database.EnsureDeleted();
        dbContext.Database.Migrate();
        var user = await UserFactory.CreateUser(dbContext);

        _client.DefaultRequestHeaders.Add("UserId", user.Id.ToString());
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "title", "test_title" },
            { "content", "test_content" },
        });

        var response = await _client.PostAsync("/topics", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var topicRecord = from t in dbContext.Topics
                        where t.Title == "test_title" &&
                                t.Content == "test_content"
                        select t;

        Assert.Null(topicRecord.FirstOrDefault());
    }
}
