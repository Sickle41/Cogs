using System.Net;
using System.Net.Http.Json;
using WarmachineAPI.Models;

namespace WarmachineAPI.Tests;

public class AbilitiesControllerTests : IClassFixture<WarmachineApiFactory>
{
    private readonly HttpClient _client;

    public AbilitiesControllerTests(WarmachineApiFactory factory)
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

    [Fact]
    public async Task Create_WithInvalidUnit_ReturnsBadRequest()
    {
        var ability = new Ability { Name = "Pathfinder", Description = "Ignores rough terrain." };

        var response = await _client.PostAsJsonAsync($"/api/units/{Guid.NewGuid()}/abilities", ability, TestJson.Options);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_ThenGet_RoundTripsAbility()
    {
        var faction = await CreateFactionAsync("Cygnar-Ability");
        var unit = await CreateUnitAsync(faction.Id, "Stormblade");

        var ability = new Ability { Name = "Pathfinder", Description = "Ignores rough terrain." };
        var createResponse = await _client.PostAsJsonAsync($"/api/units/{unit.Id}/abilities", ability, TestJson.Options);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content.ReadFromJsonAsync<Ability>(TestJson.Options);
        Assert.Equal(unit.Id, created!.UnitDefinitionId);

        var getResponse = await _client.GetAsync($"/api/abilities/{created.Id}");
        var fetched = await getResponse.Content.ReadFromJsonAsync<Ability>(TestJson.Options);
        Assert.Equal("Pathfinder", fetched!.Name);
    }

    [Fact]
    public async Task GetForUnit_OnlyReturnsThatUnitsAbilities()
    {
        var faction = await CreateFactionAsync("Cygnar-Ability-List");
        var unitA = await CreateUnitAsync(faction.Id, "Unit A");
        var unitB = await CreateUnitAsync(faction.Id, "Unit B");

        var createdResponse = await _client.PostAsJsonAsync($"/api/units/{unitA.Id}/abilities", new Ability { Name = "Shield Wall" }, TestJson.Options);
        var created = await createdResponse.Content.ReadFromJsonAsync<Ability>(TestJson.Options);
        await _client.PostAsJsonAsync($"/api/units/{unitB.Id}/abilities", new Ability { Name = "Reach" }, TestJson.Options);

        var response = await _client.GetAsync($"/api/units/{unitA.Id}/abilities");
        var list = await response.Content.ReadFromJsonAsync<List<Ability>>(TestJson.Options);

        Assert.All(list!, a => Assert.Equal(unitA.Id, a.UnitDefinitionId));
        Assert.Contains(list!, a => a.Id == created!.Id);
    }

    [Fact]
    public async Task Delete_ThenGetById_ReturnsNotFound()
    {
        var faction = await CreateFactionAsync("Cygnar-Ability-Delete");
        var unit = await CreateUnitAsync(faction.Id, "Unit Delete");

        var createResponse = await _client.PostAsJsonAsync($"/api/units/{unit.Id}/abilities", new Ability { Name = "Temporary Ability" }, TestJson.Options);
        var created = await createResponse.Content.ReadFromJsonAsync<Ability>(TestJson.Options);

        var deleteResponse = await _client.DeleteAsync($"/api/abilities/{created!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await _client.GetAsync($"/api/abilities/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }
}
