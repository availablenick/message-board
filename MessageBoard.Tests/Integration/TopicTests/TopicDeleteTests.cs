using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;

using MessageBoard.Data;
using MessageBoard.Models;

namespace MessageBoard.Tests.Integration.TopicTests;

[Collection("Sync")]
public class TopicDeleteTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public TopicDeleteTests(CustomWebApplicationFactory<Program> factory)
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
    public async Task TopicCanBeDeleted()
    {
        Topic topic;
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
            dbContext.Database.EnsureDeleted();
            dbContext.Database.Migrate();
            topic = await DataFactory.CreateTopic(dbContext);
        }

        _client.DefaultRequestHeaders.Add("UserId", topic.Author.Id.ToString());
        _client.DefaultRequestHeaders.Add("X-XSRF-TOKEN", await Utilities.GetCSRFToken(_client));
        var response = await _client.DeleteAsync($"/topics/{topic.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
            var topicRecord = from t in dbContext.Topics
                            where t.Id == topic.Id
                            select t;

            Assert.Null(topicRecord.FirstOrDefault());
            var freshUser = await dbContext.Users.Include(u => u.Topics)
                .FirstAsync(u => u.Id == topic.Author.Id);

            var freshSection = await dbContext.Sections.Include(s => s.Topics)
                .FirstAsync(s => s.Id == topic.Section.Id);

            Assert.Empty(freshUser.Topics);
            Assert.Empty(freshSection.Topics);
        }
    }

    [Fact]
    public async Task TopicCanOnlyBeDeletedByItsAuthor()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
        dbContext.Database.EnsureDeleted();
        dbContext.Database.Migrate();
        var user = await DataFactory.CreateUser(dbContext);
        var topic = await DataFactory.CreateTopic(dbContext);

        _client.DefaultRequestHeaders.Add("UserId", user.Id.ToString());
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
        var topic = await DataFactory.CreateTopic(dbContext);

        _client.DefaultRequestHeaders.Remove("Authorization");
        var response = await _client.DeleteAsync($"/topics/{topic.Id}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var topicRecord = from t in dbContext.Topics
                        where t.Id == topic.Id
                        select t;

        Assert.NotNull(topicRecord.FirstOrDefault());
    }

    [Fact]
    public async Task UserCannotDeleteNonExistentTopic()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
        dbContext.Database.EnsureDeleted();
        dbContext.Database.Migrate();
        var user = await DataFactory.CreateUser(dbContext);

        _client.DefaultRequestHeaders.Add("UserId", user.Id.ToString());
        _client.DefaultRequestHeaders.Add("X-XSRF-TOKEN", await Utilities.GetCSRFToken(_client));
        var response = await _client.DeleteAsync("/topics/1");

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
        Topic topic;
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
            dbContext.Database.EnsureDeleted();
            dbContext.Database.Migrate();
            topic = await DataFactory.CreateTopic(dbContext);
        }

        _client.DefaultRequestHeaders.Add("UserId", topic.Author.Id.ToString());
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "_method", "DELETE" },
            { "_token", await Utilities.GetCSRFToken(_client) },
        });

        var response = await _client.PostAsync($"/topics/{topic.Id}", content);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
            var topicRecord = from t in dbContext.Topics
                            where t.Id == topic.Id
                            select t;

            Assert.Null(topicRecord.FirstOrDefault());
            var freshUser = await dbContext.Users.Include(u => u.Topics)
                .FirstAsync(u => u.Id == topic.Author.Id);

            var freshSection = await dbContext.Sections.Include(s => s.Topics)
                .FirstAsync(s => s.Id == topic.Section.Id);

            Assert.Empty(freshUser.Topics);
            Assert.Empty(freshSection.Topics);
        }
    }
}
