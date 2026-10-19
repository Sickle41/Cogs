using System.Net;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;

namespace WarmachineAPI.Tests;

public class RateLimitingTests : IClassFixture<WarmachineApiFactory>
{
    private readonly HttpClient _client;

    public RateLimitingTests(WarmachineApiFactory factory)
    {
        // Override the production limiter (100/minute) with a tiny one so the test
        // doesn't need to fire 100+ real requests or wait out a real time window.
        var limitedFactory = factory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
                services.PostConfigure<RateLimiterOptions>(options =>
                {
                    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(_ =>
                        RateLimitPartition.GetFixedWindowLimiter("test-partition", _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 3,
                            Window = TimeSpan.FromMinutes(1),
                            QueueLimit = 0
                        }));
                })));

        _client = limitedFactory.CreateClient();
    }

    [Fact]
    public async Task ExceedingLimit_Returns429()
    {
        for (var i = 0; i < 3; i++)
        {
            var response = await _client.GetAsync("/api/factions");
            Assert.NotEqual(HttpStatusCode.TooManyRequests, response.StatusCode);
        }

        var limitedResponse = await _client.GetAsync("/api/factions");
        Assert.Equal(HttpStatusCode.TooManyRequests, limitedResponse.StatusCode);
    }

    [Fact]
    public async Task HealthEndpoints_AreNotRateLimited()
    {
        for (var i = 0; i < 5; i++)
        {
            var response = await _client.GetAsync("/health/live");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}
