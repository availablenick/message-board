using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;

using MessageBoard.Data;
using MessageBoard.Models;

namespace MessageBoard.Tests.Integration.ComplaintTests;

[Collection("Sync")]
public class ComplaintCreationTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public ComplaintCreationTests(CustomWebApplicationFactory<Program> factory)
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
    public async Task TopicComplaintCanBeCreated()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
        dbContext.Database.EnsureDeleted();
        dbContext.Database.Migrate();
        var topic = await DataFactory.CreateTopic(dbContext);

        _client.DefaultRequestHeaders.Add("UserId", topic.Author.Id.ToString());
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "_token", await Utilities.GetCSRFToken(_client) },
            { "reason", "test_reason" },
            { "targetId", topic.Id.ToString() },
        });

        DateTime timeBeforeResponse = DateTime.Now;
        var response = await _client.PostAsync("/complaints", content);
        DateTime timeAfterResponse = DateTime.Now;

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var complaint = dbContext.Complaints.FirstOrDefault(c => c.Reason == "test_reason");
        Assert.NotNull(complaint);
        Assert.Equal(topic.Author.Id, complaint.Author.Id);
        Assert.Equal(topic.Id, complaint.Target.Id);
        Assert.True(topic.Author.Complaints.Exists(c => c.Id == complaint.Id));
        Assert.True(topic.Complaints.Exists(c => c.Id == complaint.Id));
        Assert.True(complaint.CreatedAt.CompareTo(timeBeforeResponse) >= 0);
        Assert.True(complaint.CreatedAt.CompareTo(timeAfterResponse) <= 0);
        Assert.True(complaint.CreatedAt.CompareTo(complaint.UpdatedAt) == 0);
    }

    [Fact]
    public async Task PostComplaintCanBeCreated()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
        dbContext.Database.EnsureDeleted();
        dbContext.Database.Migrate();
        var post = await DataFactory.CreatePost(dbContext);

        _client.DefaultRequestHeaders.Add("UserId", post.Author.Id.ToString());
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "_token", await Utilities.GetCSRFToken(_client) },
            { "reason", "test_reason" },
            { "targetId", post.Id.ToString() },
        });

        DateTime timeBeforeResponse = DateTime.Now;
        var response = await _client.PostAsync("/complaints", content);
        DateTime timeAfterResponse = DateTime.Now;

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var complaint = dbContext.Complaints.FirstOrDefault(c => c.Reason == "test_reason");
        Assert.NotNull(complaint);
        Assert.Equal(post.Author.Id, complaint.Author.Id);
        Assert.Equal(post.Id, complaint.Target.Id);
        Assert.True(post.Author.Complaints.Exists(c => c.Id == complaint.Id));
        Assert.True(post.Complaints.Exists(c => c.Id == complaint.Id));
        Assert.True(complaint.CreatedAt.CompareTo(timeBeforeResponse) >= 0);
        Assert.True(complaint.CreatedAt.CompareTo(timeAfterResponse) <= 0);
        Assert.True(complaint.CreatedAt.CompareTo(complaint.UpdatedAt) == 0);
    }

    [Fact]
    public async Task ComplaintCannotBeCreatedWithoutReason()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
        dbContext.Database.EnsureDeleted();
        dbContext.Database.Migrate();
        var post = await DataFactory.CreatePost(dbContext);

        _client.DefaultRequestHeaders.Add("UserId", post.Author.Id.ToString());
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "_token", await Utilities.GetCSRFToken(_client) },
            { "targetId", post.Id.ToString() },
        });

        var response = await _client.PostAsync("/complaints", content);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal(0, await dbContext.Complaints.CountAsync());
    }

    [Fact]
    public async Task ComplaintCannotBeCreatedForNonExistentTarget()
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
            { "targetId", "3" },
        });

        var response = await _client.PostAsync("/complaints", content);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(0, await dbContext.Complaints.CountAsync());
    }

    [Fact]
    public async Task ComplaintCannotBeCreatedByUnauthenticatedUser()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
        dbContext.Database.EnsureDeleted();
        dbContext.Database.Migrate();
        var post = await DataFactory.CreatePost(dbContext);

        _client.DefaultRequestHeaders.Remove("Authorization");
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "reason", "test_reason" },
            { "targetId", post.Id.ToString() },
        });

        var response = await _client.PostAsync("/complaints", content);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(0, await dbContext.Complaints.CountAsync());
    }

    [Fact]
    public async Task CSRFProtectionIsActive()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
        dbContext.Database.EnsureDeleted();
        dbContext.Database.Migrate();
        var post = await DataFactory.CreatePost(dbContext);

        _client.DefaultRequestHeaders.Add("UserId", post.Author.Id.ToString());
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "reason", "test_reason" },
            { "targetId", post.Id.ToString() },
        });

        var response = await _client.PostAsync("/complaints", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, await dbContext.Complaints.CountAsync());
    }
}
