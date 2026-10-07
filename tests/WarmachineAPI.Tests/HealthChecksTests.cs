using System.Net;

namespace WarmachineAPI.Tests;

public class HealthChecksTests : IClassFixture<WarmachineApiFactory>
{
    private readonly HttpClient _client;

    public HealthChecksTests(WarmachineApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Health_ReturnsHealthy()
    {
        var response = await _client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task HealthLive_ReturnsHealthyWithoutRunningDbCheck()
    {
        var response = await _client.GetAsync("/health/live");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }
}
