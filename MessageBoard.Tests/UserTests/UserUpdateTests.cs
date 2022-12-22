using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;

using MessageBoard.Data;
using MessageBoard.Models;
using MessageBoard.Tests.Factories;

namespace MessageBoard.Tests.UserTests;

[Collection("User")]
public class UserUpdateTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly string _mainProjectPath;
    private readonly CustomWebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public UserUpdateTests(CustomWebApplicationFactory<Program> factory)
    {
        _mainProjectPath = $"{Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.Parent.FullName}/MessageBoard/";
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Test");
    }

    [Fact]
    public async Task UserCanBeUpdated()
    {
        User newUser;
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
            dbContext.Database.EnsureDeleted();
            dbContext.Database.Migrate();
            newUser = await UserFactory.CreateUser(dbContext);
        }

        _client.DefaultRequestHeaders.Add("UserId", newUser.Id.ToString());
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "_token", await Utilities.GetCSRFToken(_client) },
            { "username", $"{newUser.Username}_edit" },
            { "email", $"edit_{newUser.Email}" },
        });

        var timeBeforeResponse = DateTime.Now;
        var response = await _client.PutAsync($"/users/{newUser.Id}", content);
        var timeAfterResponse = DateTime.Now;

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
            var userRecord = from u in dbContext.Users
                            where u.Username == $"{newUser.Username}_edit" &&
                                    u.Email == $"edit_{newUser.Email}"
                            select u;

            var user = userRecord.FirstOrDefault();
            Assert.NotNull(user);
            Assert.True(user.UpdatedAt.CompareTo(timeBeforeResponse) >= 0);
            Assert.True(user.UpdatedAt.CompareTo(timeAfterResponse) <= 0);
        }
    }

    [Fact]
    public async Task UserCannotBeUpdatedWithoutUsername()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
        dbContext.Database.EnsureDeleted();
        dbContext.Database.Migrate();
        var newUser = await UserFactory.CreateUser(dbContext);

        _client.DefaultRequestHeaders.Add("UserId", newUser.Id.ToString());
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "_token", await Utilities.GetCSRFToken(_client) },
            { "email", $"edit_{newUser.Email}" },
        });

        var response = await _client.PutAsync($"/users/{newUser.Id}", content);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var userRecord = from u in dbContext.Users
                        where u.Email == $"edit_{newUser.Email}"
                        select u;

        Assert.Null(userRecord.FirstOrDefault());
    }

    [Fact]
    public async Task UserCannotBeUpdatedWithExistingUsername()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
        dbContext.Database.EnsureDeleted();
        dbContext.Database.Migrate();
        var user1 = await UserFactory.CreateUser(dbContext);
        var user2 = await UserFactory.CreateUser(dbContext);

        _client.DefaultRequestHeaders.Add("UserId", user1.Id.ToString());
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "_token", await Utilities.GetCSRFToken(_client) },
            { "username", user2.Username },
            { "email", $"edit_{user1.Email}" },
        });

        var response = await _client.PutAsync($"/users/{user1.Id}", content);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var userCount = await dbContext.Users.CountAsync(u => u.Username == user2.Username);
        Assert.Equal(1, userCount);
    }

    [Fact]
    public async Task UserCanBeUpdatedWithHisOwnUsername()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
        dbContext.Database.EnsureDeleted();
        dbContext.Database.Migrate();
        var newUser = await UserFactory.CreateUser(dbContext);

        _client.DefaultRequestHeaders.Add("UserId", newUser.Id.ToString());
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "_token", await Utilities.GetCSRFToken(_client) },
            { "username", newUser.Username },
            { "email", $"edit_{newUser.Email}" },
        });

        var response = await _client.PutAsync($"/users/{newUser.Id}", content);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var userRecord = from u in dbContext.Users
                        where u.Username == newUser.Username &&
                            u.Email == $"edit_{newUser.Email}"
                        select u;

        Assert.NotNull(userRecord.FirstOrDefault());
    }

    [Fact]
    public async Task UserCannotBeUpdatedWithoutEmail()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
        dbContext.Database.EnsureDeleted();
        dbContext.Database.Migrate();
        var newUser = await UserFactory.CreateUser(dbContext);

        _client.DefaultRequestHeaders.Add("UserId", newUser.Id.ToString());
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "_token", await Utilities.GetCSRFToken(_client) },
            { "username", $"{newUser.Username}_edit" },
        });

        var response = await _client.PutAsync($"/users/{newUser.Id}", content);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var userRecord = from u in dbContext.Users
                        where u.Username == $"{newUser.Username}_edit"
                        select u;

        Assert.Null(userRecord.FirstOrDefault());
    }

    [Fact]
    public async Task UserCannotBeUpdatedWithExistingEmail()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
        dbContext.Database.EnsureDeleted();
        dbContext.Database.Migrate();
        var user1 = await UserFactory.CreateUser(dbContext);
        var user2 = await UserFactory.CreateUser(dbContext);

        _client.DefaultRequestHeaders.Add("UserId", user1.Id.ToString());
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "_token", await Utilities.GetCSRFToken(_client) },
            { "username", $"{user1.Username}_edit" },
            { "email", user2.Email },
        });

        var response = await _client.PutAsync($"/users/{user1.Id}", content);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var userCount = await dbContext.Users.CountAsync(u => u.Email == user2.Email);
        Assert.Equal(1, userCount);
    }

    [Fact]
    public async Task UserCanBeUpdatedWithHisOwnEmail()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
        dbContext.Database.EnsureDeleted();
        dbContext.Database.Migrate();
        var newUser = await UserFactory.CreateUser(dbContext);

        _client.DefaultRequestHeaders.Add("UserId", newUser.Id.ToString());
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "_token", await Utilities.GetCSRFToken(_client) },
            { "username", $"{newUser.Username}_edit" },
            { "email", newUser.Email },
        });

        var response = await _client.PutAsync($"/users/{newUser.Id}", content);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var userRecord = from u in dbContext.Users
                        where u.Username == $"{newUser.Username}_edit" &&
                            u.Email == newUser.Email
                        select u;

        Assert.NotNull(userRecord.FirstOrDefault());
    }

    [Fact]
    public async Task UserCannotBeUpdatedByUnauthenticatedUser()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
        dbContext.Database.EnsureDeleted();
        dbContext.Database.Migrate();
        var newUser = await UserFactory.CreateUser(dbContext);

        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "username", $"{newUser.Username}_edit" },
            { "email", $"edit_{newUser.Email}" },
        });

        _client.DefaultRequestHeaders.Remove("Authorization");
        var response = await _client.PutAsync($"/users/{newUser.Id}", content);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var userRecord = from u in dbContext.Users
                        where u.Username == $"{newUser.Username}_edit" &&
                                u.Email == $"edit_{newUser.Email}"
                        select u;

        Assert.Null(userRecord.FirstOrDefault());
    }

    [Fact]
    public async Task UserCannotBeUpdatedByAnyoneOtherThanThemselves()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
        dbContext.Database.EnsureDeleted();
        dbContext.Database.Migrate();
        var user1 = await UserFactory.CreateUser(dbContext);
        var user2 = await UserFactory.CreateUser(dbContext);

        _client.DefaultRequestHeaders.Add("UserId", user2.Id.ToString());
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "_token", await Utilities.GetCSRFToken(_client) },
            { "username", $"{user1.Username}_edit" },
            { "email", $"edit_{user1.Email}" },
        });

        var response = await _client.PutAsync($"/users/{user1.Id}", content);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var userRecord = from u in dbContext.Users
                        where u.Username == $"{user1.Username}_edit" &&
                                u.Email == $"edit_{user1.Email}"
                        select u;

        Assert.Null(userRecord.FirstOrDefault());
    }

    [Fact]
    public async Task UserCanBeUpdatedWithJPEGAvatar()
    {
        User newUser;
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
            dbContext.Database.EnsureDeleted();
            dbContext.Database.Migrate();
            newUser = await UserFactory.CreateUser(dbContext);
        }

        _client.DefaultRequestHeaders.Add("UserId", newUser.Id.ToString());
        var csrfToken = await Utilities.GetCSRFToken(_client);
        var content = new MultipartFormDataContent();
        content.Add(new StringContent(csrfToken), "_token");
        content.Add(new StringContent($"{newUser.Username}_edit"), "username");
        content.Add(new StringContent($"edit_{newUser.Email}"), "email");
        HttpResponseMessage response;
        using (var streamContent = new StreamContent(new MemoryStream()))
        {
            streamContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
            content.Add(streamContent, "avatar", "test_file.jpg");
            response = await _client.PutAsync($"/users/{newUser.Id}", content);
        }

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
            var userRecord = from u in dbContext.Users
                            where u.Username == $"{newUser.Username}_edit" &&
                                    u.Email == $"edit_{newUser.Email}"
                            select u;

            var user = userRecord.FirstOrDefault();
            Assert.NotNull(user);
            string avatarPath = $"{_mainProjectPath}{user.Avatar}";
            Assert.True(File.Exists(avatarPath));
        }
    }

    [Fact]
    public async Task UserCanBeCreatedWithPNGAvatar()
    {
        User newUser;
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
            dbContext.Database.EnsureDeleted();
            dbContext.Database.Migrate();
            newUser = await UserFactory.CreateUser(dbContext);
        }

        _client.DefaultRequestHeaders.Add("UserId", newUser.Id.ToString());
        var csrfToken = await Utilities.GetCSRFToken(_client);
        var content = new MultipartFormDataContent();
        content.Add(new StringContent(csrfToken), "_token");
        content.Add(new StringContent($"{newUser.Username}_edit"), "username");
        content.Add(new StringContent($"edit_{newUser.Email}"), "email");
        HttpResponseMessage response;
        using (var streamContent = new StreamContent(new MemoryStream()))
        {
            streamContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            content.Add(streamContent, "avatar", "test_file.png");
            response = await _client.PutAsync($"/users/{newUser.Id}", content);
        }

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
            var userRecord = from u in dbContext.Users
                            where u.Username == $"{newUser.Username}_edit" &&
                                    u.Email == $"edit_{newUser.Email}"
                            select u;

            var user = userRecord.FirstOrDefault();
            Assert.NotNull(user);
            string avatarPath = $"{_mainProjectPath}{user.Avatar}";
            Assert.True(File.Exists(avatarPath));
        }
    }

    [Fact]
    public async Task UserCannotBeCreatedWithAvatarOfWrongMIMEType()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
        dbContext.Database.EnsureDeleted();
        dbContext.Database.Migrate();
        var newUser = await UserFactory.CreateUser(dbContext);

        _client.DefaultRequestHeaders.Add("UserId", newUser.Id.ToString());
        var csrfToken = await Utilities.GetCSRFToken(_client);
        var content = new MultipartFormDataContent();
        content.Add(new StringContent(csrfToken), "_token");
        content.Add(new StringContent($"{newUser.Username}_edit"), "username");
        content.Add(new StringContent($"edit_{newUser.Email}"), "email");
        HttpResponseMessage response;
        using (var streamContent = new StreamContent(new MemoryStream()))
        {
            streamContent.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
            content.Add(streamContent, "avatar", "test_file.txt");
            response = await _client.PutAsync($"/users/{newUser.Id}", content);
        }

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var userRecord = from u in dbContext.Users
                        where u.Username == $"{newUser.Username}_edit" &&
                                u.Email == $"edit_{newUser.Email}"
                        select u;

        var user = userRecord.FirstOrDefault();
        Assert.Null(user);
    }

    [Fact]
    public async Task CSRFProtectionIsActive()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
        dbContext.Database.EnsureDeleted();
        dbContext.Database.Migrate();
        var newUser = await UserFactory.CreateUser(dbContext);

        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "username", $"{newUser.Username}_edit" },
            { "email", $"edit_{newUser.Email}" },
        });

        _client.DefaultRequestHeaders.Add("UserId", newUser.Id.ToString());
        var response = await _client.PutAsync($"/users/{newUser.Id}", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var userRecord = from u in dbContext.Users
                        where u.Username == $"{newUser.Username}_edit" &&
                                u.Email == $"edit_{newUser.Email}"
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
            { "_method", "PUT" },
            { "_token", await Utilities.GetCSRFToken(_client) },
            { "username", $"{newUser.Username}_edit" },
            { "email", $"edit_{newUser.Email}" },
        });

        var response = await _client.PostAsync($"/users/{newUser.Id}", content);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var userRecord = from u in dbContext.Users
                        where u.Username == $"{newUser.Username}_edit" &&
                                u.Email == $"edit_{newUser.Email}"
                        select u;

        Assert.NotNull(userRecord.FirstOrDefault());
    }
}
