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

    private async Task<UnitDefinition> CreateUnitAsync(Guid factionId, string name, int pointCost, UnitCategory category = UnitCategory.Unit)
    {
        var unit = new UnitDefinition { FactionId = factionId, Name = name, Category = category, PointCost = pointCost };
        var response = await _client.PostAsJsonAsync("/api/units", unit, TestJson.Options);
        return (await response.Content.ReadFromJsonAsync<UnitDefinition>(TestJson.Options))!;
    }

    private async Task AddArmyEntryAsync(Guid armyId, Guid unitId, int quantity)
    {
        await _client.PostAsJsonAsync($"/api/armies/{armyId}/entries", new ArmyEntry { UnitDefinitionId = unitId, Quantity = quantity }, TestJson.Options);
    }

    private async Task<ArmyEntry> CreateArmyEntryAsync(Guid armyId, ArmyEntry entry)
    {
        var response = await _client.PostAsJsonAsync($"/api/armies/{armyId}/entries", entry, TestJson.Options);
        return (await response.Content.ReadFromJsonAsync<ArmyEntry>(TestJson.Options))!;
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

    [Fact]
    public async Task Create_WithInvalidOwnerAccount_ReturnsBadRequest()
    {
        var faction = await CreateFactionAsync("Cygnar-Bad-Owner");
        var army = new Army { Name = "Orphan Owner Army", FactionId = faction.Id, PointLimit = 50, OwnerAccountId = Guid.NewGuid() };

        var response = await _client.PostAsJsonAsync("/api/armies", army, TestJson.Options);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithValidOwnerAccount_ReturnsCreated()
    {
        var faction = await CreateFactionAsync("Cygnar-Valid-Owner");
        var accountResponse = await _client.PostAsJsonAsync("/api/accounts", new Account { DisplayName = "Sickle41" }, TestJson.Options);
        var account = await accountResponse.Content.ReadFromJsonAsync<Account>(TestJson.Options);

        var army = new Army { Name = "Owned Army", FactionId = faction.Id, PointLimit = 50, OwnerAccountId = account!.Id };
        var response = await _client.PostAsJsonAsync("/api/armies", army, TestJson.Options);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<Army>(TestJson.Options);
        Assert.Equal(account.Id, created!.OwnerAccountId);
    }

    [Fact]
    public async Task Create_WithoutOwnerAccount_ReturnsCreated()
    {
        var faction = await CreateFactionAsync("Cygnar-No-Owner");
        var army = new Army { Name = "Unowned Army", FactionId = faction.Id, PointLimit = 50 };

        var response = await _client.PostAsJsonAsync("/api/armies", army, TestJson.Options);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<Army>(TestJson.Options);
        Assert.Null(created!.OwnerAccountId);
    }

    [Fact]
    public async Task Create_WithNegativePointLimit_ReturnsBadRequest()
    {
        var faction = await CreateFactionAsync("Cygnar-Negative-Limit");
        var army = new Army { Name = "Invalid Army", FactionId = faction.Id, PointLimit = -10 };

        var response = await _client.PostAsJsonAsync("/api/armies", army, TestJson.Options);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_SetsCreatedAtAndUpdatedAtToNow()
    {
        var faction = await CreateFactionAsync("Cygnar-Audit-Create");
        var before = DateTime.UtcNow;

        var response = await _client.PostAsJsonAsync("/api/armies", new Army { Name = "Audit Army", FactionId = faction.Id, PointLimit = 50 }, TestJson.Options);
        var created = await response.Content.ReadFromJsonAsync<Army>(TestJson.Options);

        var after = DateTime.UtcNow;

        Assert.InRange(created!.CreatedAt, before.AddSeconds(-1), after.AddSeconds(1));
        Assert.Equal(created.CreatedAt, created.UpdatedAt);
    }

    [Fact]
    public async Task Update_PreservesCreatedAt_ButAdvancesUpdatedAt()
    {
        var faction = await CreateFactionAsync("Cygnar-Audit-Update");
        var createResponse = await _client.PostAsJsonAsync("/api/armies", new Army { Name = "Audit Army Before", FactionId = faction.Id, PointLimit = 50 }, TestJson.Options);
        var created = await createResponse.Content.ReadFromJsonAsync<Army>(TestJson.Options);

        await Task.Delay(50);

        created!.Name = "Audit Army After";
        var updateResponse = await _client.PutAsJsonAsync($"/api/armies/{created.Id}", created, TestJson.Options);
        Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);

        var getResponse = await _client.GetAsync($"/api/armies/{created.Id}");
        var fetched = await getResponse.Content.ReadFromJsonAsync<Army>(TestJson.Options);

        Assert.Equal(created.CreatedAt, fetched!.CreatedAt);
        Assert.True(fetched.UpdatedAt > fetched.CreatedAt);
    }

    [Fact]
    public async Task Update_WithCurrentToken_SucceedsAndBumpsToken()
    {
        var faction = await CreateFactionAsync("Cygnar-Concurrency-Ok");
        var createResponse = await _client.PostAsJsonAsync("/api/armies", new Army { Name = "Concurrency Army", FactionId = faction.Id, PointLimit = 50 }, TestJson.Options);
        var created = await createResponse.Content.ReadFromJsonAsync<Army>(TestJson.Options);
        Assert.NotEqual(Guid.Empty, created!.ConcurrencyToken);

        created.Name = "Concurrency Army Updated";
        var updateResponse = await _client.PutAsJsonAsync($"/api/armies/{created.Id}", created, TestJson.Options);
        Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);

        var fetched = await (await _client.GetAsync($"/api/armies/{created.Id}")).Content.ReadFromJsonAsync<Army>(TestJson.Options);
        Assert.NotEqual(created.ConcurrencyToken, fetched!.ConcurrencyToken);
    }

    [Fact]
    public async Task Update_WithStaleToken_ReturnsConflict()
    {
        var faction = await CreateFactionAsync("Cygnar-Concurrency-Conflict");
        var createResponse = await _client.PostAsJsonAsync("/api/armies", new Army { Name = "Stale Token Army", FactionId = faction.Id, PointLimit = 50 }, TestJson.Options);
        var created = await createResponse.Content.ReadFromJsonAsync<Army>(TestJson.Options);

        // First client's update succeeds and bumps the token.
        var firstEdit = new Army { Id = created!.Id, Name = "First Editor", FactionId = faction.Id, PointLimit = 50, ConcurrencyToken = created.ConcurrencyToken };
        await _client.PutAsJsonAsync($"/api/armies/{created.Id}", firstEdit, TestJson.Options);

        // Second client still has the original (now stale) token.
        var secondEdit = new Army { Id = created.Id, Name = "Second Editor", FactionId = faction.Id, PointLimit = 50, ConcurrencyToken = created.ConcurrencyToken };
        var conflictResponse = await _client.PutAsJsonAsync($"/api/armies/{created.Id}", secondEdit, TestJson.Options);

        Assert.Equal(HttpStatusCode.Conflict, conflictResponse.StatusCode);
    }

    [Fact]
    public async Task Delete_WithDependentEntry_ReturnsConflict()
    {
        var faction = await CreateFactionAsync("Cygnar-Delete-Protected-Army");
        var createResponse = await _client.PostAsJsonAsync("/api/armies", new Army { Name = "Delete Protected Army", FactionId = faction.Id, PointLimit = 50 }, TestJson.Options);
        var army = await createResponse.Content.ReadFromJsonAsync<Army>(TestJson.Options);

        var unit = await CreateUnitAsync(faction.Id, "Stormblade", 3);
        await AddArmyEntryAsync(army!.Id, unit.Id, 6);

        var deleteResponse = await _client.DeleteAsync($"/api/armies/{army.Id}");
        Assert.Equal(HttpStatusCode.Conflict, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task GetExport_NonExistentArmy_ReturnsNotFound()
    {
        var response = await _client.GetAsync($"/api/armies/{Guid.NewGuid()}/export");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetExport_WithNoEntries_ReturnsEmptyListAndZeroTotal()
    {
        var faction = await CreateFactionAsync("Cygnar-Export-Empty");
        var createResponse = await _client.PostAsJsonAsync("/api/armies", new Army { Name = "Empty Export List", FactionId = faction.Id, PointLimit = 50 }, TestJson.Options);
        var army = await createResponse.Content.ReadFromJsonAsync<Army>(TestJson.Options);

        var response = await _client.GetAsync($"/api/armies/{army!.Id}/export");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var export = await response.Content.ReadFromJsonAsync<ArmyExport>(TestJson.Options);
        Assert.Equal("Empty Export List", export!.ArmyName);
        Assert.Equal("Cygnar-Export-Empty", export.FactionName);
        Assert.Empty(export.Entries);
        Assert.Equal(0, export.TotalPoints);
    }

    [Fact]
    public async Task GetExport_ComposesEntriesWithAttachmentAndBattlegroupCaster()
    {
        var faction = await CreateFactionAsync("Cygnar-Export-Full");
        var createResponse = await _client.PostAsJsonAsync("/api/armies", new Army { Name = "Full Export List", FactionId = faction.Id, PointLimit = 50 }, TestJson.Options);
        var army = await createResponse.Content.ReadFromJsonAsync<Army>(TestJson.Options);

        var caster = await CreateUnitAsync(faction.Id, "Commander Stryker", 0, UnitCategory.Warcaster);
        var jack = await CreateUnitAsync(faction.Id, "Ironclad", 8, UnitCategory.Warjack);
        var unit = await CreateUnitAsync(faction.Id, "Stormblade", 3);

        var attachmentResponse = await _client.PostAsJsonAsync($"/api/units/{unit.Id}/attachments", new UnitAttachment { Name = "Rocket Pack", PointCost = 1 }, TestJson.Options);
        var attachment = await attachmentResponse.Content.ReadFromJsonAsync<UnitAttachment>(TestJson.Options);

        var casterEntry = await CreateArmyEntryAsync(army!.Id, new ArmyEntry { UnitDefinitionId = caster.Id, Quantity = 1 });
        await CreateArmyEntryAsync(army.Id, new ArmyEntry { UnitDefinitionId = jack.Id, Quantity = 1, BattlegroupCasterEntryId = casterEntry.Id });
        await CreateArmyEntryAsync(army.Id, new ArmyEntry { UnitDefinitionId = unit.Id, Quantity = 6, UnitAttachmentId = attachment!.Id });

        var response = await _client.GetAsync($"/api/armies/{army.Id}/export");
        var export = await response.Content.ReadFromJsonAsync<ArmyExport>(TestJson.Options);

        Assert.Equal(3, export!.Entries.Count);
        Assert.Equal(0 + 8 + 3 * 6, export.TotalPoints);

        var jackEntry = export.Entries.Single(e => e.UnitName == "Ironclad");
        Assert.Equal(UnitCategory.Warjack, jackEntry.Category);
        Assert.Equal("Commander Stryker", jackEntry.BattlegroupCasterName);
        Assert.Null(jackEntry.AttachmentName);

        var stormbladeEntry = export.Entries.Single(e => e.UnitName == "Stormblade");
        Assert.Equal(6, stormbladeEntry.Quantity);
        Assert.Equal("Rocket Pack", stormbladeEntry.AttachmentName);
        Assert.Null(stormbladeEntry.BattlegroupCasterName);
    }
}
