using System.Net;
using System.Net.Http.Json;
using WarmachineAPI.Models;

namespace WarmachineAPI.Tests;

public class CombatLogControllerTests : IClassFixture<WarmachineApiFactory>
{
    private readonly HttpClient _client;

    public CombatLogControllerTests(WarmachineApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<Map> CreateMapAsync(string name)
    {
        var response = await _client.PostAsJsonAsync("/api/maps", new Map { Name = name }, TestJson.Options);
        return (await response.Content.ReadFromJsonAsync<Map>(TestJson.Options))!;
    }

    private async Task<GameSession> CreateSessionAsync(Guid mapId)
    {
        var response = await _client.PostAsJsonAsync("/api/sessions", new GameSession { MapId = mapId }, TestJson.Options);
        return (await response.Content.ReadFromJsonAsync<GameSession>(TestJson.Options))!;
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
        var response = await _client.PostAsJsonAsync("/api/armies", new Army { Name = name, FactionId = factionId, PointLimit = 50 }, TestJson.Options);
        return (await response.Content.ReadFromJsonAsync<Army>(TestJson.Options))!;
    }

    private async Task<ArmyEntry> CreateArmyEntryAsync(Guid armyId, Guid unitId)
    {
        var response = await _client.PostAsJsonAsync($"/api/armies/{armyId}/entries", new ArmyEntry { UnitDefinitionId = unitId, Quantity = 6 }, TestJson.Options);
        return (await response.Content.ReadFromJsonAsync<ArmyEntry>(TestJson.Options))!;
    }

    private async Task<ModelInstance> CreateModelAsync(Guid sessionId, string suffix)
    {
        var faction = await CreateFactionAsync($"Cygnar-Log-{suffix}");
        var unit = await CreateUnitAsync(faction.Id, "Stormblade");
        var army = await CreateArmyAsync(faction.Id, "Stryker's Command");
        var entry = await CreateArmyEntryAsync(army.Id, unit.Id);

        var response = await _client.PostAsJsonAsync($"/api/sessions/{sessionId}/models", new ModelInstance { ArmyEntryId = entry.Id }, TestJson.Options);
        return (await response.Content.ReadFromJsonAsync<ModelInstance>(TestJson.Options))!;
    }

    [Fact]
    public async Task Create_WithInvalidSession_ReturnsBadRequest()
    {
        var entry = new CombatLogEntry { Round = 1, EventType = CombatLogEventType.Custom, Description = "Orphan event" };

        var response = await _client.PostAsJsonAsync($"/api/sessions/{Guid.NewGuid()}/log", entry, TestJson.Options);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithInvalidActor_ReturnsBadRequest()
    {
        var map = await CreateMapAsync("Log Bad Actor Board");
        var session = await CreateSessionAsync(map.Id);

        var entry = new CombatLogEntry { Round = 1, EventType = CombatLogEventType.Movement, ActorModelInstanceId = Guid.NewGuid() };
        var response = await _client.PostAsJsonAsync($"/api/sessions/{session.Id}/log", entry, TestJson.Options);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_ThenGetForSession_ReturnsEntryInOrder()
    {
        var map = await CreateMapAsync("Log Board");
        var session = await CreateSessionAsync(map.Id);
        var actor = await CreateModelAsync(session.Id, "Actor");
        var target = await CreateModelAsync(session.Id, "Target");

        var firstEntry = new CombatLogEntry { Round = 1, EventType = CombatLogEventType.Movement, ActorModelInstanceId = actor.Id, Description = "Moved forward." };
        var firstResponse = await _client.PostAsJsonAsync($"/api/sessions/{session.Id}/log", firstEntry, TestJson.Options);
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);
        var firstCreated = await firstResponse.Content.ReadFromJsonAsync<CombatLogEntry>(TestJson.Options);
        Assert.Equal(session.Id, firstCreated!.GameSessionId);

        var secondEntry = new CombatLogEntry { Round = 1, EventType = CombatLogEventType.MeleeAttack, ActorModelInstanceId = actor.Id, TargetModelInstanceId = target.Id, Description = "Swung sword." };
        await _client.PostAsJsonAsync($"/api/sessions/{session.Id}/log", secondEntry, TestJson.Options);

        var response = await _client.GetAsync($"/api/sessions/{session.Id}/log");
        var list = await response.Content.ReadFromJsonAsync<List<CombatLogEntry>>(TestJson.Options);

        Assert.Equal(2, list!.Count);
        Assert.Equal(CombatLogEventType.Movement, list[0].EventType);
        Assert.Equal(CombatLogEventType.MeleeAttack, list[1].EventType);
        Assert.Equal(target.Id, list[1].TargetModelInstanceId);
    }

    [Fact]
    public async Task GetForSession_NonExistentSession_ReturnsNotFound()
    {
        var response = await _client.GetAsync($"/api/sessions/{Guid.NewGuid()}/log");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
