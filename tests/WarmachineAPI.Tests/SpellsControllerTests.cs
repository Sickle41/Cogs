using System.Net;
using System.Net.Http.Json;
using WarmachineAPI.Models;

namespace WarmachineAPI.Tests;

public class SpellsControllerTests : IClassFixture<WarmachineApiFactory>
{
    private readonly HttpClient _client;

    public SpellsControllerTests(WarmachineApiFactory factory)
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
        var unit = new UnitDefinition { FactionId = factionId, Name = name, Category = UnitCategory.Warcaster, PointCost = 0 };
        var response = await _client.PostAsJsonAsync("/api/units", unit, TestJson.Options);
        return (await response.Content.ReadFromJsonAsync<UnitDefinition>(TestJson.Options))!;
    }

    [Fact]
    public async Task Create_WithInvalidUnit_ReturnsBadRequest()
    {
        var spell = new Spell { Name = "Fireball", Cost = 3, Range = 10, Aoe = 4, Pow = 12 };

        var response = await _client.PostAsJsonAsync($"/api/units/{Guid.NewGuid()}/spells", spell, TestJson.Options);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_ThenGet_RoundTripsSpell()
    {
        var faction = await CreateFactionAsync("Cygnar-Spell");
        var caster = await CreateUnitAsync(faction.Id, "Commander Coleman Stryker");

        var spell = new Spell { Name = "Arcane Bolt", Cost = 2, Range = 10, Aoe = 0, Pow = 10, IsUpkeep = false, Description = "A bolt of raw energy." };
        var createResponse = await _client.PostAsJsonAsync($"/api/units/{caster.Id}/spells", spell, TestJson.Options);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content.ReadFromJsonAsync<Spell>(TestJson.Options);
        Assert.Equal(caster.Id, created!.UnitDefinitionId);

        var getResponse = await _client.GetAsync($"/api/spells/{created.Id}");
        var fetched = await getResponse.Content.ReadFromJsonAsync<Spell>(TestJson.Options);
        Assert.Equal("Arcane Bolt", fetched!.Name);
        Assert.Equal(10, fetched.Pow);
    }

    [Fact]
    public async Task GetForUnit_OnlyReturnsThatUnitsSpells()
    {
        var faction = await CreateFactionAsync("Cygnar-Spell-List");
        var casterA = await CreateUnitAsync(faction.Id, "Caster A");
        var casterB = await CreateUnitAsync(faction.Id, "Caster B");

        var createdResponse = await _client.PostAsJsonAsync($"/api/units/{casterA.Id}/spells", new Spell { Name = "Boundless Charge", Cost = 3 }, TestJson.Options);
        var created = await createdResponse.Content.ReadFromJsonAsync<Spell>(TestJson.Options);
        await _client.PostAsJsonAsync($"/api/units/{casterB.Id}/spells", new Spell { Name = "Snipe", Cost = 2 }, TestJson.Options);

        var response = await _client.GetAsync($"/api/units/{casterA.Id}/spells");
        var list = await response.Content.ReadFromJsonAsync<List<Spell>>(TestJson.Options);

        Assert.All(list!, s => Assert.Equal(casterA.Id, s.UnitDefinitionId));
        Assert.Contains(list!, s => s.Id == created!.Id);
    }

    [Fact]
    public async Task Delete_ThenGetById_ReturnsNotFound()
    {
        var faction = await CreateFactionAsync("Cygnar-Spell-Delete");
        var caster = await CreateUnitAsync(faction.Id, "Caster Delete");

        var createResponse = await _client.PostAsJsonAsync($"/api/units/{caster.Id}/spells", new Spell { Name = "Temporary Spell", Cost = 1 }, TestJson.Options);
        var created = await createResponse.Content.ReadFromJsonAsync<Spell>(TestJson.Options);

        var deleteResponse = await _client.DeleteAsync($"/api/spells/{created!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await _client.GetAsync($"/api/spells/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }
}
