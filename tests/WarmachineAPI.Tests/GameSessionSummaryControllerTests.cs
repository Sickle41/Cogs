using System.Net;
using System.Net.Http.Json;
using WarmachineAPI.Controllers;
using WarmachineAPI.Models;

namespace WarmachineAPI.Tests;

public class GameSessionSummaryControllerTests : IClassFixture<WarmachineApiFactory>
{
    private readonly HttpClient _client;

    public GameSessionSummaryControllerTests(WarmachineApiFactory factory)
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
        var response = await _client.PostAsJsonAsync("/api/armies", new Army { Name = name, FactionId = factionId, PointLimit = 50 }, TestJson.Options);
        return (await response.Content.ReadFromJsonAsync<Army>(TestJson.Options))!;
    }

    private async Task<ArmyEntry> CreateArmyEntryAsync(Guid armyId, Guid unitId)
    {
        var response = await _client.PostAsJsonAsync($"/api/armies/{armyId}/entries", new ArmyEntry { UnitDefinitionId = unitId, Quantity = 1 }, TestJson.Options);
        return (await response.Content.ReadFromJsonAsync<ArmyEntry>(TestJson.Options))!;
    }

    private async Task<Map> CreateMapAsync(string name)
    {
        var response = await _client.PostAsJsonAsync("/api/maps", new Map { Name = name }, TestJson.Options);
        return (await response.Content.ReadFromJsonAsync<Map>(TestJson.Options))!;
    }

    private async Task<Scenario> CreateScenarioAsync(string name)
    {
        var response = await _client.PostAsJsonAsync("/api/scenarios", new Scenario { Name = name }, TestJson.Options);
        return (await response.Content.ReadFromJsonAsync<Scenario>(TestJson.Options))!;
    }

    private async Task<GameSession> CreateSessionAsync(Guid mapId, Guid? scenarioId = null)
    {
        var response = await _client.PostAsJsonAsync("/api/sessions", new GameSession { MapId = mapId, ScenarioId = scenarioId }, TestJson.Options);
        return (await response.Content.ReadFromJsonAsync<GameSession>(TestJson.Options))!;
    }

    private async Task<ModelInstance> CreateModelAsync(Guid sessionId, Guid armyEntryId)
    {
        var response = await _client.PostAsJsonAsync($"/api/sessions/{sessionId}/models", new ModelInstance { ArmyEntryId = armyEntryId }, TestJson.Options);
        return (await response.Content.ReadFromJsonAsync<ModelInstance>(TestJson.Options))!;
    }

    [Fact]
    public async Task GetSummary_NonExistentSession_ReturnsNotFound()
    {
        var response = await _client.GetAsync($"/api/sessions/{Guid.NewGuid()}/summary");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetSummary_IncludesScenarioNameWhenPresent()
    {
        var map = await CreateMapAsync("Summary Scenario Board");
        var scenario = await CreateScenarioAsync("Incursion Summary");
        var session = await CreateSessionAsync(map.Id, scenario.Id);

        var response = await _client.GetAsync($"/api/sessions/{session.Id}/summary");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var summary = await response.Content.ReadFromJsonAsync<GameSessionSummary>(TestJson.Options);
        Assert.Equal("Incursion Summary", summary!.ScenarioName);
        Assert.Equal(SessionStatus.Setup, summary.Status);
    }

    [Fact]
    public async Task GetSummary_WithoutScenario_HasNullScenarioName()
    {
        var map = await CreateMapAsync("Summary No Scenario Board");
        var session = await CreateSessionAsync(map.Id);

        var response = await _client.GetAsync($"/api/sessions/{session.Id}/summary");
        var summary = await response.Content.ReadFromJsonAsync<GameSessionSummary>(TestJson.Options);

        Assert.Null(summary!.ScenarioName);
    }

    [Fact]
    public async Task GetSummary_AggregatesModelCountsAndDamagePerParticipant()
    {
        var factionA = await CreateFactionAsync("Cygnar-Summary");
        var factionB = await CreateFactionAsync("Khador-Summary");
        var unitA = await CreateUnitAsync(factionA.Id, "Stormblade");
        var unitB = await CreateUnitAsync(factionB.Id, "Winter Guard");
        var armyA = await CreateArmyAsync(factionA.Id, "Cygnar Army");
        var armyB = await CreateArmyAsync(factionB.Id, "Khador Army");
        var entryA = await CreateArmyEntryAsync(armyA.Id, unitA.Id);
        var entryB = await CreateArmyEntryAsync(armyB.Id, unitB.Id);

        var map = await CreateMapAsync("Summary Aggregation Board");
        var session = await CreateSessionAsync(map.Id);

        await _client.PostAsJsonAsync($"/api/sessions/{session.Id}/participants", new GameParticipant { ArmyId = armyA.Id, TurnOrder = 1 }, TestJson.Options);
        await _client.PostAsJsonAsync($"/api/sessions/{session.Id}/participants", new GameParticipant { ArmyId = armyB.Id, TurnOrder = 2 }, TestJson.Options);

        var modelA1 = await CreateModelAsync(session.Id, entryA.Id);
        var modelA2 = await CreateModelAsync(session.Id, entryA.Id);
        var modelB1 = await CreateModelAsync(session.Id, entryB.Id);

        await _client.PatchAsJsonAsync($"/api/models/{modelA1.Id}/damage", new DamageUpdate { DamageTaken = 3, IsDestroyed = false }, TestJson.Options);
        await _client.PatchAsJsonAsync($"/api/models/{modelA2.Id}/damage", new DamageUpdate { DamageTaken = 5, IsDestroyed = true }, TestJson.Options);
        await _client.PatchAsJsonAsync($"/api/models/{modelB1.Id}/damage", new DamageUpdate { DamageTaken = 2, IsDestroyed = false }, TestJson.Options);

        var response = await _client.GetAsync($"/api/sessions/{session.Id}/summary");
        var summary = await response.Content.ReadFromJsonAsync<GameSessionSummary>(TestJson.Options);

        var summaryA = summary!.Participants.Single(p => p.ArmyId == armyA.Id);
        Assert.Equal("Cygnar Army", summaryA.ArmyName);
        Assert.Equal(2, summaryA.ModelCount);
        Assert.Equal(1, summaryA.ModelsDestroyed);
        Assert.Equal(8, summaryA.TotalDamageTaken);

        var summaryB = summary.Participants.Single(p => p.ArmyId == armyB.Id);
        Assert.Equal(1, summaryB.ModelCount);
        Assert.Equal(0, summaryB.ModelsDestroyed);
        Assert.Equal(2, summaryB.TotalDamageTaken);
    }

    [Fact]
    public async Task GetSummary_CountsCombatLogEntriesByEventType()
    {
        var map = await CreateMapAsync("Summary Log Board");
        var session = await CreateSessionAsync(map.Id);

        await _client.PostAsJsonAsync($"/api/sessions/{session.Id}/log", new CombatLogEntry { Round = 1, EventType = CombatLogEventType.Movement, Description = "Moved." }, TestJson.Options);
        await _client.PostAsJsonAsync($"/api/sessions/{session.Id}/log", new CombatLogEntry { Round = 1, EventType = CombatLogEventType.MeleeAttack, Description = "Attacked." }, TestJson.Options);
        await _client.PostAsJsonAsync($"/api/sessions/{session.Id}/log", new CombatLogEntry { Round = 1, EventType = CombatLogEventType.MeleeAttack, Description = "Attacked again." }, TestJson.Options);

        var response = await _client.GetAsync($"/api/sessions/{session.Id}/summary");
        var summary = await response.Content.ReadFromJsonAsync<GameSessionSummary>(TestJson.Options);

        Assert.Equal(1, summary!.LogEntryCountsByType[CombatLogEventType.Movement]);
        Assert.Equal(2, summary.LogEntryCountsByType[CombatLogEventType.MeleeAttack]);
    }
}
