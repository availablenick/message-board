using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;

using MessageBoard.Data;
using MessageBoard.Models;

namespace MessageBoard.Tests.Integration.ComplaintTests;

[Collection("Sync")]
public class ComplaintDeleteTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public ComplaintDeleteTests(CustomWebApplicationFactory<Program> factory)
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
    public async Task ComplaintCanBeDeleted()
    {
        User user;
        Complaint complaint;
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
            dbContext.Database.EnsureDeleted();
            dbContext.Database.Migrate();
            user = await DataFactory.CreateUser(dbContext);
            complaint = await DataFactory.CreateComplaint(dbContext);
        }

        _client.DefaultRequestHeaders.Add("UserId", user.Id.ToString());
        _client.DefaultRequestHeaders.Add("Role", "Moderator");
        _client.DefaultRequestHeaders.Add("X-XSRF-TOKEN", await Utilities.GetCSRFToken(_client));
        var response = await _client.DeleteAsync($"/complaints/{complaint.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
            Assert.Null(dbContext.Complaints.FirstOrDefault(c => c.Id == complaint.Id));
            var freshAuthor = await dbContext.Users.Include(u => u.Complaints)
                .FirstAsync(u => u.Id == complaint.Author.Id);

            var freshTarget = await dbContext.Rateables.Include(r => r.Complaints)
                .FirstAsync(r => r.Id == complaint.Target.Id);

            Assert.Empty(freshAuthor.Complaints);
            Assert.Empty(freshTarget.Complaints);
        }
    }

    [Fact]
    public async Task UserCannotDeleteNonExistentComplaint()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
        dbContext.Database.EnsureDeleted();
        dbContext.Database.Migrate();
        var user = await DataFactory.CreateUser(dbContext);

        _client.DefaultRequestHeaders.Add("UserId", user.Id.ToString());
        _client.DefaultRequestHeaders.Add("Role", "Moderator");
        _client.DefaultRequestHeaders.Add("X-XSRF-TOKEN", await Utilities.GetCSRFToken(_client));
        var response = await _client.DeleteAsync("/complaints/1");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ComplaintCannotBeDeletedByUnauthenticatedUser()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
        dbContext.Database.EnsureDeleted();
        dbContext.Database.Migrate();
        var complaint = await DataFactory.CreateComplaint(dbContext);

        _client.DefaultRequestHeaders.Remove("Authorization");
        var response = await _client.DeleteAsync($"/complaints/{complaint.Id}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotNull(dbContext.Complaints.FirstOrDefault(c => c.Id == complaint.Id));
    }

    [Fact]
    public async Task ComplaintCannotBeDeletedByRegularUser()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
        dbContext.Database.EnsureDeleted();
        dbContext.Database.Migrate();
        var user = await DataFactory.CreateUser(dbContext);
        var complaint = await DataFactory.CreateComplaint(dbContext);

        _client.DefaultRequestHeaders.Add("UserId", user.Id.ToString());
        _client.DefaultRequestHeaders.Add("X-XSRF-TOKEN", await Utilities.GetCSRFToken(_client));
        var response = await _client.DeleteAsync($"/complaints/{complaint.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.NotNull(dbContext.Complaints.FirstOrDefault(c => c.Id == complaint.Id));
    }

    [Fact]
    public async Task CSRFProtectionIsActive()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
        dbContext.Database.EnsureDeleted();
        dbContext.Database.Migrate();
        var user = await DataFactory.CreateUser(dbContext);
        var complaint = await DataFactory.CreateComplaint(dbContext);

        _client.DefaultRequestHeaders.Add("UserId", user.Id.ToString());
        _client.DefaultRequestHeaders.Add("Role", "Moderator");
        var response = await _client.DeleteAsync($"/complaints/{complaint.Id}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(dbContext.Complaints.FirstOrDefault(c => c.Id == complaint.Id));
    }

    [Fact]
    public async Task HTTPMethodOverrideCanBeUsed()
    {
        User user;
        Complaint complaint;
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
            dbContext.Database.EnsureDeleted();
            dbContext.Database.Migrate();
            user = await DataFactory.CreateUser(dbContext);
            complaint = await DataFactory.CreateComplaint(dbContext);
        }

        _client.DefaultRequestHeaders.Add("UserId", user.Id.ToString());
        _client.DefaultRequestHeaders.Add("Role", "Moderator");
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "_method", "DELETE" },
            { "_token", await Utilities.GetCSRFToken(_client) },
        });

        var response = await _client.PostAsync($"/complaints/{complaint.Id}", content);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<MessageBoardDbContext>();
            Assert.Null(dbContext.Complaints.FirstOrDefault(c => c.Id == complaint.Id));
            var freshAuthor = await dbContext.Users.Include(u => u.Complaints)
                .FirstAsync(u => u.Id == complaint.Author.Id);

            var freshTarget = await dbContext.Rateables.Include(r => r.Complaints)
                .FirstAsync(r => r.Id == complaint.Target.Id);

            Assert.Empty(freshAuthor.Complaints);
            Assert.Empty(freshTarget.Complaints);
        }
    }
}
