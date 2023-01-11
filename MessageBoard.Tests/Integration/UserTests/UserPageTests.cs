using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;

using MessageBoard.Data;
using MessageBoard.Models;

namespace MessageBoard.Tests.Integration.UserTests;

[Collection("Sync")]
public class UserPageTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public UserPageTests(CustomWebApplicationFactory<Program> factory)
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
    public async Task UserRegistrationPageCanBeAccessed()
    {
        _client.DefaultRequestHeaders.Remove("Authorization");

        var response = await _client.GetAsync("/users/new");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UserRegistrationPageCannotAccessedByAuthenticatedUser()
    {
        _client.DefaultRequestHeaders.Add("UserId", "1");

        var response = await _client.GetAsync("/users/new");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/", response.Headers.Location.OriginalString);
    }

    [Fact]
    public async Task UserListPageCanBeAccessed()
    {
        _client.DefaultRequestHeaders.Remove("Authorization");

        var response = await _client.GetAsync("/users");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
