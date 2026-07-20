using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace TemplateSistema.Api.Tests;

public class HealthEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public HealthEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:DefaultConnection",
                "Host=localhost;Port=5432;Database=gestao_ic_test;Username=postgres;Password=postgres");
        }).CreateClient();
    }

    [Fact]
    public async Task GetHealth_ReturnsOkOrServiceUnavailable()
    {
        var response = await _client.GetAsync("/health");

        Assert.True(
            response.StatusCode is HttpStatusCode.OK or HttpStatusCode.ServiceUnavailable,
            $"Unexpected status: {response.StatusCode}");
    }
}
