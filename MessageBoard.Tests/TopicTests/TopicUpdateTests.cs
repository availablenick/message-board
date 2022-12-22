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
public class TopicUpdateTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public TopicUpdateTests(CustomWebApplicationFactory<Program> factory)
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
    public async Task TopicCanBeUpdated()
    {
        User user;
        Topic newTopic;
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
            dbContext.Database.EnsureDeleted();
            dbContext.Database.Migrate();
            user = await UserFactory.CreateUser(dbContext);
            newTopic = await TopicFactory.CreateTopic(user, dbContext);
        }

        _client.DefaultRequestHeaders.Add("UserId", user.Id.ToString());
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "_token", await Utilities.GetCSRFToken(_client) },
            { "title", $"{newTopic.Title}_edit" },
            { "content", $"{newTopic.Content}_edit" },
        });

        DateTime timeBeforeResponse = DateTime.Now;
        var response = await _client.PutAsync($"/topics/{newTopic.Id}", content);
        DateTime timeAfterResponse = DateTime.Now;

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
            var topicRecord = from t in dbContext.Topics
                            where t.Title == $"{newTopic.Title}_edit" &&
                                    t.Content == $"{newTopic.Content}_edit"
                            select t;

            var topic = topicRecord.FirstOrDefault();
            Assert.NotNull(topic);
            Assert.True(topic.UpdatedAt.CompareTo(timeBeforeResponse) >= 0);
            Assert.True(topic.UpdatedAt.CompareTo(timeAfterResponse) <= 0);
        }
    }

    [Fact]
    public async Task TopicCannotBeUpdatedWithoutTitle()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
        dbContext.Database.EnsureDeleted();
        dbContext.Database.Migrate();
        var user = await UserFactory.CreateUser(dbContext);
        var newTopic = await TopicFactory.CreateTopic(user, dbContext);

        _client.DefaultRequestHeaders.Add("UserId", user.Id.ToString());
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "_token", await Utilities.GetCSRFToken(_client) },
            { "content", $"{newTopic.Content}_edit" },
        });

        var response = await _client.PutAsync($"/topics/{newTopic.Id}", content);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var topicRecord = from t in dbContext.Topics
                        where t.Title == $"{newTopic.Title}_edit" &&
                                t.Content == $"{newTopic.Content}_edit"
                        select t;

        Assert.Null(topicRecord.FirstOrDefault());
    }

    [Fact]
    public async Task TopicCannotBeUpdatedWithoutContent()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
        dbContext.Database.EnsureDeleted();
        dbContext.Database.Migrate();
        var user = await UserFactory.CreateUser(dbContext);
        var newTopic = await TopicFactory.CreateTopic(user, dbContext);

        _client.DefaultRequestHeaders.Add("UserId", user.Id.ToString());
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "_token", await Utilities.GetCSRFToken(_client) },
            { "title", $"{newTopic.Title}_edit" },
        });

        var response = await _client.PutAsync($"/topics/{newTopic.Id}", content);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var topicRecord = from t in dbContext.Topics
                        where t.Title == $"{newTopic.Title}_edit" &&
                                t.Content == $"{newTopic.Content}_edit"
                        select t;

        Assert.Null(topicRecord.FirstOrDefault());
    }

    [Fact]
    public async Task TopicCanOnlyBeUpdatedByItsAuthor()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
        dbContext.Database.EnsureDeleted();
        dbContext.Database.Migrate();
        var user1 = await UserFactory.CreateUser(dbContext);
        var user2 = await UserFactory.CreateUser(dbContext);
        var newTopic = await TopicFactory.CreateTopic(user2, dbContext);

        _client.DefaultRequestHeaders.Add("UserId", user1.Id.ToString());
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "_token", await Utilities.GetCSRFToken(_client) },
            { "title", $"{newTopic.Title}_edit" },
            { "content", $"{newTopic.Content}_edit" },
        });

        var response = await _client.PutAsync($"/topics/{newTopic.Id}", content);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var topicRecord = from t in dbContext.Topics
                        where t.Title == $"{newTopic.Title}_edit" &&
                                t.Content == $"{newTopic.Content}_edit"
                        select t;

        Assert.Null(topicRecord.FirstOrDefault());
    }

    [Fact]
    public async Task TopicCannotBeCreatedByUnauthenticatedUser()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
        dbContext.Database.EnsureDeleted();
        dbContext.Database.Migrate();
        var user = await UserFactory.CreateUser(dbContext);
        var topic = await TopicFactory.CreateTopic(user, dbContext);

        _client.DefaultRequestHeaders.Remove("Authorization");
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "title", $"{topic.Title}_edit" },
            { "content", $"{topic.Content}_edit" },
        });

        var response = await _client.PutAsync($"/topics/{topic.Id}", content);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var topicRecord = from t in dbContext.Topics
                        where t.Title == $"{topic.Title}_edit" &&
                                t.Content == $"{topic.Content}_edit"
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
        var topic = await TopicFactory.CreateTopic(user, dbContext);

        _client.DefaultRequestHeaders.Add("UserId", user.Id.ToString());
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "title", $"{topic.Title}_edit" },
            { "content", $"{topic.Content}_edit" },
        });

        var response = await _client.PutAsync($"/topics/{topic.Id}", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var topicRecord = from t in dbContext.Topics
                        where t.Title == $"{topic.Title}_edit" &&
                                t.Content == $"{topic.Content}_edit"
                        select t;

        Assert.Null(topicRecord.FirstOrDefault());
    }

    [Fact]
    public async Task HTTPMethodOverrideCanBeUsed()
    {
        User user;
        Topic newTopic;
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
            dbContext.Database.EnsureDeleted();
            dbContext.Database.Migrate();
            user = await UserFactory.CreateUser(dbContext);
            newTopic = await TopicFactory.CreateTopic(user, dbContext);
        }

        _client.DefaultRequestHeaders.Add("UserId", user.Id.ToString());
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "_method", "PUT" },
            { "_token", await Utilities.GetCSRFToken(_client) },
            { "title", $"{newTopic.Title}_edit" },
            { "content", $"{newTopic.Content}_edit" },
        });

        DateTime timeBeforeResponse = DateTime.Now;
        var response = await _client.PostAsync($"/topics/{newTopic.Id}", content);
        DateTime timeAfterResponse = DateTime.Now;

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
            var topicRecord = from t in dbContext.Topics
                            where t.Title == $"{newTopic.Title}_edit" &&
                                    t.Content == $"{newTopic.Content}_edit"
                            select t;

            var topic = topicRecord.FirstOrDefault();
            Assert.NotNull(topic);
            Assert.True(topic.UpdatedAt.CompareTo(timeBeforeResponse) >= 0);
            Assert.True(topic.UpdatedAt.CompareTo(timeAfterResponse) <= 0);
        }
    }
}
