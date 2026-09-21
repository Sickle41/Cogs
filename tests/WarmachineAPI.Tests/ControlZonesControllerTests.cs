using System.Net;
using System.Net.Http.Json;
using WarmachineAPI.Models;

namespace WarmachineAPI.Tests;

public class ControlZonesControllerTests : IClassFixture<WarmachineApiFactory>
{
    private readonly HttpClient _client;

    public ControlZonesControllerTests(WarmachineApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<Scenario> CreateScenarioAsync(string name)
    {
        var response = await _client.PostAsJsonAsync("/api/scenarios", new Scenario { Name = name }, TestJson.Options);
        return (await response.Content.ReadFromJsonAsync<Scenario>(TestJson.Options))!;
    }

    [Fact]
    public async Task Create_WithInvalidScenario_ReturnsBadRequest()
    {
        var zone = new ControlZone { Name = "Orphan Flag", X = 300, Y = 300, Radius = 40, PointsPerTurn = 1 };

        var response = await _client.PostAsJsonAsync($"/api/scenarios/{Guid.NewGuid()}/zones", zone, TestJson.Options);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_ThenGet_RoundTripsZone()
    {
        var scenario = await CreateScenarioAsync("Incursion-Zone");
        var zone = new ControlZone { Name = "Center Flag", X = 609.6, Y = 609.6, Radius = 40, PointsPerTurn = 2 };

        var createResponse = await _client.PostAsJsonAsync($"/api/scenarios/{scenario.Id}/zones", zone, TestJson.Options);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content.ReadFromJsonAsync<ControlZone>(TestJson.Options);
        Assert.Equal(scenario.Id, created!.ScenarioId);

        var getResponse = await _client.GetAsync($"/api/control-zones/{created.Id}");
        var fetched = await getResponse.Content.ReadFromJsonAsync<ControlZone>(TestJson.Options);
        Assert.Equal("Center Flag", fetched!.Name);
        Assert.Equal(2, fetched.PointsPerTurn);
    }

    [Fact]
    public async Task GetForScenario_OnlyReturnsThatScenariosZones()
    {
        var scenarioA = await CreateScenarioAsync("Scenario A");
        var scenarioB = await CreateScenarioAsync("Scenario B");

        var createdResponse = await _client.PostAsJsonAsync($"/api/scenarios/{scenarioA.Id}/zones", new ControlZone { Name = "A Flag", Radius = 40 }, TestJson.Options);
        var created = await createdResponse.Content.ReadFromJsonAsync<ControlZone>(TestJson.Options);
        await _client.PostAsJsonAsync($"/api/scenarios/{scenarioB.Id}/zones", new ControlZone { Name = "B Flag", Radius = 40 }, TestJson.Options);

        var response = await _client.GetAsync($"/api/scenarios/{scenarioA.Id}/zones");
        var list = await response.Content.ReadFromJsonAsync<List<ControlZone>>(TestJson.Options);

        Assert.All(list!, z => Assert.Equal(scenarioA.Id, z.ScenarioId));
        Assert.Contains(list!, z => z.Id == created!.Id);
    }

    [Fact]
    public async Task Delete_ThenGetById_ReturnsNotFound()
    {
        var scenario = await CreateScenarioAsync("Scenario Delete");
        var createResponse = await _client.PostAsJsonAsync($"/api/scenarios/{scenario.Id}/zones", new ControlZone { Name = "Temp Flag", Radius = 40 }, TestJson.Options);
        var created = await createResponse.Content.ReadFromJsonAsync<ControlZone>(TestJson.Options);

        var deleteResponse = await _client.DeleteAsync($"/api/control-zones/{created!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await _client.GetAsync($"/api/control-zones/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }
}
