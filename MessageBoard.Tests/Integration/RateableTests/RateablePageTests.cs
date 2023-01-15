using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;

using MessageBoard.Data;
using MessageBoard.Models;

namespace MessageBoard.Tests.Integration.RateableTests;

[Collection("Sync")]
public class RateablePageTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public RateablePageTests(CustomWebApplicationFactory<Program> factory)
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
    public async Task RateablePageCanRedirectToTopic()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
        dbContext.Database.EnsureDeleted();
        dbContext.Database.Migrate();
        var topic = await DataFactory.CreateTopic(dbContext);

        _client.DefaultRequestHeaders.Remove("Authorization");
        var response = await _client.GetAsync($"/rateables/{topic.Id}");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal($"/discussions/{topic.Id}", response.Headers.Location.OriginalString);
    }

    [Fact]
    public async Task RateablePageCanRedirectToPrivateMessage()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
        dbContext.Database.EnsureDeleted();
        dbContext.Database.Migrate();
        var message = await DataFactory.CreatePrivateMessage(dbContext);

        _client.DefaultRequestHeaders.Remove("Authorization");
        var response = await _client.GetAsync($"/rateables/{message.Id}");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal($"/discussions/{message.Id}", response.Headers.Location.OriginalString);
    }

    [Fact]
    public async Task RateablePageCanRedirectToPostDiscussion()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
        dbContext.Database.EnsureDeleted();
        dbContext.Database.Migrate();
        var post = await DataFactory.CreatePost(dbContext);

        _client.DefaultRequestHeaders.Remove("Authorization");
        var response = await _client.GetAsync($"/rateables/{post.Id}");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal($"/discussions/{post.Discussion.Id}", response.Headers.Location.OriginalString);
    }

    [Fact]
    public async Task UserCannotAccessNonExistentRateablePage()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
        dbContext.Database.EnsureDeleted();
        dbContext.Database.Migrate();

        _client.DefaultRequestHeaders.Remove("Authorization");
        var response = await _client.GetAsync("/rateables/1");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
