using System.Net;
using System.Net.Http.Json;
using WarmachineAPI.Models;

namespace WarmachineAPI.Tests;

public class DeploymentZonesControllerTests : IClassFixture<WarmachineApiFactory>
{
    private readonly HttpClient _client;

    public DeploymentZonesControllerTests(WarmachineApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<Map> CreateMapAsync(string name)
    {
        var response = await _client.PostAsJsonAsync("/api/maps", new Map { Name = name }, TestJson.Options);
        return (await response.Content.ReadFromJsonAsync<Map>(TestJson.Options))!;
    }

    [Fact]
    public async Task Create_WithInvalidMap_ReturnsBadRequest()
    {
        var zone = new DeploymentZone { Name = "Orphan Zone", X = 0, Y = 0, Width = 100, Height = 300 };

        var response = await _client.PostAsJsonAsync($"/api/maps/{Guid.NewGuid()}/deployment-zones", zone, TestJson.Options);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_ThenGet_RoundTripsZone()
    {
        var map = await CreateMapAsync("Deployment Board");
        var zone = new DeploymentZone { Name = "Player 1", X = 0, Y = 0, Width = 1219.2, Height = 254 };

        var createResponse = await _client.PostAsJsonAsync($"/api/maps/{map.Id}/deployment-zones", zone, TestJson.Options);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content.ReadFromJsonAsync<DeploymentZone>(TestJson.Options);
        Assert.Equal(map.Id, created!.MapId);
        Assert.Equal(254, created.Height);

        var getResponse = await _client.GetAsync($"/api/deployment-zones/{created.Id}");
        var fetched = await getResponse.Content.ReadFromJsonAsync<DeploymentZone>(TestJson.Options);
        Assert.Equal("Player 1", fetched!.Name);
    }

    [Fact]
    public async Task GetForMap_OnlyReturnsThatMapsZones()
    {
        var mapA = await CreateMapAsync("Deployment Map A");
        var mapB = await CreateMapAsync("Deployment Map B");

        var createdResponse = await _client.PostAsJsonAsync($"/api/maps/{mapA.Id}/deployment-zones", new DeploymentZone { Name = "A Zone", Width = 100, Height = 200 }, TestJson.Options);
        var created = await createdResponse.Content.ReadFromJsonAsync<DeploymentZone>(TestJson.Options);
        await _client.PostAsJsonAsync($"/api/maps/{mapB.Id}/deployment-zones", new DeploymentZone { Name = "B Zone", Width = 100, Height = 200 }, TestJson.Options);

        var response = await _client.GetAsync($"/api/maps/{mapA.Id}/deployment-zones");
        var list = await response.Content.ReadFromJsonAsync<List<DeploymentZone>>(TestJson.Options);

        Assert.All(list!, z => Assert.Equal(mapA.Id, z.MapId));
        Assert.Contains(list!, z => z.Id == created!.Id);
    }

    [Fact]
    public async Task Delete_ThenGetById_ReturnsNotFound()
    {
        var map = await CreateMapAsync("Deployment Delete Board");
        var createResponse = await _client.PostAsJsonAsync($"/api/maps/{map.Id}/deployment-zones", new DeploymentZone { Name = "Temp Zone", Width = 100, Height = 200 }, TestJson.Options);
        var created = await createResponse.Content.ReadFromJsonAsync<DeploymentZone>(TestJson.Options);

        var deleteResponse = await _client.DeleteAsync($"/api/deployment-zones/{created!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await _client.GetAsync($"/api/deployment-zones/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }
}
