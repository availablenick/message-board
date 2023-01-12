using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;

using MessageBoard.Data;
using MessageBoard.Models;

namespace MessageBoard.Tests.Integration.SectionTests;

[Collection("Sync")]
public class SectionCreationTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public SectionCreationTests(CustomWebApplicationFactory<Program> factory)
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
    public async Task SectionCanBeCreated()
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
            { "name", "test_section" },
            { "description", "test_description" },
        });

        DateTime timeBeforeResponse = DateTime.Now;
        var response = await _client.PostAsync("/sections", content);
        DateTime timeAfterResponse = DateTime.Now;

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/", response.Headers.Location.OriginalString);
        var section = dbContext.Sections
            .FirstOrDefault(s => s.Name == "test_section" &&
                s.Description == "test_description");

        Assert.NotNull(section);
        Assert.True(section.CreatedAt.CompareTo(timeBeforeResponse) >= 0);
        Assert.True(section.CreatedAt.CompareTo(timeAfterResponse) <= 0);
        Assert.True(section.CreatedAt.CompareTo(section.UpdatedAt) == 0);
    }

    [Fact]
    public async Task SectionCannotBeCreatedWithoutName()
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
            { "description", "test_description" },
        });

        var response = await _client.PostAsync("/sections", content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(0, await dbContext.Sections.CountAsync());
    }

    [Fact]
    public async Task SectionCannotBeCreatedWithoutDescription()
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
            { "name", "test_section" },
        });

        var response = await _client.PostAsync("/sections", content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(0, await dbContext.Sections.CountAsync());
    }

    [Fact]
    public async Task SectionCannotBeCreatedWithExistingName()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
        dbContext.Database.EnsureDeleted();
        dbContext.Database.Migrate();
        var user = await DataFactory.CreateUser(dbContext);
        var section = await DataFactory.CreateSection(dbContext);

        _client.DefaultRequestHeaders.Add("UserId", user.Id.ToString());
        _client.DefaultRequestHeaders.Add("Role", "Moderator");
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "_token", await Utilities.GetCSRFToken(_client) },
            { "name", section.Name },
            { "description", "test_description" },
        });

        var response = await _client.PostAsync("/sections", content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, await dbContext.Sections.CountAsync());
    }

    [Fact]
    public async Task SectionCannotBeCreatedByUnauthenticatedUser()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
        dbContext.Database.EnsureDeleted();
        dbContext.Database.Migrate();

        _client.DefaultRequestHeaders.Remove("Authorization");
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "name", "test_section" },
            { "description", "test_description" },
        });

        var response = await _client.PostAsync("/sections", content);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(0, await dbContext.Sections.CountAsync());
    }

    [Fact]
    public async Task SectionCannotBeCreatedByRegularUser()
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
            { "name", "test_section" },
            { "description", "test_description" },
        });

        var response = await _client.PostAsync("/sections", content);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(0, await dbContext.Sections.CountAsync());
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
            { "name", "test_section" },
            { "description", "test_description" },
        });

        var response = await _client.PostAsync("/sections", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, await dbContext.Sections.CountAsync());
    }
}
