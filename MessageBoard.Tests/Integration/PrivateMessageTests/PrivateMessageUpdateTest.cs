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
public class PrivateMessageUpdateTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public PrivateMessageUpdateTests(CustomWebApplicationFactory<Program> factory)
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
    public async Task PrivateMessageCanBeUpdated()
    {
        PrivateMessage message;
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
            dbContext.Database.EnsureDeleted();
            dbContext.Database.Migrate();
            message = await DataFactory.CreatePrivateMessage(dbContext);
        }

        _client.DefaultRequestHeaders.Add("UserId", message.Author.Id.ToString());
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "_token", await Utilities.GetCSRFToken(_client) },
            { "title", $"{message.Title}_edit" },
            { "content", $"{message.Content}_edit" },
        });

        DateTime timeBeforeResponse = DateTime.Now;
        var response = await _client.PutAsync($"/messages/{message.Id}", content);
        DateTime timeAfterResponse = DateTime.Now;

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal($"/messages/{message.Id}", response.Headers.Location.OriginalString);
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
            var freshMessage = dbContext.PrivateMessages
                .FirstOrDefault(m => m.Title == $"{message.Title}_edit" &&
                                    m.Content == $"{message.Content}_edit");

            Assert.NotNull(freshMessage);
            Assert.True(freshMessage.UpdatedAt.CompareTo(timeBeforeResponse) >= 0);
            Assert.True(freshMessage.UpdatedAt.CompareTo(timeAfterResponse) <= 0);
        }
    }

    [Fact]
    public async Task PrivateMessageCannotBeUpdatedWithoutTitle()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
        dbContext.Database.EnsureDeleted();
        dbContext.Database.Migrate();
        var message = await DataFactory.CreatePrivateMessage(dbContext);

        _client.DefaultRequestHeaders.Add("UserId", message.Author.Id.ToString());
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "_token", await Utilities.GetCSRFToken(_client) },
            { "content", $"{message.Content}_edit" },
        });

        var response = await _client.PutAsync($"/messages/{message.Id}", content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Null(dbContext.PrivateMessages
            .FirstOrDefault(m => m.Content == $"{message.Content}_edit"));
    }

    [Fact]
    public async Task PrivateMessageCannotBeUpdatedWithoutContent()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
        dbContext.Database.EnsureDeleted();
        dbContext.Database.Migrate();
        var message = await DataFactory.CreatePrivateMessage(dbContext);

        _client.DefaultRequestHeaders.Add("UserId", message.Author.Id.ToString());
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "_token", await Utilities.GetCSRFToken(_client) },
            { "title", $"{message.Title}_edit" },
        });

        var response = await _client.PutAsync($"/messages/{message.Id}", content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Null(dbContext.PrivateMessages
            .FirstOrDefault(m => m.Content == $"{message.Title}_edit"));
    }

    [Fact]
    public async Task PrivateMessageCannotBeUpdatedByUnauthenticatedUser()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
        dbContext.Database.EnsureDeleted();
        dbContext.Database.Migrate();
        var message = await DataFactory.CreatePrivateMessage(dbContext);

        _client.DefaultRequestHeaders.Remove("Authorization");
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "title", $"{message.Title}_edit" },
            { "content", $"{message.Content}_edit" },
        });

        var response = await _client.PutAsync($"/messages/{message.Id}", content);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var freshMessage = dbContext.PrivateMessages
            .FirstOrDefault(m => m.Title == $"{message.Title}_edit" &&
                                m.Content == $"{message.Content}_edit");
    }

    [Fact]
    public async Task PrivateMessageCanOnlyBeUpdatedByItsAuthor()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
        dbContext.Database.EnsureDeleted();
        dbContext.Database.Migrate();
        var user = await DataFactory.CreateUser(dbContext);
        var message = await DataFactory.CreatePrivateMessage(dbContext);

        _client.DefaultRequestHeaders.Add("UserId", user.Id.ToString());
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "_token", await Utilities.GetCSRFToken(_client) },
            { "title", $"{message.Title}_edit" },
            { "content", $"{message.Content}_edit" },
        });

        var response = await _client.PutAsync($"/messages/{message.Id}", content);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Null(dbContext.PrivateMessages
            .FirstOrDefault(m => m.Content == $"{message.Title}_edit"));
    }

    [Fact]
    public async Task UserCannotUpdateNonExistentPrivateMessage()
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

        var response = await _client.PutAsync("/messages/1", content);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CSRFProtectionIsActive()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
        dbContext.Database.EnsureDeleted();
        dbContext.Database.Migrate();
        var message = await DataFactory.CreatePrivateMessage(dbContext);

        _client.DefaultRequestHeaders.Add("UserId", message.Author.Id.ToString());
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "title", $"{message.Title}_edit" },
            { "content", $"{message.Content}_edit" },
        });

        var response = await _client.PutAsync($"/messages/{message.Id}", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Null(dbContext.PrivateMessages
            .FirstOrDefault(m => m.Content == $"{message.Title}_edit"));
    }

    [Fact]
    public async Task HTTPMethodOverrideCanBeUsed()
    {
        PrivateMessage message;
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
            dbContext.Database.EnsureDeleted();
            dbContext.Database.Migrate();
            message = await DataFactory.CreatePrivateMessage(dbContext);
        }

        _client.DefaultRequestHeaders.Add("UserId", message.Author.Id.ToString());
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "_method", "PUT" },
            { "_token", await Utilities.GetCSRFToken(_client) },
            { "title", $"{message.Title}_edit" },
            { "content", $"{message.Content}_edit" },
        });

        DateTime timeBeforeResponse = DateTime.Now;
        var response = await _client.PostAsync($"/messages/{message.Id}", content);
        DateTime timeAfterResponse = DateTime.Now;

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal($"/messages/{message.Id}", response.Headers.Location.OriginalString);
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
            var freshMessage = dbContext.PrivateMessages
                .FirstOrDefault(m => m.Title == $"{message.Title}_edit" &&
                                    m.Content == $"{message.Content}_edit");

            Assert.NotNull(freshMessage);
            Assert.True(freshMessage.UpdatedAt.CompareTo(timeBeforeResponse) >= 0);
            Assert.True(freshMessage.UpdatedAt.CompareTo(timeAfterResponse) <= 0);
        }
    }
}
