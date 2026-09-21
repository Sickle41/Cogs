using System.Net;
using System.Net.Http.Json;
using WarmachineAPI.Models;

namespace WarmachineAPI.Tests;

public class ArmiesControllerTests : IClassFixture<WarmachineApiFactory>
{
    private readonly HttpClient _client;

    public ArmiesControllerTests(WarmachineApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<Faction> CreateFactionAsync(string name)
    {
        var response = await _client.PostAsJsonAsync("/api/factions", new Faction { Name = name }, TestJson.Options);
        return (await response.Content.ReadFromJsonAsync<Faction>(TestJson.Options))!;
    }

    [Fact]
    public async Task Create_WithInvalidFaction_ReturnsBadRequest()
    {
        var army = new Army { Name = "Ghost Army", FactionId = Guid.NewGuid(), PointLimit = 50 };

        var response = await _client.PostAsJsonAsync("/api/armies", army, TestJson.Options);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithValidFaction_ReturnsCreated()
    {
        var faction = await CreateFactionAsync("Cygnar-Army");
        var army = new Army { Name = "Stryker's Command", FactionId = faction.Id, PointLimit = 50 };

        var response = await _client.PostAsJsonAsync("/api/armies", army, TestJson.Options);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<Army>(TestJson.Options);
        Assert.Equal(faction.Id, created!.FactionId);
        Assert.Equal(50, created.PointLimit);
    }

    [Fact]
    public async Task Delete_ThenGetById_ReturnsNotFound()
    {
        var faction = await CreateFactionAsync("Khador-Army");
        var createResponse = await _client.PostAsJsonAsync("/api/armies", new Army { Name = "Winter's Wrath", FactionId = faction.Id, PointLimit = 75 }, TestJson.Options);
        var created = await createResponse.Content.ReadFromJsonAsync<Army>(TestJson.Options);

        var deleteResponse = await _client.DeleteAsync($"/api/armies/{created!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await _client.GetAsync($"/api/armies/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }
}
