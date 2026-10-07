using System.Net;
using System.Net.Http.Json;
using WarmachineAPI.Models;

namespace WarmachineAPI.Tests;

public class UnitAttachmentsControllerTests : IClassFixture<WarmachineApiFactory>
{
    private readonly HttpClient _client;

    public UnitAttachmentsControllerTests(WarmachineApiFactory factory)
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
        var attachment = new UnitAttachment { Name = "Rocket Pack", PointCost = 1 };

        var response = await _client.PostAsJsonAsync($"/api/units/{Guid.NewGuid()}/attachments", attachment, TestJson.Options);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_ThenGet_RoundTripsAttachment()
    {
        var faction = await CreateFactionAsync("Cygnar-Attachment");
        var unit = await CreateUnitAsync(faction.Id, "Trencher Grenadier");

        var attachment = new UnitAttachment { Name = "Thumper Cannon", PointCost = 2, Description = "A heavy ordnance upgrade." };
        var createResponse = await _client.PostAsJsonAsync($"/api/units/{unit.Id}/attachments", attachment, TestJson.Options);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content.ReadFromJsonAsync<UnitAttachment>(TestJson.Options);
        Assert.Equal(unit.Id, created!.UnitDefinitionId);

        var getResponse = await _client.GetAsync($"/api/unit-attachments/{created.Id}");
        var fetched = await getResponse.Content.ReadFromJsonAsync<UnitAttachment>(TestJson.Options);
        Assert.Equal("Thumper Cannon", fetched!.Name);
        Assert.Equal(2, fetched.PointCost);
    }

    [Fact]
    public async Task GetForUnit_OnlyReturnsThatUnitsAttachments()
    {
        var faction = await CreateFactionAsync("Cygnar-Attachment-List");
        var unitA = await CreateUnitAsync(faction.Id, "Unit A");
        var unitB = await CreateUnitAsync(faction.Id, "Unit B");

        var createdResponse = await _client.PostAsJsonAsync($"/api/units/{unitA.Id}/attachments", new UnitAttachment { Name = "A Attachment", PointCost = 1 }, TestJson.Options);
        var created = await createdResponse.Content.ReadFromJsonAsync<UnitAttachment>(TestJson.Options);
        await _client.PostAsJsonAsync($"/api/units/{unitB.Id}/attachments", new UnitAttachment { Name = "B Attachment", PointCost = 1 }, TestJson.Options);

        var response = await _client.GetAsync($"/api/units/{unitA.Id}/attachments");
        var list = await response.Content.ReadFromJsonAsync<List<UnitAttachment>>(TestJson.Options);

        Assert.All(list!, a => Assert.Equal(unitA.Id, a.UnitDefinitionId));
        Assert.Contains(list!, a => a.Id == created!.Id);
    }

    [Fact]
    public async Task Delete_ThenGetById_ReturnsNotFound()
    {
        var faction = await CreateFactionAsync("Cygnar-Attachment-Delete");
        var unit = await CreateUnitAsync(faction.Id, "Unit Delete");

        var createResponse = await _client.PostAsJsonAsync($"/api/units/{unit.Id}/attachments", new UnitAttachment { Name = "Temporary Attachment", PointCost = 1 }, TestJson.Options);
        var created = await createResponse.Content.ReadFromJsonAsync<UnitAttachment>(TestJson.Options);

        var deleteResponse = await _client.DeleteAsync($"/api/unit-attachments/{created!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await _client.GetAsync($"/api/unit-attachments/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }
}
