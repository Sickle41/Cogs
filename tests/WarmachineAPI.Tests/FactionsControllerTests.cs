using System.Net;
using System.Net.Http.Json;
using WarmachineAPI.Models;

namespace WarmachineAPI.Tests;

public class FactionsControllerTests : IClassFixture<WarmachineApiFactory>
{
    private readonly HttpClient _client;

    public FactionsControllerTests(WarmachineApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Create_Then_Get_ReturnsFaction()
    {
        var faction = new Faction { Name = "Cygnar", Description = "Steam nation." };

        var createResponse = await _client.PostAsJsonAsync("/api/factions", faction, TestJson.Options);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content.ReadFromJsonAsync<Faction>(TestJson.Options);
        Assert.NotNull(created);
        Assert.NotEqual(Guid.Empty, created!.Id);

        var getResponse = await _client.GetAsync($"/api/factions/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var fetched = await getResponse.Content.ReadFromJsonAsync<Faction>(TestJson.Options);
        Assert.Equal("Cygnar", fetched!.Name);
    }

    [Fact]
    public async Task GetAll_IncludesCreatedFaction()
    {
        var faction = new Faction { Name = "Khador", Description = "Frozen empire." };
        var createResponse = await _client.PostAsJsonAsync("/api/factions", faction, TestJson.Options);
        var created = await createResponse.Content.ReadFromJsonAsync<Faction>(TestJson.Options);

        var listResponse = await _client.GetAsync("/api/factions");
        var list = await listResponse.Content.ReadFromJsonAsync<List<Faction>>(TestJson.Options);

        Assert.Contains(list!, f => f.Id == created!.Id);
    }

    [Fact]
    public async Task Update_ChangesName()
    {
        var faction = new Faction { Name = "Retribution", Description = "Elves." };
        var createResponse = await _client.PostAsJsonAsync("/api/factions", faction, TestJson.Options);
        var created = await createResponse.Content.ReadFromJsonAsync<Faction>(TestJson.Options);

        created!.Name = "Retribution of Scyrah";
        var updateResponse = await _client.PutAsJsonAsync($"/api/factions/{created.Id}", created, TestJson.Options);
        Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);

        var getResponse = await _client.GetAsync($"/api/factions/{created.Id}");
        var fetched = await getResponse.Content.ReadFromJsonAsync<Faction>(TestJson.Options);
        Assert.Equal("Retribution of Scyrah", fetched!.Name);
    }

    [Fact]
    public async Task Delete_RemovesFaction()
    {
        var faction = new Faction { Name = "Trollbloods", Description = "Trolls." };
        var createResponse = await _client.PostAsJsonAsync("/api/factions", faction, TestJson.Options);
        var created = await createResponse.Content.ReadFromJsonAsync<Faction>(TestJson.Options);

        var deleteResponse = await _client.DeleteAsync($"/api/factions/{created!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await _client.GetAsync($"/api/factions/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task GetById_NonExistent_ReturnsNotFound()
    {
        var response = await _client.GetAsync($"/api/factions/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
