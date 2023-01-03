using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;

using MessageBoard.Data;
using MessageBoard.Models;

namespace MessageBoard.Tests.Integration.RatingTests;

[Collection("Sync")]
public class RatingDeleteTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public RatingDeleteTests(CustomWebApplicationFactory<Program> factory)
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
    public async Task RatingCanBeDeleted()
    {
        Rating rating;
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
            dbContext.Database.EnsureDeleted();
            dbContext.Database.Migrate();
            rating = await DataFactory.CreateRating(1, dbContext);
        }

        _client.DefaultRequestHeaders.Add("UserId", rating.Owner.Id.ToString());
        _client.DefaultRequestHeaders.Add("X-XSRF-TOKEN", await Utilities.GetCSRFToken(_client));
        var response = await _client.DeleteAsync($"/ratings/{rating.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
            Assert.Null(dbContext.Ratings.FirstOrDefault(r => r.Id == rating.Id));
            var freshOwner = await dbContext.Users.Include(u => u.Ratings)
                .FirstAsync(u => u.Id == rating.Owner.Id);
            
            var freshTarget = await dbContext.Rateables.Include(r => r.Ratings)
                .FirstAsync(r => r.Id == rating.Target.Id);

            Assert.Empty(freshOwner.Ratings);
            Assert.Empty(freshTarget.Ratings);
        }
    }

    [Fact]
    public async Task RatingCanOnlyBeDeletedByItsOwner()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
        dbContext.Database.EnsureDeleted();
        dbContext.Database.Migrate();
        var user = await DataFactory.CreateUser(dbContext);
        var rating = await DataFactory.CreateRating(1, dbContext);

        _client.DefaultRequestHeaders.Add("UserId", user.Id.ToString());
        _client.DefaultRequestHeaders.Add("X-XSRF-TOKEN", await Utilities.GetCSRFToken(_client));
        var response = await _client.DeleteAsync($"/ratings/{rating.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.NotNull(dbContext.Ratings.FirstOrDefault(r => r.Id == rating.Id));
    }

    [Fact]
    public async Task RatingCannotBeDeletedByUnauthenticatedUser()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
        dbContext.Database.EnsureDeleted();
        dbContext.Database.Migrate();
        var rating = await DataFactory.CreateRating(1, dbContext);

        _client.DefaultRequestHeaders.Remove("Authorization");
        var response = await _client.DeleteAsync($"/ratings/{rating.Id}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotNull(dbContext.Ratings.FirstOrDefault(r => r.Id == rating.Id));
    }

    [Fact]
    public async Task UserCannotDeleteNonExistentRating()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
        dbContext.Database.EnsureDeleted();
        dbContext.Database.Migrate();
        var user = await DataFactory.CreateUser(dbContext);

        _client.DefaultRequestHeaders.Add("UserId", user.Id.ToString());
        _client.DefaultRequestHeaders.Add("X-XSRF-TOKEN", await Utilities.GetCSRFToken(_client));
        var response = await _client.DeleteAsync("/ratings/1");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CSRFProtectionIsActive()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
        dbContext.Database.EnsureDeleted();
        dbContext.Database.Migrate();
        var rating = await DataFactory.CreateRating(1, dbContext);

        _client.DefaultRequestHeaders.Add("UserId", rating.Owner.Id.ToString());
        var response = await _client.DeleteAsync($"/ratings/{rating.Id}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(dbContext.Ratings.FirstOrDefault(r => r.Id == rating.Id));
    }

    [Fact]
    public async Task HTTPMethodOverrideCanBeUsed()
    {
        Rating rating;
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
            dbContext.Database.EnsureDeleted();
            dbContext.Database.Migrate();
            rating = await DataFactory.CreateRating(1, dbContext);
        }

        _client.DefaultRequestHeaders.Add("UserId", rating.Owner.Id.ToString());
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "_method", "DELETE" },
            { "_token", await Utilities.GetCSRFToken(_client) },
        });

        var response = await _client.PostAsync($"/ratings/{rating.Id}", content);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
            Assert.Null(dbContext.Ratings.FirstOrDefault(r => r.Id == rating.Id));
            var freshOwner = await dbContext.Users.Include(u => u.Ratings)
                .FirstAsync(u => u.Id == rating.Owner.Id);
            
            var freshTarget = await dbContext.Rateables.Include(r => r.Ratings)
                .FirstAsync(r => r.Id == rating.Target.Id);

            Assert.Empty(freshOwner.Ratings);
            Assert.Empty(freshTarget.Ratings);
        }
    }
}
