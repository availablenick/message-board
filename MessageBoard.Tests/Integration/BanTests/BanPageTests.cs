using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;

using MessageBoard.Data;
using MessageBoard.Models;

namespace MessageBoard.Tests.Integration.BanTests;

[Collection("Sync")]
public class BanPageTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public BanPageTests(CustomWebApplicationFactory<Program> factory)
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
    public async Task BanCreationPageCanBeAccessed()
    {
        _client.DefaultRequestHeaders.Add("UserId", "1");
        _client.DefaultRequestHeaders.Add("Role", "Moderator");

        var response = await _client.GetAsync("/bans/new");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task BanCreationPageCannotBeAccessedByRegularUser()
    {
        _client.DefaultRequestHeaders.Add("UserId", "1");

        var response = await _client.GetAsync("/bans/new");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task BanEditPageCanBeAccessed()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
        dbContext.Database.EnsureDeleted();
        dbContext.Database.Migrate();
        var ban = await DataFactory.CreateBan(dbContext);

        _client.DefaultRequestHeaders.Add("UserId", "1");
        _client.DefaultRequestHeaders.Add("Role", "Moderator");
        var response = await _client.GetAsync($"/bans/{ban.Id}/edit");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task BanEditPageCannotBeAccessedByRegularUser()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
        dbContext.Database.EnsureDeleted();
        dbContext.Database.Migrate();
        var ban = await DataFactory.CreateBan(dbContext);

        _client.DefaultRequestHeaders.Add("UserId", "1");
        var response = await _client.GetAsync($"/bans/{ban.Id}/edit");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UserCannotAccessEditPageOfNonExistentBan()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
        dbContext.Database.EnsureDeleted();
        dbContext.Database.Migrate();

        _client.DefaultRequestHeaders.Add("UserId", "1");
        _client.DefaultRequestHeaders.Add("Role", "Moderator");
        var response = await _client.GetAsync("/bans/1/edit");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
