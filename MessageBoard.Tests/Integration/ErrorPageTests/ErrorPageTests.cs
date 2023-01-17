using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;

using MessageBoard.Data;
using MessageBoard.Models;

namespace MessageBoard.Tests.Integration.ErrorPageTests;

[Collection("Sync")]
public class ErrorPageTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public ErrorPageTests(CustomWebApplicationFactory<Program> factory)
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
    public async Task ForbiddenPageCanBeAccessed()
    {
        _client.DefaultRequestHeaders.Remove("Authorization");

        var response = await _client.GetAsync("/forbidden");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
