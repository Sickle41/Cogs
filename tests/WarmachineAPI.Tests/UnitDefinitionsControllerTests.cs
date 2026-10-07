using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using WarmachineAPI.Models;

namespace WarmachineAPI.Tests;

public class UnitDefinitionsControllerTests : IClassFixture<WarmachineApiFactory>
{
    private readonly HttpClient _client;

    public UnitDefinitionsControllerTests(WarmachineApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<Faction> CreateFactionAsync(string name)
    {
        var response = await _client.PostAsJsonAsync("/api/factions", new Faction { Name = name }, TestJson.Options);
        return (await response.Content.ReadFromJsonAsync<Faction>(TestJson.Options))!;
    }

    private static UnitDefinition BuildUnit(Guid factionId, string name) => new()
    {
        FactionId = factionId,
        Name = name,
        Category = UnitCategory.Unit,
        PointCost = 3,
        BaseSize = BaseSize.Medium,
        Speed = 6,
        MeleeAttack = 6,
        RangedAttack = 0,
        ArcaneAttack = 0,
        Defense = 13,
        Armor = 15,
        DamageCapacity = 5,
        IsCharacter = false
    };

    [Fact]
    public async Task Create_WithValidFaction_ReturnsCreated()
    {
        var faction = await CreateFactionAsync("Cygnar");
        var unit = BuildUnit(faction.Id, "Stormblade");

        var response = await _client.PostAsJsonAsync("/api/units", unit, TestJson.Options);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<UnitDefinition>(TestJson.Options);
        Assert.Equal(UnitCategory.Unit, created!.Category);
        Assert.Equal(BaseSize.Medium, created.BaseSize);
    }

    [Fact]
    public async Task Create_WithInvalidFaction_ReturnsBadRequest()
    {
        var unit = BuildUnit(Guid.NewGuid(), "Ghost Unit");

        var response = await _client.PostAsJsonAsync("/api/units", unit, TestJson.Options);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestJson.Options);
        Assert.Equal(400, problem!.Status);
        Assert.Contains("does not exist", problem.Detail);
    }

    [Fact]
    public async Task GetAll_FilteredByFaction_OnlyReturnsThatFactionsUnits()
    {
        var cygnar = await CreateFactionAsync("Cygnar-Filter");
        var khador = await CreateFactionAsync("Khador-Filter");

        var cygnarUnitResponse = await _client.PostAsJsonAsync("/api/units", BuildUnit(cygnar.Id, "Stormblade"), TestJson.Options);
        var cygnarUnit = await cygnarUnitResponse.Content.ReadFromJsonAsync<UnitDefinition>(TestJson.Options);
        await _client.PostAsJsonAsync("/api/units", BuildUnit(khador.Id, "Winter Guard"), TestJson.Options);

        var response = await _client.GetAsync($"/api/units?factionId={cygnar.Id}");
        var page = await response.Content.ReadFromJsonAsync<PagedResult<UnitDefinition>>(TestJson.Options);

        Assert.All(page!.Items, u => Assert.Equal(cygnar.Id, u.FactionId));
        Assert.Contains(page.Items, u => u.Id == cygnarUnit!.Id);
    }

    [Fact]
    public async Task Delete_ThenGetById_ReturnsNotFound()
    {
        var faction = await CreateFactionAsync("Protectorate");
        var createResponse = await _client.PostAsJsonAsync("/api/units", BuildUnit(faction.Id, "Temple Flameguard"), TestJson.Options);
        var created = await createResponse.Content.ReadFromJsonAsync<UnitDefinition>(TestJson.Options);

        var deleteResponse = await _client.DeleteAsync($"/api/units/{created!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await _client.GetAsync($"/api/units/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task Create_WithDamageColumns_RoundTripsTemplate()
    {
        var faction = await CreateFactionAsync("Cygnar-Jack");
        var jack = BuildUnit(faction.Id, "Ironclad");
        jack.Category = UnitCategory.Warjack;
        jack.DamageColumns = new List<DamageColumnTemplate>
        {
            new() { Name = "Left Arm", TotalBoxes = 6 },
            new() { Name = "Right Arm", TotalBoxes = 6 },
            new() { Name = "Cortex", TotalBoxes = 5 }
        };

        var response = await _client.PostAsJsonAsync("/api/units", jack, TestJson.Options);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<UnitDefinition>(TestJson.Options);
        Assert.Equal(3, created!.DamageColumns.Count);
        Assert.Contains(created.DamageColumns, c => c.Name == "Cortex" && c.TotalBoxes == 5);
    }

    [Fact]
    public async Task Create_WithNegativePointCost_ReturnsBadRequest()
    {
        var faction = await CreateFactionAsync("Cygnar-Negative-Cost");
        var unit = BuildUnit(faction.Id, "Underpriced Unit");
        unit.PointCost = -1;

        var response = await _client.PostAsJsonAsync("/api/units", unit, TestJson.Options);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
