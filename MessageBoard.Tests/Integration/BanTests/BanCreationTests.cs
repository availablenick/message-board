using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;

using MessageBoard.Data;
using MessageBoard.Models;

namespace MessageBoard.Tests.Integration.BanTests;

[Collection("Sync")]
public class BanCreationTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public BanCreationTests(CustomWebApplicationFactory<Program> factory)
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
    public async Task BanCanBeCreated()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
        dbContext.Database.EnsureDeleted();
        dbContext.Database.Migrate();
        var user = await DataFactory.CreateUser(dbContext);

        _client.DefaultRequestHeaders.Add("UserId", user.Id.ToString());
        _client.DefaultRequestHeaders.Add("Role", "Moderator");
        var expirationTime = DateTime.Now.AddHours(1);
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "_token", await Utilities.GetCSRFToken(_client) },
            { "reason", "test_reason" },
            { "expiresAt", expirationTime.ToString() },
            { "username", user.Username },
        });

        DateTime timeBeforeResponse = DateTime.Now;
        var response = await _client.PostAsync("/bans", content);
        DateTime timeAfterResponse = DateTime.Now;

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/users", response.Headers.Location.OriginalString);
        var ban = dbContext.Bans.FirstOrDefault(b => b.Reason == "test_reason");
        Assert.NotNull(ban);
        Assert.Equal(user.Id, ban.User.Id);
        Assert.Equal(ban.Id, user.Ban.Id);
        Assert.Equal(expirationTime.ToString(), ban.ExpiresAt.ToString());
        Assert.True(ban.CreatedAt.CompareTo(timeBeforeResponse) >= 0);
        Assert.True(ban.CreatedAt.CompareTo(timeAfterResponse) <= 0);
        Assert.True(ban.CreatedAt.CompareTo(ban.UpdatedAt) == 0);
    }

    [Fact]
    public async Task BanCannotBeCreatedWithoutReason()
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
            { "expiresAt", DateTime.Now.AddHours(1).ToString() },
            { "username", user.Username },
        });

        var response = await _client.PostAsync("/bans", content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(0, await dbContext.Bans.CountAsync());
    }

    [Fact]
    public async Task BanCannotBeCreatedWithPastExpirationTime()
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
            { "reason", "test_reason" },
            { "expiresAt", DateTime.Now.AddHours(-1).ToString() },
            { "username", user.Username },
        });

        var response = await _client.PostAsync("/bans", content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(0, await dbContext.Bans.CountAsync());
    }

    [Fact]
    public async Task BanCannotBeCreatedIfUserHasActiveBan()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
        dbContext.Database.EnsureDeleted();
        dbContext.Database.Migrate();
        var user = await DataFactory.CreateUser(dbContext);
        var ban = await DataFactory.CreateBan(dbContext, null, DateTime.Now.AddHours(3));

        _client.DefaultRequestHeaders.Add("UserId", user.Id.ToString());
        _client.DefaultRequestHeaders.Add("Role", "Moderator");
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "_token", await Utilities.GetCSRFToken(_client) },
            { "reason", "test_reason" },
            { "expiresAt", DateTime.Now.AddHours(2).ToString() },
            { "username", ban.User.Username },
        });

        var response = await _client.PostAsync("/bans", content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, await dbContext.Bans.CountAsync());
    }

    [Fact]
    public async Task BanCanBeCreatedIfUserHasExpiredBan()
    {
        User user;
        Ban ban;
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
            dbContext.Database.EnsureDeleted();
            dbContext.Database.Migrate();
            user = await DataFactory.CreateUser(dbContext);
            ban = await DataFactory.CreateBan(dbContext, null, DateTime.Now.AddHours(-1));
        }

        _client.DefaultRequestHeaders.Add("UserId", user.Id.ToString());
        _client.DefaultRequestHeaders.Add("Role", "Moderator");
        var expirationTime = DateTime.Now.AddHours(2);
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "_token", await Utilities.GetCSRFToken(_client) },
            { "reason", $"{ban.Reason}_2" },
            { "expiresAt", expirationTime.ToString() },
            { "username", ban.User.Username },
        });

        var response = await _client.PostAsync("/bans", content);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/users", response.Headers.Location.OriginalString);
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
            Assert.Equal(1, await dbContext.Bans.CountAsync());
            var newBan = dbContext.Bans
                .FirstOrDefault(b => b.Reason == $"{ban.Reason}_2");

            Assert.NotNull(newBan);
            Assert.Equal(expirationTime.ToString(), newBan.ExpiresAt.ToString());
        }
    }

    [Fact]
    public async Task BanCannotBeCreatedForNonExistentUser()
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
            { "reason", "test_reason" },
            { "expiresAt", DateTime.Now.AddHours(-1).ToString() },
            { "username", $"{user.Username}2" },
        });

        var response = await _client.PostAsync("/bans", content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(0, await dbContext.Bans.CountAsync());
    }

    [Fact]
    public async Task BanCannotBeCreatedByRegularUser()
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
            { "reason", "test_reason" },
            { "expiresAt", DateTime.Now.AddHours(-1).ToString() },
            { "username", user.Username },
        });

        var response = await _client.PostAsync("/bans", content);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(0, await dbContext.Bans.CountAsync());
    }

    [Fact]
    public async Task CSRFProtectionIsActive()
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
            { "reason", "test_reason" },
            { "expiresAt", DateTime.Now.AddHours(-1).ToString() },
            { "username", user.Username },
        });

        var response = await _client.PostAsync("/bans", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, await dbContext.Bans.CountAsync());
    }
}
