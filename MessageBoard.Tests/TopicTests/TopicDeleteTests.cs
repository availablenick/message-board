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
public class TopicDeleteTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly string _mainProjectPath;
    private readonly CustomWebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public TopicDeleteTests(CustomWebApplicationFactory<Program> factory)
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
    public async Task TopicCanBeDeleted()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
        dbContext.Database.EnsureDeleted();
        dbContext.Database.Migrate();
        var user = await UserFactory.CreateUser(dbContext);
        var topic = await TopicFactory.CreateTopic(user, dbContext);

        _client.DefaultRequestHeaders.Add("UserId", user.Id.ToString());
        _client.DefaultRequestHeaders.Add("X-XSRF-TOKEN", await Utilities.GetCSRFToken(_client));
        var response = await _client.DeleteAsync($"/topics/{topic.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var topicRecord = from t in dbContext.Topics
                        where t.Id == topic.Id
                        select t;

        Assert.Null(topicRecord.FirstOrDefault());
    }

    [Fact]
    public async Task TopicCanOnlyBeDeletedByItsAuthor()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
        dbContext.Database.EnsureDeleted();
        dbContext.Database.Migrate();
        var user1 = await UserFactory.CreateUser(dbContext);
        var user2 = await UserFactory.CreateUser(dbContext);
        var topic = await TopicFactory.CreateTopic(user2, dbContext);

        _client.DefaultRequestHeaders.Add("UserId", user1.Id.ToString());
        _client.DefaultRequestHeaders.Add("X-XSRF-TOKEN", await Utilities.GetCSRFToken(_client));
        var response = await _client.DeleteAsync($"/topics/{topic.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var topicRecord = from t in dbContext.Topics
                        where t.Id == topic.Id
                        select t;

        Assert.NotNull(topicRecord.FirstOrDefault());
    }

    [Fact]
    public async Task TopicCannotBeDeletedByUnauthenticatedUser()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
        dbContext.Database.EnsureDeleted();
        dbContext.Database.Migrate();
        var user = await UserFactory.CreateUser(dbContext);
        var topic = await TopicFactory.CreateTopic(user, dbContext);

        _client.DefaultRequestHeaders.Remove("Authorization");
        var response = await _client.DeleteAsync($"/topics/{topic.Id}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var topicRecord = from t in dbContext.Topics
                        where t.Id == topic.Id
                        select t;

        Assert.NotNull(topicRecord.FirstOrDefault());
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

        _client.DefaultRequestHeaders.Add("UserId", user.Id.ToString());
        var response = await _client.DeleteAsync($"/topics/{topic.Id}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var topicRecord = from t in dbContext.Topics
                        where t.Id == topic.Id
                        select t;

        Assert.NotNull(topicRecord.FirstOrDefault());
    }

    [Fact]
    public async Task HTTPMethodOverrideCanBeUsed()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
        dbContext.Database.EnsureDeleted();
        dbContext.Database.Migrate();
        var user = await UserFactory.CreateUser(dbContext);
        var topic = await TopicFactory.CreateTopic(user, dbContext);

        _client.DefaultRequestHeaders.Add("UserId", user.Id.ToString());
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "_method", "DELETE" },
            { "_token", await Utilities.GetCSRFToken(_client) },
        });

        var response = await _client.PostAsync($"/topics/{topic.Id}", content);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var topicRecord = from t in dbContext.Topics
                        where t.Id == topic.Id
                        select t;

        Assert.Null(topicRecord.FirstOrDefault());
    }
}