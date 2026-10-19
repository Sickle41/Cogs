using System.Net;
using System.Net.Http.Json;
using WarmachineAPI.Models;

namespace WarmachineAPI.Tests;

public class FeatsControllerTests : IClassFixture<WarmachineApiFactory>
{
    private readonly HttpClient _client;

    public FeatsControllerTests(WarmachineApiFactory factory)
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
        var feat = new Feat { Name = "Feedback", Description = "Punishing recourse." };

        var response = await _client.PostAsJsonAsync($"/api/units/{Guid.NewGuid()}/feats", feat, TestJson.Options);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_ThenGet_RoundTripsFeat()
    {
        var faction = await CreateFactionAsync("Cygnar-Feat");
        var caster = await CreateUnitAsync(faction.Id, "Commander Coleman Stryker");

        var feat = new Feat { Name = "Last Stand", Description = "Transfer damage away from Stryker." };
        var createResponse = await _client.PostAsJsonAsync($"/api/units/{caster.Id}/feats", feat, TestJson.Options);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content.ReadFromJsonAsync<Feat>(TestJson.Options);
        Assert.Equal(caster.Id, created!.UnitDefinitionId);

        var getResponse = await _client.GetAsync($"/api/feats/{created.Id}");
        var fetched = await getResponse.Content.ReadFromJsonAsync<Feat>(TestJson.Options);
        Assert.Equal("Last Stand", fetched!.Name);
    }

    [Fact]
    public async Task GetForUnit_OnlyReturnsThatUnitsFeats()
    {
        var faction = await CreateFactionAsync("Cygnar-Feat-List");
        var casterA = await CreateUnitAsync(faction.Id, "Caster A");
        var casterB = await CreateUnitAsync(faction.Id, "Caster B");

        var createdResponse = await _client.PostAsJsonAsync($"/api/units/{casterA.Id}/feats", new Feat { Name = "Feat A" }, TestJson.Options);
        var created = await createdResponse.Content.ReadFromJsonAsync<Feat>(TestJson.Options);
        await _client.PostAsJsonAsync($"/api/units/{casterB.Id}/feats", new Feat { Name = "Feat B" }, TestJson.Options);

        var response = await _client.GetAsync($"/api/units/{casterA.Id}/feats");
        var list = await response.Content.ReadFromJsonAsync<List<Feat>>(TestJson.Options);

        Assert.All(list!, f => Assert.Equal(casterA.Id, f.UnitDefinitionId));
        Assert.Contains(list!, f => f.Id == created!.Id);
    }

    [Fact]
    public async Task Delete_ThenGetById_ReturnsNotFound()
    {
        var faction = await CreateFactionAsync("Cygnar-Feat-Delete");
        var caster = await CreateUnitAsync(faction.Id, "Caster Delete");

        var createResponse = await _client.PostAsJsonAsync($"/api/units/{caster.Id}/feats", new Feat { Name = "Temporary Feat" }, TestJson.Options);
        var created = await createResponse.Content.ReadFromJsonAsync<Feat>(TestJson.Options);

        var deleteResponse = await _client.DeleteAsync($"/api/feats/{created!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await _client.GetAsync($"/api/feats/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }
}
