using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;

using MessageBoard.Data;
using MessageBoard.Models;

namespace MessageBoard.Tests.Integration.SectionTests;

[Collection("Sync")]
public class SectionUpdateTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public SectionUpdateTests(CustomWebApplicationFactory<Program> factory)
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
    public async Task SectionCanBeUpdated()
    {
        User user;
        Section section;
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
            dbContext.Database.EnsureDeleted();
            dbContext.Database.Migrate();
            user = await DataFactory.CreateUser(dbContext);
            section = await DataFactory.CreateSection(dbContext);
        }

        _client.DefaultRequestHeaders.Add("UserId", user.Id.ToString());
        _client.DefaultRequestHeaders.Add("Role", "Moderator");
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "_token", await Utilities.GetCSRFToken(_client) },
            { "name", $"{section.Name}_edit" },
        });

        DateTime timeBeforeResponse = DateTime.Now;
        var response = await _client.PutAsync($"/sections/{section.Id}", content);
        DateTime timeAfterResponse = DateTime.Now;

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
            var freshSection = dbContext.Sections
                .FirstOrDefault(s => s.Name == $"{section.Name}_edit");

            Assert.NotNull(freshSection);
            Assert.True(freshSection.UpdatedAt.CompareTo(timeBeforeResponse) >= 0);
            Assert.True(freshSection.UpdatedAt.CompareTo(timeAfterResponse) <= 0);
        }
    }

    [Fact]
    public async Task SectionCannotBeUpdatedWithoutName()
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
        });

        var response = await _client.PutAsync($"/sections/{section.Id}", content);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Null(dbContext.Sections
            .FirstOrDefault(s => s.Name == $"{section.Name}_edit"));
    }

    [Fact]
    public async Task SectionCannotBeUpdatedByUnauthenticatedUser()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
        dbContext.Database.EnsureDeleted();
        dbContext.Database.Migrate();
        var user = await DataFactory.CreateUser(dbContext);
        var section = await DataFactory.CreateSection(dbContext);

        _client.DefaultRequestHeaders.Remove("Authorization");
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "name", $"{section.Name}_edit" },
        });

        var response = await _client.PutAsync($"/sections/{section.Id}", content);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotNull(dbContext.Sections
            .FirstOrDefault(s => s.Name == section.Name));
    }

    [Fact]
    public async Task SectionCannotBeUpdatedByRegularUser()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
        dbContext.Database.EnsureDeleted();
        dbContext.Database.Migrate();
        var user = await DataFactory.CreateUser(dbContext);
        var section = await DataFactory.CreateSection(dbContext);

        _client.DefaultRequestHeaders.Add("UserId", user.Id.ToString());
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "_token", await Utilities.GetCSRFToken(_client) },
            { "name", $"{section.Name}_edit" },
        });

        var response = await _client.PutAsync($"/sections/{section.Id}", content);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Null(dbContext.Sections
            .FirstOrDefault(s => s.Name == $"{section.Name}_edit"));
    }

    [Fact]
    public async Task UserCannotUpdateNonExistentSection()
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
            { "name", "test_name" },
        });

        var response = await _client.PutAsync("/sections/1", content);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CSRFProtectionIsActive()
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
            { "name", $"{section.Name}_edit" },
        });

        var response = await _client.PutAsync($"/sections/{section.Id}", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Null(dbContext.Sections
            .FirstOrDefault(s => s.Name == $"{section.Name}_edit"));
    }

    [Fact]
    public async Task HTTPMethodOverrideCanBeUsed()
    {
        User user;
        Section section;
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
            dbContext.Database.EnsureDeleted();
            dbContext.Database.Migrate();
            user = await DataFactory.CreateUser(dbContext);
            section = await DataFactory.CreateSection(dbContext);
        }

        _client.DefaultRequestHeaders.Add("UserId", user.Id.ToString());
        _client.DefaultRequestHeaders.Add("Role", "Moderator");
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "_method", "PUT" },
            { "_token", await Utilities.GetCSRFToken(_client) },
            { "name", $"{section.Name}_edit" },
        });

        DateTime timeBeforeResponse = DateTime.Now;
        var response = await _client.PostAsync($"/sections/{section.Id}", content);
        DateTime timeAfterResponse = DateTime.Now;

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
            var freshSection = dbContext.Sections
                .FirstOrDefault(s => s.Name == $"{section.Name}_edit");

            Assert.NotNull(freshSection);
            Assert.True(freshSection.UpdatedAt.CompareTo(timeBeforeResponse) >= 0);
            Assert.True(freshSection.UpdatedAt.CompareTo(timeAfterResponse) <= 0);
        }
    }
}
