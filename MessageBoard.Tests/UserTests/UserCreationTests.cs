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

[Collection("User")]
public class UserCreationTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly string _mainProjectPath;
    private readonly CustomWebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public UserCreationTests(CustomWebApplicationFactory<Program> factory)
    {
        _mainProjectPath = $"{Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.Parent.FullName}/MessageBoard/";
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task UserCanBeCreated()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
        dbContext.Database.EnsureDeleted();
        dbContext.Database.Migrate();

        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "username", "test_username" },
            { "email", "test@test.com" },
            { "password", "test_password" },
        });

        var response = await _client.PostAsync("/users", content);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var userRecord = from u in dbContext.Users
                        where u.Username == "test_username" &&
                                u.Email == "test@test.com"
                        select u;

        var user = userRecord.FirstOrDefault();
        Assert.NotNull(user);
        var passwordHasher = new PasswordHasher<User>();
        PasswordVerificationResult res = passwordHasher.VerifyHashedPassword(
            user, user.PasswordHash, "test_password");
        Assert.Equal(PasswordVerificationResult.Success, res);
    }

    [Fact]
    public async Task UserCannotBeCreatedWithoutUsername()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
        dbContext.Database.EnsureDeleted();
        dbContext.Database.Migrate();

        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "email", "test@test.com" },
            { "password", "test_password" },
        });

        var response = await _client.PostAsync("/users", content);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal(0, await dbContext.Users.CountAsync());
    }

    [Fact]
    public async Task UserCannotBeCreatedWithExistingUsername()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
        dbContext.Database.EnsureDeleted();
        dbContext.Database.Migrate();
        var newUser = await UserFactory.CreateUser(dbContext);

        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "username", newUser.Username },
            { "email", $"2{newUser.Email}" },
            { "password", "test_password" },
        });

        var response = await _client.PostAsync("/users", content);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal(1, await dbContext.Users.CountAsync());
    }

    [Fact]
    public async Task UserCannotBeCreatedWithoutEmail()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
        dbContext.Database.EnsureDeleted();
        dbContext.Database.Migrate();

        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "username", "test_username" },
            { "password", "test_password" },
        });

        var response = await _client.PostAsync("/users", content);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal(0, await dbContext.Users.CountAsync());
    }

    [Fact]
    public async Task UserCannotBeCreatedWithInvalidEmail()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
        dbContext.Database.EnsureDeleted();
        dbContext.Database.Migrate();

        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "username", "test_username" },
            { "email", "test_email" },
            { "password", "test_password" },
        });

        var response = await _client.PostAsync("/users", content);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal(0, await dbContext.Users.CountAsync());
    }

    [Fact]
    public async Task UserCannotBeCreatedWithExistingEmail()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
        dbContext.Database.EnsureDeleted();
        dbContext.Database.Migrate();
        var newUser = await UserFactory.CreateUser(dbContext);

        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "username", $"{newUser.Username}2" },
            { "email", newUser.Email },
            { "password", "test_password" },
        });

        var response = await _client.PostAsync("/users", content);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal(1, await dbContext.Users.CountAsync());
    }

    [Fact]
    public async Task UserCannotBeCreatedWithoutPassword()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
        dbContext.Database.EnsureDeleted();
        dbContext.Database.Migrate();

        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "username", "test_username" },
            { "email", "test@test.com" },
        });

        var response = await _client.PostAsync("/users", content);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal(0, await dbContext.Users.CountAsync());
    }

    [Fact]
    public async Task UserCannotBeCreatedByAuthenticatedUser()
    {
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Test");

        var response = await _client.PostAsync("/users", null);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/", response.Headers.Location.OriginalString);
    }

    [Fact]
    public async Task UserCanBeCreatedWithJPEGAvatar()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
        dbContext.Database.EnsureDeleted();
        dbContext.Database.Migrate();

        var multipartFormDataContent = new MultipartFormDataContent();
        multipartFormDataContent.Add(new StringContent("test_username"), "username");
        multipartFormDataContent.Add(new StringContent("test@test.com"), "email");
        multipartFormDataContent.Add(new StringContent("test_password"), "password");

        HttpResponseMessage response;
        using (var streamContent = new StreamContent(new MemoryStream()))
        {
            streamContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
            multipartFormDataContent.Add(streamContent, "avatar", "test_file.jpg");
            response = await _client.PostAsync("/users", multipartFormDataContent);
        }

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var userRecord = from u in dbContext.Users
                        where u.Username == "test_username" &&
                            u.Email == "test@test.com"
                        select u;

        var user = userRecord.FirstOrDefault();
        Assert.NotNull(user);
        string avatarPath = $"{_mainProjectPath}{user.Avatar}";
        Assert.True(File.Exists(avatarPath));
    }

    [Fact]
    public async Task UserCanBeCreatedWithPNGAvatar()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
        dbContext.Database.EnsureDeleted();
        dbContext.Database.Migrate();

        var multipartFormDataContent = new MultipartFormDataContent();
        multipartFormDataContent.Add(new StringContent("test_username"), "username");
        multipartFormDataContent.Add(new StringContent("test@test.com"), "email");
        multipartFormDataContent.Add(new StringContent("test_password"), "password");

        HttpResponseMessage response;
        using (var streamContent = new StreamContent(new MemoryStream()))
        {
            streamContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            multipartFormDataContent.Add(streamContent, "avatar", "test_file.png");
            response = await _client.PostAsync("/users", multipartFormDataContent);
        }

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var userRecord = from u in dbContext.Users
                        where u.Username == "test_username" &&
                            u.Email == "test@test.com"
                        select u;

        var user = userRecord.FirstOrDefault();
        Assert.NotNull(user);
        string avatarPath = $"{_mainProjectPath}{user.Avatar}";
        Assert.True(File.Exists(avatarPath));
    }

    [Fact]
    public async Task UserCannotBeCreatedWithAvatarOfWrongMIMEType()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
        dbContext.Database.EnsureDeleted();
        dbContext.Database.Migrate();

        var multipartFormDataContent = new MultipartFormDataContent();
        multipartFormDataContent.Add(new StringContent("test_username"), "username");
        multipartFormDataContent.Add(new StringContent("test@test.com"), "email");
        multipartFormDataContent.Add(new StringContent("test_password"), "password");

        HttpResponseMessage response;
        using (var streamContent = new StreamContent(new MemoryStream()))
        {
            streamContent.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
            multipartFormDataContent.Add(streamContent, "avatar", "test_file.txt");
            response = await _client.PostAsync("/users", multipartFormDataContent);
        }

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal(0, await dbContext.Users.CountAsync());
    }
}
