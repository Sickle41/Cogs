using Microsoft.AspNetCore.Hosting;

namespace WarmachineAPI.Tests;

public class CorsTests : IClassFixture<WarmachineApiFactory>
{
    private readonly HttpClient _client;

    public CorsTests(WarmachineApiFactory factory)
    {
        var devFactory = factory.WithWebHostBuilder(builder => builder.UseEnvironment("Development"));
        _client = devFactory.CreateClient();
    }

    private static HttpRequestMessage PreflightRequest(string origin)
    {
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/factions");
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", "GET");
        return request;
    }

    [Fact]
    public async Task Preflight_FromAllowedDevOrigin_ReturnsAccessControlHeader()
    {
        var response = await _client.SendAsync(PreflightRequest("http://localhost:3000"));

        Assert.True(response.Headers.Contains("Access-Control-Allow-Origin"));
        Assert.Equal("http://localhost:3000", response.Headers.GetValues("Access-Control-Allow-Origin").Single());
    }

    [Fact]
    public async Task Preflight_FromDisallowedOrigin_HasNoAccessControlHeader()
    {
        var response = await _client.SendAsync(PreflightRequest("http://not-allowed.example.com"));

        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }
}
