using System.Net;
using System.Net.Http.Json;
using WarmachineAPI.Models;

namespace WarmachineAPI.Tests;

public class TerrainControllerTests : IClassFixture<WarmachineApiFactory>
{
    private readonly HttpClient _client;

    public TerrainControllerTests(WarmachineApiFactory factory)
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
        var terrain = new TerrainFeature { Name = "Orphan Forest", TerrainType = TerrainType.Forest, Shape = TerrainShape.Circle, Radius = 100 };

        var response = await _client.PostAsJsonAsync($"/api/maps/{Guid.NewGuid()}/terrain", terrain, TestJson.Options);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_CircleShape_RoundTripsCorrectly()
    {
        var map = await CreateMapAsync("Circle Test Board");
        var forest = new TerrainFeature
        {
            Name = "Central Forest",
            TerrainType = TerrainType.Forest,
            Shape = TerrainShape.Circle,
            X = 600,
            Y = 600,
            Radius = 150,
            GrantsConcealment = true,
            MovementEffect = MovementEffect.Difficult
        };

        var response = await _client.PostAsJsonAsync($"/api/maps/{map.Id}/terrain", forest, TestJson.Options);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<TerrainFeature>(TestJson.Options);
        Assert.Equal(map.Id, created!.MapId);
        Assert.Equal(TerrainShape.Circle, created.Shape);
        Assert.Equal(150, created.Radius);
        Assert.True(created.GrantsConcealment);
    }

    [Fact]
    public async Task Create_RectangleShape_RoundTripsCorrectly()
    {
        var map = await CreateMapAsync("Rectangle Test Board");
        var wall = new TerrainFeature
        {
            Name = "Ruined Wall",
            TerrainType = TerrainType.Obstruction,
            Shape = TerrainShape.Rectangle,
            X = 300,
            Y = 300,
            Width = 200,
            Height = 20,
            BlocksLineOfSight = true,
            GrantsCover = true,
            MovementEffect = MovementEffect.Impassable
        };

        var response = await _client.PostAsJsonAsync($"/api/maps/{map.Id}/terrain", wall, TestJson.Options);
        var created = await response.Content.ReadFromJsonAsync<TerrainFeature>(TestJson.Options);

        Assert.Equal(TerrainShape.Rectangle, created!.Shape);
        Assert.Equal(200, created.Width);
        Assert.True(created.BlocksLineOfSight);
    }

    [Fact]
    public async Task GetForMap_OnlyReturnsThatMapsTerrain()
    {
        var mapA = await CreateMapAsync("Map A");
        var mapB = await CreateMapAsync("Map B");

        var terrainA = new TerrainFeature { Name = "A Feature", TerrainType = TerrainType.Hill, Shape = TerrainShape.Circle, Radius = 50 };
        var createdAResponse = await _client.PostAsJsonAsync($"/api/maps/{mapA.Id}/terrain", terrainA, TestJson.Options);
        var createdA = await createdAResponse.Content.ReadFromJsonAsync<TerrainFeature>(TestJson.Options);

        var terrainB = new TerrainFeature { Name = "B Feature", TerrainType = TerrainType.Water, Shape = TerrainShape.Circle, Radius = 50 };
        await _client.PostAsJsonAsync($"/api/maps/{mapB.Id}/terrain", terrainB, TestJson.Options);

        var response = await _client.GetAsync($"/api/maps/{mapA.Id}/terrain");
        var list = await response.Content.ReadFromJsonAsync<List<TerrainFeature>>(TestJson.Options);

        Assert.All(list!, t => Assert.Equal(mapA.Id, t.MapId));
        Assert.Contains(list!, t => t.Id == createdA!.Id);
    }
}
