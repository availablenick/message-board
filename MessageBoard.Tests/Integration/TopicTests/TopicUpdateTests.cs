using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;

using MessageBoard.Data;
using MessageBoard.Models;

namespace MessageBoard.Tests.Integration.TopicTests;

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
    public async Task TopicCanBeUpdatedByItsAuthor()
    {
        Topic newTopic;
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
            dbContext.Database.EnsureDeleted();
            dbContext.Database.Migrate();
            newTopic = await DataFactory.CreateTopic(dbContext);
        }

        _client.DefaultRequestHeaders.Add("UserId", newTopic.Author.Id.ToString());
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
    public async Task TopicCanBeUpdatedByModerator()
    {
        User user;
        Topic newTopic;
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
            dbContext.Database.EnsureDeleted();
            dbContext.Database.Migrate();
            user = await DataFactory.CreateUser(dbContext);
            newTopic = await DataFactory.CreateTopic(dbContext);
        }

        _client.DefaultRequestHeaders.Add("UserId", user.Id.ToString());
        _client.DefaultRequestHeaders.Add("Role", "Moderator");
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
    public async Task TopicCannotBeUpdatedByUnauthorizedUser()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
        dbContext.Database.EnsureDeleted();
        dbContext.Database.Migrate();
        var user = await DataFactory.CreateUser(dbContext);
        var newTopic = await DataFactory.CreateTopic(dbContext);

        _client.DefaultRequestHeaders.Add("UserId", user.Id.ToString());
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
    public async Task TopicCannotBeUpdatedByUnauthenticatedUser()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
        dbContext.Database.EnsureDeleted();
        dbContext.Database.Migrate();
        var topic = await DataFactory.CreateTopic(dbContext);

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
    public async Task TopicCannotBeUpdatedWithoutTitle()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
        dbContext.Database.EnsureDeleted();
        dbContext.Database.Migrate();
        var newTopic = await DataFactory.CreateTopic(dbContext);

        _client.DefaultRequestHeaders.Add("UserId", newTopic.Author.Id.ToString());
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
        var newTopic = await DataFactory.CreateTopic(dbContext);

        _client.DefaultRequestHeaders.Add("UserId", newTopic.Author.Id.ToString());
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
    public async Task TopicPinnedStatusCanBeUpdated()
    {
        Topic newTopic;
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
            dbContext.Database.EnsureDeleted();
            dbContext.Database.Migrate();
            newTopic = await DataFactory.CreateTopic(dbContext);
        }

        _client.DefaultRequestHeaders.Add("UserId", newTopic.Author.Id.ToString());
        _client.DefaultRequestHeaders.Add("Role", "Moderator");
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "_token", await Utilities.GetCSRFToken(_client) },
            { "isPinned", "true" },
        });

        DateTime timeBeforeResponse = DateTime.Now;
        var response = await _client.PostAsync($"/topics/{newTopic.Id}/pinning", content);
        DateTime timeAfterResponse = DateTime.Now;

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
            var topic = await dbContext.Topics.FindAsync(newTopic.Id);
            Assert.True(topic.IsPinned);
        }
    }

    [Fact]
    public async Task TopicPinnedStatusCannotBeUpdatedByRegularUser()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
        dbContext.Database.EnsureDeleted();
        dbContext.Database.Migrate();
        var newTopic = await DataFactory.CreateTopic(dbContext);

        _client.DefaultRequestHeaders.Add("UserId", newTopic.Author.Id.ToString());
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "_token", await Utilities.GetCSRFToken(_client) },
            { "isPinned", "true" },
        });

        var response = await _client.PostAsync($"/topics/{newTopic.Id}/pinning", content);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var topic = await dbContext.Topics.FindAsync(newTopic.Id);
        Assert.False(topic.IsPinned);
    }

    [Fact]
    public async Task UserCannotUpdatePinnedStatusOfNonExistentTopic()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
        dbContext.Database.EnsureDeleted();
        dbContext.Database.Migrate();
        var user = await DataFactory.CreateUser(dbContext);

        _client.DefaultRequestHeaders.Add("UserId", user.Id.ToString());
        _client.DefaultRequestHeaders.Add("Role", "Moderator");
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "_token", await Utilities.GetCSRFToken(_client) },
            { "isPinned", "true" },
        });

        var response = await _client.PostAsync("/topics/1/pinning", content);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UserCannotUpdateNonExistentTopic()
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
            { "title", "test_title" },
            { "content", "test_content" },
        });

        var response = await _client.PutAsync("/topics/1", content);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
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
        Topic newTopic;
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
            dbContext.Database.EnsureDeleted();
            dbContext.Database.Migrate();
            newTopic = await DataFactory.CreateTopic(dbContext);
        }

        _client.DefaultRequestHeaders.Add("UserId", newTopic.Author.Id.ToString());
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
