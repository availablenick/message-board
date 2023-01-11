using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;

using MessageBoard.Data;
using MessageBoard.Models;

namespace MessageBoard.Tests.Integration.AuthTests;

[Collection("Sync")]
public class AuthPageTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public AuthPageTests(CustomWebApplicationFactory<Program> factory)
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
    public async Task LoginPageCanBeAccessed()
    {
        _client.DefaultRequestHeaders.Remove("Authorization");

        var response = await _client.GetAsync("/login");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task LoginPageCannotBeAccessedByUnauthenticatedUser()
    {
        _client.DefaultRequestHeaders.Add("UserId", "1");

        var response = await _client.GetAsync("/login");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/", response.Headers.Location.OriginalString);
    }
}
