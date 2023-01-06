using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;

using MessageBoard.Data;
using MessageBoard.Models;

namespace MessageBoard.Tests.Integration.BanTests;

[Collection("Sync")]
public class BanUpdateTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public BanUpdateTests(CustomWebApplicationFactory<Program> factory)
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
    public async Task BanCanBeUpdated()
    {
        Ban ban;
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
            dbContext.Database.EnsureDeleted();
            dbContext.Database.Migrate();
            ban = await DataFactory.CreateBan(dbContext);
        }

        _client.DefaultRequestHeaders.Add("UserId", ban.User.Id.ToString());
        _client.DefaultRequestHeaders.Add("Role", "Moderator");
        var expirationTime = DateTime.Now.AddHours(1);
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "_token", await Utilities.GetCSRFToken(_client) },
            { "reason", $"{ban.Reason}_edit" },
            { "expiresAt", expirationTime.ToString() },
        });

        DateTime timeBeforeResponse = DateTime.Now;
        var response = await _client.PutAsync($"/bans/{ban.Id}", content);
        DateTime timeAfterResponse = DateTime.Now;

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
            var freshBan = dbContext.Bans
                .FirstOrDefault(b => b.Reason == $"{ban.Reason}_edit");
            Assert.NotNull(freshBan);
            Assert.Equal(expirationTime.ToString(), freshBan.ExpiresAt.ToString());
            Assert.True(freshBan.UpdatedAt.CompareTo(timeBeforeResponse) >= 0);
            Assert.True(freshBan.UpdatedAt.CompareTo(timeAfterResponse) <= 0);
        }
    }

    [Fact]
    public async Task BanCannotBeUpdatedWithoutReason()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
        dbContext.Database.EnsureDeleted();
        dbContext.Database.Migrate();
        var ban = await DataFactory.CreateBan(dbContext);

        _client.DefaultRequestHeaders.Add("UserId", ban.User.Id.ToString());
        _client.DefaultRequestHeaders.Add("Role", "Moderator");
        var expirationTime = DateTime.Now.AddHours(1);
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "_token", await Utilities.GetCSRFToken(_client) },
            { "expiresAt", expirationTime.ToString() },
        });

        var response = await _client.PutAsync($"/bans/{ban.Id}", content);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var freshBan = await dbContext.Bans.FindAsync(ban.Id);
        Assert.NotEqual(expirationTime.ToString(), freshBan.ExpiresAt.ToString());
    }

    [Fact]
    public async Task BanCannotBeUpdatedWithPastExpirationTime()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
        dbContext.Database.EnsureDeleted();
        dbContext.Database.Migrate();
        var ban = await DataFactory.CreateBan(dbContext);

        _client.DefaultRequestHeaders.Add("UserId", ban.User.Id.ToString());
        _client.DefaultRequestHeaders.Add("Role", "Moderator");
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "_token", await Utilities.GetCSRFToken(_client) },
            { "reason", $"{ban.Reason}_edit" },
            { "expiresAt", DateTime.Now.AddHours(-1).ToString() },
        });

        var response = await _client.PutAsync($"/bans/{ban.Id}", content);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var freshBan = await dbContext.Bans.FindAsync(ban.Id);
        Assert.NotEqual($"{ban.Reason}_edit", freshBan.Reason);
    }

    [Fact]
    public async Task UserCannotUpdateNonExistentBan()
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
            { "expiresAt", DateTime.Now.AddHours(1).ToString() },
        });

        var response = await _client.PutAsync("/bans/1", content);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task BanCannotBeUpdatedByRegularUser()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
        dbContext.Database.EnsureDeleted();
        dbContext.Database.Migrate();
        var ban = await DataFactory.CreateBan(dbContext);

        _client.DefaultRequestHeaders.Add("UserId", ban.User.Id.ToString());
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "_token", await Utilities.GetCSRFToken(_client) },
            { "reason", $"{ban.Reason}_edit" },
            { "expiresAt", DateTime.Now.AddHours(1).ToString() },
        });

        var response = await _client.PutAsync($"/bans/{ban.Id}", content);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var freshBan = dbContext.Bans
                .FirstOrDefault(b => b.Reason == $"{ban.Reason}_edit");
        Assert.Null(freshBan);
    }

    [Fact]
    public async Task CSRFProtectionIsActive()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
        dbContext.Database.EnsureDeleted();
        dbContext.Database.Migrate();
        var ban = await DataFactory.CreateBan(dbContext);

        _client.DefaultRequestHeaders.Add("UserId", ban.User.Id.ToString());
        _client.DefaultRequestHeaders.Add("Role", "Moderator");
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "reason", $"{ban.Reason}_edit" },
            { "expiresAt", DateTime.Now.AddHours(-1).ToString() },
        });

        var response = await _client.PutAsync($"/bans/{ban.Id}", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var freshBan = dbContext.Bans
                .FirstOrDefault(b => b.Reason == $"{ban.Reason}_edit");
        Assert.Null(freshBan);
    }

    [Fact]
    public async Task HTTPMethodOverrideCanBeUsed()
    {
        Ban ban;
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
            dbContext.Database.EnsureDeleted();
            dbContext.Database.Migrate();
            ban = await DataFactory.CreateBan(dbContext);
        }

        _client.DefaultRequestHeaders.Add("UserId", ban.User.Id.ToString());
        _client.DefaultRequestHeaders.Add("Role", "Moderator");
        var expirationTime = DateTime.Now.AddHours(1);
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "_method", "PUT" },
            { "_token", await Utilities.GetCSRFToken(_client) },
            { "reason", $"{ban.Reason}_edit" },
            { "expiresAt", expirationTime.ToString() },
        });

        DateTime timeBeforeResponse = DateTime.Now;
        var response = await _client.PostAsync($"/bans/{ban.Id}", content);
        DateTime timeAfterResponse = DateTime.Now;

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
            var freshBan = dbContext.Bans
                .FirstOrDefault(b => b.Reason == $"{ban.Reason}_edit");
            Assert.NotNull(freshBan);
            Assert.Equal(expirationTime.ToString(), freshBan.ExpiresAt.ToString());
            Assert.True(freshBan.UpdatedAt.CompareTo(timeBeforeResponse) >= 0);
            Assert.True(freshBan.UpdatedAt.CompareTo(timeAfterResponse) <= 0);
        }
    }
}
