using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;

using MessageBoard.Data;
using MessageBoard.Models;
using MessageBoard.Tests.Factories;

namespace MessageBoard.Tests.UserTests;

[Collection("Sync")]
public class UserDeleteTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public UserDeleteTests(CustomWebApplicationFactory<Program> factory)
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
    public async Task UserCanBeDeleted()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
        dbContext.Database.EnsureDeleted();
        dbContext.Database.Migrate();
        var newUser = await UserFactory.CreateUser(dbContext);

        _client.DefaultRequestHeaders.Add("UserId", newUser.Id.ToString());
        _client.DefaultRequestHeaders.Add("X-XSRF-TOKEN", await Utilities.GetCSRFToken(_client));
        var response = await _client.DeleteAsync($"/users/{newUser.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var userRecord = from u in dbContext.Users
                        where u.Username == newUser.Username &&
                                u.Email == newUser.Email &&
                                u.IsDeleted == true
                        select u;

        Assert.NotNull(userRecord.FirstOrDefault());
    }

    [Fact]
    public async Task UserCannotBeDeletedByUnauthenticatedUser()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
        dbContext.Database.EnsureDeleted();
        dbContext.Database.Migrate();
        var newUser = await UserFactory.CreateUser(dbContext);

        _client.DefaultRequestHeaders.Remove("Authorization");
        var response = await _client.DeleteAsync($"/users/{newUser.Id}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var userRecord = from u in dbContext.Users
                        where u.Username == newUser.Username &&
                                u.Email == newUser.Email &&
                                u.IsDeleted == true
                        select u;

        Assert.Null(userRecord.FirstOrDefault());
    }

    [Fact]
    public async Task UserCannotBeDeletedByAnyoneOtherThanThemselves()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
        dbContext.Database.EnsureDeleted();
        dbContext.Database.Migrate();
        var user1 = await UserFactory.CreateUser(dbContext);
        var user2 = await UserFactory.CreateUser(dbContext);

        _client.DefaultRequestHeaders.Add("UserId", user1.Id.ToString());
        _client.DefaultRequestHeaders.Add("X-XSRF-TOKEN", await Utilities.GetCSRFToken(_client));
        var response = await _client.DeleteAsync($"/users/{user2.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var userRecord = from u in dbContext.Users
                        where u.Username == user2.Username &&
                                u.Email == user2.Email &&
                                u.IsDeleted == true
                        select u;

        Assert.Null(userRecord.FirstOrDefault());
    }

    [Fact]
    public async Task CSRFProtectionIsActive()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
        dbContext.Database.EnsureDeleted();
        dbContext.Database.Migrate();
        var newUser = await UserFactory.CreateUser(dbContext);

        _client.DefaultRequestHeaders.Add("UserId", newUser.Id.ToString());
        var response = await _client.DeleteAsync($"/users/{newUser.Id}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var userRecord = from u in dbContext.Users
                        where u.Username == newUser.Username &&
                                u.Email == newUser.Email &&
                                u.IsDeleted == true
                        select u;

        Assert.Null(userRecord.FirstOrDefault());
    }

    [Fact]
    public async Task HTTPMethodOverrideCanBeUsed()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
        dbContext.Database.EnsureDeleted();
        dbContext.Database.Migrate();
        var newUser = await UserFactory.CreateUser(dbContext);

        _client.DefaultRequestHeaders.Add("UserId", newUser.Id.ToString());
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "_method", "DELETE" },
            { "_token", await Utilities.GetCSRFToken(_client) },
        });

        var response = await _client.PostAsync($"/users/{newUser.Id}", content);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var userRecord = from u in dbContext.Users
                        where u.Username == newUser.Username &&
                                u.Email == newUser.Email &&
                                u.IsDeleted == true
                        select u;

        Assert.NotNull(userRecord.FirstOrDefault());
    }
}
