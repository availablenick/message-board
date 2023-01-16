using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;

using MessageBoard.Data;
using MessageBoard.Models;

namespace MessageBoard.Tests.Integration.RatingTests;

[Collection("Sync")]
public class RatingUpdateTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public RatingUpdateTests(CustomWebApplicationFactory<Program> factory)
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
    public async Task RatingCanBeUpdated(int ratingValue)
    {
        Rating newRating;
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
            dbContext.Database.EnsureDeleted();
            dbContext.Database.Migrate();
            newRating = await DataFactory.CreateRating(ratingValue, dbContext);
        }

        _client.DefaultRequestHeaders.Add("UserId", newRating.Owner.Id.ToString());
        int newValue = ratingValue * -1;
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "_token", await Utilities.GetCSRFToken(_client) },
            { "value", newValue.ToString() },
        });

        DateTime timeBeforeResponse = DateTime.Now;
        var response = await _client.PutAsync($"/ratings/{newRating.Id}", content);
        DateTime timeAfterResponse = DateTime.Now;

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal($"/rateables/{newRating.Target.Id}", response.Headers.Location.OriginalString);
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
            var rating = dbContext.Ratings.FirstOrDefault(r => r.Value == newValue);
            Assert.NotNull(rating);
            Assert.True(rating.UpdatedAt.CompareTo(timeBeforeResponse) >= 0);
            Assert.True(rating.UpdatedAt.CompareTo(timeAfterResponse) <= 0);
        }
    }

    [Theory]
    [InlineData(-2)]
    [InlineData(0)]
    [InlineData(2)]
    public async Task RatingCannotBeUpdatedWithInvalidValues(int ratingValue)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
        dbContext.Database.EnsureDeleted();
        dbContext.Database.Migrate();
        var newRating = await DataFactory.CreateRating(1, dbContext);

        _client.DefaultRequestHeaders.Add("UserId", newRating.Owner.Id.ToString());
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "_token", await Utilities.GetCSRFToken(_client) },
            { "value", ratingValue.ToString() },
        });

        var response = await _client.PutAsync($"/ratings/{newRating.Id}", content);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Null(dbContext.Ratings.FirstOrDefault(r => r.Value == ratingValue));
    }

    [Fact]
    public async Task RatingCanOnlyBeUpdatedByItsOwner()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
        dbContext.Database.EnsureDeleted();
        dbContext.Database.Migrate();
        var user = await DataFactory.CreateUser(dbContext);
        var newRating = await DataFactory.CreateRating(1, dbContext);

        _client.DefaultRequestHeaders.Add("UserId", user.Id.ToString());
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "_token", await Utilities.GetCSRFToken(_client) },
            { "value", "-1" },
        });

        var response = await _client.PutAsync($"/ratings/{newRating.Id}", content);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Null(dbContext.Ratings.FirstOrDefault(r => r.Value == -1));
    }

    [Fact]
    public async Task RatingCannotBeUpdatedByUnauthenticatedUser()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
        dbContext.Database.EnsureDeleted();
        dbContext.Database.Migrate();
        var newRating = await DataFactory.CreateRating(1, dbContext);

        _client.DefaultRequestHeaders.Remove("Authorization");
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "value", "-1" },
        });

        var response = await _client.PutAsync($"/ratings/{newRating.Id}", content);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Null(dbContext.Ratings.FirstOrDefault(r => r.Value == -1));
    }

    [Fact]
    public async Task UserCannotUpdateNonExistentRating()
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
            { "value", "1" },
        });

        var response = await _client.PutAsync("/ratings/1", content);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CSRFProtectionIsActive()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
        dbContext.Database.EnsureDeleted();
        dbContext.Database.Migrate();
        var newRating = await DataFactory.CreateRating(1, dbContext);

        _client.DefaultRequestHeaders.Add("UserId", newRating.Owner.Id.ToString());
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "value", "-1" },
        });

        var response = await _client.PutAsync($"/ratings/{newRating.Id}", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Null(dbContext.Ratings.FirstOrDefault(r => r.Value == -1));
    }

    [Fact]
    public async Task HTTPMethodOverrideCanBeUsed()
    {
        Rating newRating;
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
            dbContext.Database.EnsureDeleted();
            dbContext.Database.Migrate();
            newRating = await DataFactory.CreateRating(-1, dbContext);
        }

        _client.DefaultRequestHeaders.Add("UserId", newRating.Owner.Id.ToString());
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "_method", "PUT" },
            { "_token", await Utilities.GetCSRFToken(_client) },
            { "value", "1" },
        });

        DateTime timeBeforeResponse = DateTime.Now;
        var response = await _client.PostAsync($"/ratings/{newRating.Id}", content);
        DateTime timeAfterResponse = DateTime.Now;

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal($"/rateables/{newRating.Target.Id}", response.Headers.Location.OriginalString);
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
            var rating = dbContext.Ratings.FirstOrDefault(r => r.Value == 1);
            Assert.NotNull(rating);
            Assert.True(rating.UpdatedAt.CompareTo(timeBeforeResponse) >= 0);
            Assert.True(rating.UpdatedAt.CompareTo(timeAfterResponse) <= 0);
        }
    }
}
