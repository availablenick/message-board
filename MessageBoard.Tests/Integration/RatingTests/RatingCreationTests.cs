using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;

using MessageBoard.Data;
using MessageBoard.Models;

namespace MessageBoard.Tests.Integration.RatingTests;

[Collection("Sync")]
public class RatingCreationTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public RatingCreationTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Test");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(-1)]
    public async Task TopicRatingCanBeCreated(int ratingValue)
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
            { "value", ratingValue.ToString() },
            { "targetId", topic.Id.ToString() },
        });

        DateTime timeBeforeResponse = DateTime.Now;
        var response = await _client.PostAsync("/ratings", content);
        DateTime timeAfterResponse = DateTime.Now;

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var ratingRecord = from r in dbContext.Ratings
                        where r.Value == ratingValue
                        select r;

        var rating = ratingRecord.FirstOrDefault();
        Assert.NotNull(rating);
        Assert.Equal(topic.Author.Id, rating.Owner.Id);
        Assert.Equal(topic.Id, rating.Target.Id);
        Assert.True(topic.Author.Ratings.Exists(r => r.Id == rating.Id));
        Assert.True(topic.Ratings.Exists(r => r.Id == rating.Id));
        Assert.True(rating.CreatedAt.CompareTo(timeBeforeResponse) >= 0);
        Assert.True(rating.CreatedAt.CompareTo(timeAfterResponse) <= 0);
        Assert.True(rating.CreatedAt.CompareTo(rating.UpdatedAt) == 0);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(-1)]
    public async Task PostRatingCanBeCreated(int ratingValue)
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
            { "value", ratingValue.ToString() },
            { "targetId", post.Id.ToString() },
        });

        DateTime timeBeforeResponse = DateTime.Now;
        var response = await _client.PostAsync("/ratings", content);
        DateTime timeAfterResponse = DateTime.Now;

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var ratingRecord = from r in dbContext.Ratings
                        where r.Value == ratingValue
                        select r;

        var rating = ratingRecord.FirstOrDefault();
        Assert.NotNull(rating);
        Assert.Equal(post.Author.Id, rating.Owner.Id);
        Assert.Equal(post.Id, rating.Target.Id);
        Assert.True(post.Author.Ratings.Exists(r => r.Id == rating.Id));
        Assert.True(post.Ratings.Exists(r => r.Id == rating.Id));
        Assert.True(rating.CreatedAt.CompareTo(timeBeforeResponse) >= 0);
        Assert.True(rating.CreatedAt.CompareTo(timeAfterResponse) <= 0);
        Assert.True(rating.CreatedAt.CompareTo(rating.UpdatedAt) == 0);
    }
    
    [Theory]
    [InlineData(-2)]
    [InlineData(0)]
    [InlineData(2)]
    public async Task RatingCannotBeCreatedWithInvalidValues(int ratingValue)
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
            { "value", ratingValue.ToString() },
            { "targetId", post.Id.ToString() },
        });

        var response = await _client.PostAsync("/ratings", content);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal(0, await dbContext.Ratings.CountAsync());
    }

    [Fact]
    public async Task RatingCannotBeCreatedWithoutTargetId()
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
            { "value", "1" },
        });

        var response = await _client.PostAsync("/ratings", content);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal(0, await dbContext.Ratings.CountAsync());
    }

    [Fact]
    public async Task RatingCannotBeCreatedByUnauthenticatedUser()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
        dbContext.Database.EnsureDeleted();
        dbContext.Database.Migrate();
        var post = await DataFactory.CreatePost(dbContext);

        _client.DefaultRequestHeaders.Remove("Authorization");
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "_token", await Utilities.GetCSRFToken(_client) },
            { "value", "1" },
            { "targetId", post.Id.ToString() },
        });

        var response = await _client.PostAsync("/ratings", content);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(0, await dbContext.Ratings.CountAsync());
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
            { "value", "1" },
            { "targetId", post.Id.ToString() },
        });

        var response = await _client.PostAsync("/ratings", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
