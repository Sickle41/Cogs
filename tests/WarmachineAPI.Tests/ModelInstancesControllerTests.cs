using System.Net;
using System.Net.Http.Json;
using WarmachineAPI.Controllers;
using WarmachineAPI.Models;

namespace WarmachineAPI.Tests;

public class ModelInstancesControllerTests : IClassFixture<WarmachineApiFactory>
{
    private readonly HttpClient _client;

    public ModelInstancesControllerTests(WarmachineApiFactory factory)
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
        var response = await _client.PostAsJsonAsync($"/api/armies/{armyId}/entries", new ArmyEntry { UnitDefinitionId = unitId, Quantity = 6 }, TestJson.Options);
        return (await response.Content.ReadFromJsonAsync<ArmyEntry>(TestJson.Options))!;
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

    private async Task<ArmyEntry> CreateFullChainArmyEntryAsync(string suffix)
    {
        var faction = await CreateFactionAsync($"Cygnar-Model-{suffix}");
        var unit = await CreateUnitAsync(faction.Id, "Stormblade");
        var army = await CreateArmyAsync(faction.Id, "Stryker's Command");
        return await CreateArmyEntryAsync(army.Id, unit.Id);
    }

    [Fact]
    public async Task Create_WithInvalidSession_ReturnsBadRequest()
    {
        var entry = await CreateFullChainArmyEntryAsync("BadSession");
        var model = new ModelInstance { ArmyEntryId = entry.Id, X = 0, Y = 0 };

        var response = await _client.PostAsJsonAsync($"/api/sessions/{Guid.NewGuid()}/models", model, TestJson.Options);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithInvalidArmyEntry_ReturnsBadRequest()
    {
        var map = await CreateMapAsync("Model Bad Entry Board");
        var session = await CreateSessionAsync(map.Id);

        var model = new ModelInstance { ArmyEntryId = Guid.NewGuid(), X = 0, Y = 0 };
        var response = await _client.PostAsJsonAsync($"/api/sessions/{session.Id}/models", model, TestJson.Options);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_ThenUpdatePosition_ReflectsNewCoordinates()
    {
        var map = await CreateMapAsync("Model Position Board");
        var session = await CreateSessionAsync(map.Id);
        var entry = await CreateFullChainArmyEntryAsync("Position");

        var createResponse = await _client.PostAsJsonAsync($"/api/sessions/{session.Id}/models", new ModelInstance { ArmyEntryId = entry.Id, X = 100, Y = 100, Facing = 0 }, TestJson.Options);
        var created = await createResponse.Content.ReadFromJsonAsync<ModelInstance>(TestJson.Options);

        var positionUpdate = new PositionUpdate { X = 150, Y = 120, Facing = 90 };
        var patchResponse = await _client.PatchAsJsonAsync($"/api/models/{created!.Id}/position", positionUpdate, TestJson.Options);
        Assert.Equal(HttpStatusCode.NoContent, patchResponse.StatusCode);

        var getResponse = await _client.GetAsync($"/api/models/{created.Id}");
        var fetched = await getResponse.Content.ReadFromJsonAsync<ModelInstance>(TestJson.Options);
        Assert.Equal(150, fetched!.X);
        Assert.Equal(120, fetched.Y);
        Assert.Equal(90, fetched.Facing);
    }

    [Fact]
    public async Task Create_ThenUpdateDamage_ReflectsDamageAndStatusEffects()
    {
        var map = await CreateMapAsync("Model Damage Board");
        var session = await CreateSessionAsync(map.Id);
        var entry = await CreateFullChainArmyEntryAsync("Damage");

        var createResponse = await _client.PostAsJsonAsync($"/api/sessions/{session.Id}/models", new ModelInstance { ArmyEntryId = entry.Id }, TestJson.Options);
        var created = await createResponse.Content.ReadFromJsonAsync<ModelInstance>(TestJson.Options);

        var damageUpdate = new DamageUpdate { DamageTaken = 3, IsDestroyed = false, StatusEffects = new List<StatusEffect> { StatusEffect.KnockedDown } };
        var patchResponse = await _client.PatchAsJsonAsync($"/api/models/{created!.Id}/damage", damageUpdate, TestJson.Options);
        Assert.Equal(HttpStatusCode.NoContent, patchResponse.StatusCode);

        var getResponse = await _client.GetAsync($"/api/models/{created.Id}");
        var fetched = await getResponse.Content.ReadFromJsonAsync<ModelInstance>(TestJson.Options);
        Assert.Equal(3, fetched!.DamageTaken);
        Assert.Contains(StatusEffect.KnockedDown, fetched.StatusEffects);
    }

    [Fact]
    public async Task Create_ThenUpdateDamageGrid_ReflectsColumnState()
    {
        var map = await CreateMapAsync("Model Damage Grid Board");
        var session = await CreateSessionAsync(map.Id);
        var entry = await CreateFullChainArmyEntryAsync("DamageGrid");

        var createResponse = await _client.PostAsJsonAsync($"/api/sessions/{session.Id}/models", new ModelInstance { ArmyEntryId = entry.Id }, TestJson.Options);
        var created = await createResponse.Content.ReadFromJsonAsync<ModelInstance>(TestJson.Options);

        var gridUpdate = new List<DamageColumnState>
        {
            new() { Name = "Left Arm", BoxesFilled = 2 },
            new() { Name = "Cortex", BoxesFilled = 1 }
        };
        var patchResponse = await _client.PatchAsJsonAsync($"/api/models/{created!.Id}/damage-grid", gridUpdate, TestJson.Options);
        Assert.Equal(HttpStatusCode.NoContent, patchResponse.StatusCode);

        var getResponse = await _client.GetAsync($"/api/models/{created.Id}");
        var fetched = await getResponse.Content.ReadFromJsonAsync<ModelInstance>(TestJson.Options);
        Assert.Equal(2, fetched!.DamageGrid.Count);
        Assert.Contains(fetched.DamageGrid, c => c.Name == "Left Arm" && c.BoxesFilled == 2);
        Assert.Contains(fetched.DamageGrid, c => c.Name == "Cortex" && c.BoxesFilled == 1);
    }

    [Fact]
    public async Task Create_ThenUpdateActivation_ReflectsHasActivated()
    {
        var map = await CreateMapAsync("Model Activation Board");
        var session = await CreateSessionAsync(map.Id);
        var entry = await CreateFullChainArmyEntryAsync("Activation");

        var createResponse = await _client.PostAsJsonAsync($"/api/sessions/{session.Id}/models", new ModelInstance { ArmyEntryId = entry.Id }, TestJson.Options);
        var created = await createResponse.Content.ReadFromJsonAsync<ModelInstance>(TestJson.Options);
        Assert.False(created!.HasActivated);

        var patchResponse = await _client.PatchAsJsonAsync($"/api/models/{created.Id}/activation", new ActivationUpdate { HasActivated = true }, TestJson.Options);
        Assert.Equal(HttpStatusCode.NoContent, patchResponse.StatusCode);

        var getResponse = await _client.GetAsync($"/api/models/{created.Id}");
        var fetched = await getResponse.Content.ReadFromJsonAsync<ModelInstance>(TestJson.Options);
        Assert.True(fetched!.HasActivated);
    }

    [Fact]
    public async Task ResetActivations_SetsAllModelsInSessionToNotActivated()
    {
        var map = await CreateMapAsync("Model Reset Activation Board");
        var session = await CreateSessionAsync(map.Id);
        var entryA = await CreateFullChainArmyEntryAsync("ResetA");
        var entryB = await CreateFullChainArmyEntryAsync("ResetB");

        var createAResponse = await _client.PostAsJsonAsync($"/api/sessions/{session.Id}/models", new ModelInstance { ArmyEntryId = entryA.Id }, TestJson.Options);
        var createdA = await createAResponse.Content.ReadFromJsonAsync<ModelInstance>(TestJson.Options);
        var createBResponse = await _client.PostAsJsonAsync($"/api/sessions/{session.Id}/models", new ModelInstance { ArmyEntryId = entryB.Id }, TestJson.Options);
        var createdB = await createBResponse.Content.ReadFromJsonAsync<ModelInstance>(TestJson.Options);

        await _client.PatchAsJsonAsync($"/api/models/{createdA!.Id}/activation", new ActivationUpdate { HasActivated = true }, TestJson.Options);
        await _client.PatchAsJsonAsync($"/api/models/{createdB!.Id}/activation", new ActivationUpdate { HasActivated = true }, TestJson.Options);

        var resetResponse = await _client.PostAsync($"/api/sessions/{session.Id}/reset-activations", null);
        Assert.Equal(HttpStatusCode.NoContent, resetResponse.StatusCode);

        var fetchedA = await (await _client.GetAsync($"/api/models/{createdA.Id}")).Content.ReadFromJsonAsync<ModelInstance>(TestJson.Options);
        var fetchedB = await (await _client.GetAsync($"/api/models/{createdB.Id}")).Content.ReadFromJsonAsync<ModelInstance>(TestJson.Options);
        Assert.False(fetchedA!.HasActivated);
        Assert.False(fetchedB!.HasActivated);
    }

    [Fact]
    public async Task Delete_ThenGetById_ReturnsNotFound()
    {
        var map = await CreateMapAsync("Model Delete Board");
        var session = await CreateSessionAsync(map.Id);
        var entry = await CreateFullChainArmyEntryAsync("Delete");

        var createResponse = await _client.PostAsJsonAsync($"/api/sessions/{session.Id}/models", new ModelInstance { ArmyEntryId = entry.Id }, TestJson.Options);
        var created = await createResponse.Content.ReadFromJsonAsync<ModelInstance>(TestJson.Options);

        var deleteResponse = await _client.DeleteAsync($"/api/models/{created!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await _client.GetAsync($"/api/models/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }
}
