using System.Net;
using System.Net.Http.Json;
using WarmachineAPI.Models;

namespace WarmachineAPI.Tests;

public class ArmyEntriesControllerTests : IClassFixture<WarmachineApiFactory>
{
    private readonly HttpClient _client;

    public ArmyEntriesControllerTests(WarmachineApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<Faction> CreateFactionAsync(string name)
    {
        var response = await _client.PostAsJsonAsync("/api/factions", new Faction { Name = name }, TestJson.Options);
        return (await response.Content.ReadFromJsonAsync<Faction>(TestJson.Options))!;
    }

    private async Task<UnitDefinition> CreateUnitAsync(Guid factionId, string name)
    {
        var unit = new UnitDefinition { FactionId = factionId, Name = name, Category = UnitCategory.Unit, PointCost = 3 };
        var response = await _client.PostAsJsonAsync("/api/units", unit, TestJson.Options);
        return (await response.Content.ReadFromJsonAsync<UnitDefinition>(TestJson.Options))!;
    }

    private async Task<Army> CreateArmyAsync(Guid factionId, string name)
    {
        var army = new Army { Name = name, FactionId = factionId, PointLimit = 50 };
        var response = await _client.PostAsJsonAsync("/api/armies", army, TestJson.Options);
        return (await response.Content.ReadFromJsonAsync<Army>(TestJson.Options))!;
    }

    [Fact]
    public async Task Create_WithCrossFactionUnit_ReturnsBadRequest()
    {
        var cygnar = await CreateFactionAsync("Cygnar-Entry");
        var khador = await CreateFactionAsync("Khador-Entry");
        var army = await CreateArmyAsync(cygnar.Id, "Cygnar List");
        var khadorUnit = await CreateUnitAsync(khador.Id, "Winter Guard");

        var entry = new ArmyEntry { UnitDefinitionId = khadorUnit.Id, Quantity = 6 };
        var response = await _client.PostAsJsonAsync($"/api/armies/{army.Id}/entries", entry, TestJson.Options);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithMatchingFactionUnit_ReturnsCreated()
    {
        var faction = await CreateFactionAsync("Cygnar-Entry-Match");
        var army = await CreateArmyAsync(faction.Id, "Cygnar List Match");
        var unit = await CreateUnitAsync(faction.Id, "Stormblade");

        var entry = new ArmyEntry { UnitDefinitionId = unit.Id, Quantity = 6 };
        var response = await _client.PostAsJsonAsync($"/api/armies/{army.Id}/entries", entry, TestJson.Options);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<ArmyEntry>(TestJson.Options);
        Assert.Equal(army.Id, created!.ArmyId);
        Assert.Equal(6, created.Quantity);
    }

    [Fact]
    public async Task GetForArmy_ReturnsOnlyThatArmysEntries()
    {
        var faction = await CreateFactionAsync("Cygnar-Entry-List");
        var armyA = await CreateArmyAsync(faction.Id, "List A");
        var armyB = await CreateArmyAsync(faction.Id, "List B");
        var unit = await CreateUnitAsync(faction.Id, "Long Gunners");

        var createdResponse = await _client.PostAsJsonAsync($"/api/armies/{armyA.Id}/entries", new ArmyEntry { UnitDefinitionId = unit.Id, Quantity = 6 }, TestJson.Options);
        var created = await createdResponse.Content.ReadFromJsonAsync<ArmyEntry>(TestJson.Options);
        await _client.PostAsJsonAsync($"/api/armies/{armyB.Id}/entries", new ArmyEntry { UnitDefinitionId = unit.Id, Quantity = 3 }, TestJson.Options);

        var response = await _client.GetAsync($"/api/armies/{armyA.Id}/entries");
        var list = await response.Content.ReadFromJsonAsync<List<ArmyEntry>>(TestJson.Options);

        Assert.All(list!, e => Assert.Equal(armyA.Id, e.ArmyId));
        Assert.Contains(list!, e => e.Id == created!.Id);
    }

    [Fact]
    public async Task Delete_ThenGetById_ReturnsNotFound()
    {
        var faction = await CreateFactionAsync("Cygnar-Entry-Delete");
        var army = await CreateArmyAsync(faction.Id, "List Delete");
        var unit = await CreateUnitAsync(faction.Id, "Sword Knights");

        var createResponse = await _client.PostAsJsonAsync($"/api/armies/{army.Id}/entries", new ArmyEntry { UnitDefinitionId = unit.Id, Quantity = 6 }, TestJson.Options);
        var created = await createResponse.Content.ReadFromJsonAsync<ArmyEntry>(TestJson.Options);

        var deleteResponse = await _client.DeleteAsync($"/api/army-entries/{created!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await _client.GetAsync($"/api/army-entries/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }
}
