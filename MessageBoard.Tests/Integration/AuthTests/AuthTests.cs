using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;

using MessageBoard.Data;

namespace MessageBoard.Tests.Integration.AuthTests;

[Collection("Sync")]
public class AuthTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public AuthTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder => { builder.UseEnvironment("Staging"); });
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task UnauthenticatedUserCanLogIn()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
        dbContext.Database.EnsureDeleted();
        dbContext.Database.Migrate();
        await _client.PostAsync("/users", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "username", "test_username" },
            { "email", "test@test.com" },
            { "password", "test_password" },
            { "passwordConfirmation", "test_password" },
        }));

        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "username", "test_username" },
            { "password", "test_password" },
        });

        var response1 = await _client.PostAsync("/login", content);
        var response2 = await _client.GetAsync("/secure-endpoint");

        Assert.Equal(HttpStatusCode.Redirect, response1.StatusCode);
        Assert.Equal("/", response1.Headers.Location.OriginalString);
        response2.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task UnauthenticatedUserCanLogInAsModerator()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
        dbContext.Database.EnsureDeleted();
        dbContext.Database.Migrate();
        await _client.PostAsync("/users", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "username", "test_username" },
            { "email", "test@test.com" },
            { "password", "test_password" },
            { "passwordConfirmation", "test_password" },
        }));

        var user = dbContext.Users.FirstOrDefault(u => u.Username == "test_username");
        user.Role = "Moderator";
        dbContext.Users.Update(user);
        await dbContext.SaveChangesAsync();

        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "username", "test_username" },
            { "password", "test_password" },
        });

        var response1 = await _client.PostAsync("/login", content);
        var response2 = await _client.GetAsync("/moderator-endpoint");

        Assert.Equal(HttpStatusCode.Redirect, response1.StatusCode);
        Assert.Equal("/", response1.Headers.Location.OriginalString);
        response2.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task UnauthenticatedUserCannotLogInWithNonExistentUsername()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
        dbContext.Database.EnsureDeleted();
        dbContext.Database.Migrate();

        var response = await _client.PostAsync("/login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "username", "test_username" },
            { "password", "test_password" },
        }));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task UnauthenticatedUserCannotLogInWithWrongPassword()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
        dbContext.Database.EnsureDeleted();
        dbContext.Database.Migrate();
        await _client.PostAsync("/users", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "username", "test_username" },
            { "email", "test@test.com" },
            { "password", "test_password" },
            { "passwordConfirmation", "test_password" },
        }));

        var response = await _client.PostAsync("/login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "username", "test_username" },
            { "password", "wrong_password" },
        }));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task AuthenticatedUserCannotLogIn()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
        dbContext.Database.EnsureDeleted();
        dbContext.Database.Migrate();
        await _client.PostAsync("/users", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "username", "test_username" },
            { "email", "test@test.com" },
            { "password", "test_password" },
            { "passwordConfirmation", "test_password" },
        }));

        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "username", "test_username" },
            { "password", "test_password" },
        });

        await _client.PostAsync("/login", content);
        var response = await _client.PostAsync("/login", content);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/forbidden?returnUrl=", response.Headers.Location.OriginalString);
    }

    [Fact]
    public async Task BannedUserCannotLogIn()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
        dbContext.Database.EnsureDeleted();
        dbContext.Database.Migrate();
        await _client.PostAsync("/users", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "username", "test_username" },
            { "email", "test@test.com" },
            { "password", "test_password" },
            { "passwordConfirmation", "test_password" },
        }));

        var user = dbContext.Users.FirstOrDefault(u => u.Username == "test_username");
        var ban = await DataFactory.CreateBan(dbContext, user);
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "username", "test_username" },
            { "password", "test_password" },
        });

        var response1 = await _client.PostAsync("/login", content);
        var response2 = await _client.GetAsync("/secure-endpoint");

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response1.StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, response2.StatusCode);
        Assert.Contains("/login?returnUrl=", response2.Headers.Location.OriginalString);
    }

    [Fact]
    public async Task AuthenticatedUserCanLogOut()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
        dbContext.Database.EnsureDeleted();
        dbContext.Database.Migrate();
        await _client.PostAsync("/users", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "username", "test_username" },
            { "email", "test@test.com" },
            { "password", "test_password" },
            { "passwordConfirmation", "test_password" },
        }));

        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "username", "test_username" },
            { "password", "test_password" },
        });

        await _client.PostAsync("/login", content);
        _client.DefaultRequestHeaders.Add("X-XSRF-TOKEN", await Utilities.GetCSRFToken(_client));
        var response1 = await _client.PostAsync("/logout", null);
        var response2 = await _client.GetAsync("/secure-endpoint");

        Assert.Equal(HttpStatusCode.Redirect, response1.StatusCode);
        Assert.Equal("/", response1.Headers.Location.OriginalString);
        Assert.Equal(HttpStatusCode.Redirect, response2.StatusCode);
        Assert.Contains("/login?returnUrl=", response2.Headers.Location.OriginalString);
    }

    [Fact]
    public async Task UnauthenticatedUserCannotLogOut()
    {
        var response = await _client.PostAsync("/logout", null);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/login?returnUrl=", response.Headers.Location.OriginalString);
    }

    [Fact]
    public async Task CSRFProtectionIsActiveForLogout()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
        dbContext.Database.EnsureDeleted();
        dbContext.Database.Migrate();
        await _client.PostAsync("/users", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "username", "test_username" },
            { "email", "test@test.com" },
            { "password", "test_password" },
            { "passwordConfirmation", "test_password" },
        }));

        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "username", "test_username" },
            { "password", "test_password" },
        });

        await _client.PostAsync("/login", content);
        var response = await _client.PostAsync("/logout", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
