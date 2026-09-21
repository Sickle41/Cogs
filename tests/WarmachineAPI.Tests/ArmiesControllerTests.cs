using System.Net;
using System.Net.Http.Json;
using WarmachineAPI.Controllers;
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

    private async Task<UnitDefinition> CreateUnitAsync(Guid factionId, string name, int pointCost)
    {
        var unit = new UnitDefinition { FactionId = factionId, Name = name, Category = UnitCategory.Unit, PointCost = pointCost };
        var response = await _client.PostAsJsonAsync("/api/units", unit, TestJson.Options);
        return (await response.Content.ReadFromJsonAsync<UnitDefinition>(TestJson.Options))!;
    }

    private async Task AddArmyEntryAsync(Guid armyId, Guid unitId, int quantity)
    {
        await _client.PostAsJsonAsync($"/api/armies/{armyId}/entries", new ArmyEntry { UnitDefinitionId = unitId, Quantity = quantity }, TestJson.Options);
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

    [Fact]
    public async Task PointsSummary_WithNoEntries_ReturnsZeroTotal()
    {
        var faction = await CreateFactionAsync("Cygnar-Points-Empty");
        var createResponse = await _client.PostAsJsonAsync("/api/armies", new Army { Name = "Empty List", FactionId = faction.Id, PointLimit = 50 }, TestJson.Options);
        var army = await createResponse.Content.ReadFromJsonAsync<Army>(TestJson.Options);

        var response = await _client.GetAsync($"/api/armies/{army!.Id}/points-summary");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var summary = await response.Content.ReadFromJsonAsync<ArmyPointsSummary>(TestJson.Options);
        Assert.Equal(0, summary!.TotalPoints);
        Assert.Equal(50, summary.RemainingPoints);
        Assert.False(summary.IsOverLimit);
    }

    [Fact]
    public async Task PointsSummary_UnderLimit_ComputesRemainingCorrectly()
    {
        var faction = await CreateFactionAsync("Cygnar-Points-Under");
        var createResponse = await _client.PostAsJsonAsync("/api/armies", new Army { Name = "Under Limit List", FactionId = faction.Id, PointLimit = 50 }, TestJson.Options);
        var army = await createResponse.Content.ReadFromJsonAsync<Army>(TestJson.Options);

        var unit = await CreateUnitAsync(faction.Id, "Stormblade", 3);
        await AddArmyEntryAsync(army!.Id, unit.Id, 6); // 18 points

        var response = await _client.GetAsync($"/api/armies/{army.Id}/points-summary");
        var summary = await response.Content.ReadFromJsonAsync<ArmyPointsSummary>(TestJson.Options);

        Assert.Equal(18, summary!.TotalPoints);
        Assert.Equal(32, summary.RemainingPoints);
        Assert.False(summary.IsOverLimit);
    }

    [Fact]
    public async Task PointsSummary_OverLimit_FlagsIsOverLimit()
    {
        var faction = await CreateFactionAsync("Cygnar-Points-Over");
        var createResponse = await _client.PostAsJsonAsync("/api/armies", new Army { Name = "Over Limit List", FactionId = faction.Id, PointLimit = 10 }, TestJson.Options);
        var army = await createResponse.Content.ReadFromJsonAsync<Army>(TestJson.Options);

        var unit = await CreateUnitAsync(faction.Id, "Stormblade", 3);
        await AddArmyEntryAsync(army!.Id, unit.Id, 6); // 18 points, over the 10 limit

        var response = await _client.GetAsync($"/api/armies/{army.Id}/points-summary");
        var summary = await response.Content.ReadFromJsonAsync<ArmyPointsSummary>(TestJson.Options);

        Assert.Equal(18, summary!.TotalPoints);
        Assert.Equal(-8, summary.RemainingPoints);
        Assert.True(summary.IsOverLimit);
    }

    [Fact]
    public async Task PointsSummary_NonExistentArmy_ReturnsNotFound()
    {
        var response = await _client.GetAsync($"/api/armies/{Guid.NewGuid()}/points-summary");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
