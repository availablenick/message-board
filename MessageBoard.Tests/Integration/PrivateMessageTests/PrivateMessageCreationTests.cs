using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

using MessageBoard.Data;
using MessageBoard.Models;

namespace MessageBoard.Tests.Integration.PrivateMessageTests;

[Collection("Sync")]
public class PrivateMessageCreationTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public PrivateMessageCreationTests(CustomWebApplicationFactory<Program> factory)
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
    public async Task PrivateMessageCanBeCreated()
    {
        User user1;
        User user2;
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
            dbContext.Database.EnsureDeleted();
            dbContext.Database.Migrate();
            user1 = await DataFactory.CreateUser(dbContext);
            user2 = await DataFactory.CreateUser(dbContext);
        }

        _client.DefaultRequestHeaders.Add("UserId", user1.Id.ToString());
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "_token", await Utilities.GetCSRFToken(_client) },
            { "title", "test_title" },
            { "content", "test_content" },
            { "usernames[0]", user2.Username },
        });

        DateTime timeBeforeResponse = DateTime.Now;
        var response = await _client.PostAsync("/messages", content);
        DateTime timeAfterResponse = DateTime.Now;

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
            var message = dbContext.PrivateMessages.Include(m => m.Users)
                .FirstOrDefault(m => m.Title == "test_title" && m.Content == "test_content");
            Assert.NotNull(message);
            Assert.True(message.CreatedAt.CompareTo(timeBeforeResponse) >= 0);
            Assert.True(message.CreatedAt.CompareTo(timeAfterResponse) <= 0);
            Assert.True(message.CreatedAt.CompareTo(message.UpdatedAt) == 0);
            Assert.Equal(user1.Id, message.Author.Id);
            Assert.True(message.Users.Exists(u => u.Id == user1.Id));
            Assert.True(message.Users.Exists(u => u.Id == user2.Id));

            var freshUser1 = await dbContext.Users
                .Include(u => u.PrivateMessages)
                .FirstAsync(u => u.Id == user1.Id);

            var freshUser2 = await dbContext.Users
                .Include(u => u.PrivateMessages)
                .FirstAsync(u => u.Id == user2.Id);

            Assert.True(freshUser1.PrivateMessages.Exists(m => m.Id == message.Id));
            Assert.True(freshUser2.PrivateMessages.Exists(m => m.Id == message.Id));
        }
    }

    [Fact]
    public async Task PrivateMessageCannotBeCreatedWithoutTitle()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
        dbContext.Database.EnsureDeleted();
        dbContext.Database.Migrate();
        var user1 = await DataFactory.CreateUser(dbContext);
        var user2 = await DataFactory.CreateUser(dbContext);

        _client.DefaultRequestHeaders.Add("UserId", user1.Id.ToString());
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "_token", await Utilities.GetCSRFToken(_client) },
            { "content", "test_content" },
            { "usernames[0]", user2.Username },
        });

        var response = await _client.PostAsync("/messages", content);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal(0, await dbContext.PrivateMessages.CountAsync());
    }

    [Fact]
    public async Task PrivateMessageCannotBeCreatedWithoutContent()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
        dbContext.Database.EnsureDeleted();
        dbContext.Database.Migrate();
        var user1 = await DataFactory.CreateUser(dbContext);
        var user2 = await DataFactory.CreateUser(dbContext);

        _client.DefaultRequestHeaders.Add("UserId", user1.Id.ToString());
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "_token", await Utilities.GetCSRFToken(_client) },
            { "title", "test_title" },
            { "usernames[0]", user2.Username },
        });

        var response = await _client.PostAsync("/messages", content);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal(0, await dbContext.PrivateMessages.CountAsync());
    }

    [Fact]
    public async Task PrivateMessageCannotBeCreatedWithouUsernames()
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

        var response = await _client.PostAsync("/messages", content);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal(0, await dbContext.PrivateMessages.CountAsync());
    }

    [Fact]
    public async Task PrivateMessageCannotBeCreatedWithoutNonExistentUsernames()
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
            { "usernames[0]", $"{user.Username}2" },
        });

        var response = await _client.PostAsync("/messages", content);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal(0, await dbContext.PrivateMessages.CountAsync());
    }

    [Fact]
    public async Task PrivateMessageCannotBeCreatedByUnauthenticatedUser()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
        dbContext.Database.EnsureDeleted();
        dbContext.Database.Migrate();
        var user = await DataFactory.CreateUser(dbContext);

        _client.DefaultRequestHeaders.Remove("Authorization");
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "_token", await Utilities.GetCSRFToken(_client) },
            { "title", "test_title" },
            { "content", "test_content" },
            { "usernames[0]", user.Username },
        });

        var response = await _client.PostAsync("/messages", content);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(0, await dbContext.PrivateMessages.CountAsync());
    }

    [Fact]
    public async Task CSRFProtectionIsActive()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
        dbContext.Database.EnsureDeleted();
        dbContext.Database.Migrate();
        var user1 = await DataFactory.CreateUser(dbContext);
        var user2 = await DataFactory.CreateUser(dbContext);

        _client.DefaultRequestHeaders.Add("UserId", user1.Id.ToString());
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "title", "test_title" },
            { "content", "test_content" },
            { "usernames[0]", user2.Username },
        });

        var response = await _client.PostAsync("/messages", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, await dbContext.PrivateMessages.CountAsync());
    }
}
