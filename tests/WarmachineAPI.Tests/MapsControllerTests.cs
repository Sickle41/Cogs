using System.Net;
using System.Net.Http.Json;
using WarmachineAPI.Models;

namespace WarmachineAPI.Tests;

public class MapsControllerTests : IClassFixture<WarmachineApiFactory>
{
    private readonly HttpClient _client;

    public MapsControllerTests(WarmachineApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Create_WithoutDimensions_DefaultsToStandardBoard()
    {
        var response = await _client.PostAsJsonAsync("/api/maps", new { name = "Default Board" }, TestJson.Options);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<Map>(TestJson.Options);
        Assert.Equal(1219.2, created!.Width);
        Assert.Equal(1219.2, created.Height);
    }

    [Fact]
    public async Task Create_WithCustomDimensions_PreservesThem()
    {
        var map = new Map { Name = "Big Board", Width = 1828.8, Height = 1219.2 };

        var response = await _client.PostAsJsonAsync("/api/maps", map, TestJson.Options);
        var created = await response.Content.ReadFromJsonAsync<Map>(TestJson.Options);

        Assert.Equal(1828.8, created!.Width);
        Assert.Equal(1219.2, created.Height);
    }

    [Fact]
    public async Task Delete_ThenGetById_ReturnsNotFound()
    {
        var map = new Map { Name = "Temporary Board" };
        var createResponse = await _client.PostAsJsonAsync("/api/maps", map, TestJson.Options);
        var created = await createResponse.Content.ReadFromJsonAsync<Map>(TestJson.Options);

        var deleteResponse = await _client.DeleteAsync($"/api/maps/{created!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await _client.GetAsync($"/api/maps/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }
}
