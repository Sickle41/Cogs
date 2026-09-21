using System.Net;
using System.Net.Http.Json;
using WarmachineAPI.Models;

namespace WarmachineAPI.Tests;

public class GameParticipantsControllerTests : IClassFixture<WarmachineApiFactory>
{
    private readonly HttpClient _client;

    public GameParticipantsControllerTests(WarmachineApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<Map> CreateMapAsync(string name)
    {
        var response = await _client.PostAsJsonAsync("/api/maps", new Map { Name = name }, TestJson.Options);
        return (await response.Content.ReadFromJsonAsync<Map>(TestJson.Options))!;
    }

    private async Task<Faction> CreateFactionAsync(string name)
    {
        var response = await _client.PostAsJsonAsync("/api/factions", new Faction { Name = name }, TestJson.Options);
        return (await response.Content.ReadFromJsonAsync<Faction>(TestJson.Options))!;
    }

    private async Task<Army> CreateArmyAsync(Guid factionId, string name)
    {
        var response = await _client.PostAsJsonAsync("/api/armies", new Army { Name = name, FactionId = factionId, PointLimit = 50 }, TestJson.Options);
        return (await response.Content.ReadFromJsonAsync<Army>(TestJson.Options))!;
    }

    private async Task<GameSession> CreateSessionAsync(Guid mapId)
    {
        var response = await _client.PostAsJsonAsync("/api/sessions", new GameSession { MapId = mapId }, TestJson.Options);
        return (await response.Content.ReadFromJsonAsync<GameSession>(TestJson.Options))!;
    }

    [Fact]
    public async Task Create_WithInvalidArmy_ReturnsBadRequest()
    {
        var map = await CreateMapAsync("Participant Bad Army Board");
        var session = await CreateSessionAsync(map.Id);

        var participant = new GameParticipant { ArmyId = Guid.NewGuid(), TurnOrder = 1 };
        var response = await _client.PostAsJsonAsync($"/api/sessions/{session.Id}/participants", participant, TestJson.Options);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithValidArmy_ReturnsCreated()
    {
        var map = await CreateMapAsync("Participant Board");
        var session = await CreateSessionAsync(map.Id);
        var faction = await CreateFactionAsync("Cygnar-Participant");
        var army = await CreateArmyAsync(faction.Id, "Stryker's Command");

        var participant = new GameParticipant { ArmyId = army.Id, TurnOrder = 1 };
        var response = await _client.PostAsJsonAsync($"/api/sessions/{session.Id}/participants", participant, TestJson.Options);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<GameParticipant>(TestJson.Options);
        Assert.Equal(session.Id, created!.GameSessionId);
        Assert.Equal(army.Id, created.ArmyId);
    }

    [Fact]
    public async Task Delete_ThenGetById_ReturnsNotFound()
    {
        var map = await CreateMapAsync("Participant Delete Board");
        var session = await CreateSessionAsync(map.Id);
        var faction = await CreateFactionAsync("Khador-Participant");
        var army = await CreateArmyAsync(faction.Id, "Winter's Wrath");

        var createResponse = await _client.PostAsJsonAsync($"/api/sessions/{session.Id}/participants", new GameParticipant { ArmyId = army.Id, TurnOrder = 1 }, TestJson.Options);
        var created = await createResponse.Content.ReadFromJsonAsync<GameParticipant>(TestJson.Options);

        var deleteResponse = await _client.DeleteAsync($"/api/participants/{created!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await _client.GetAsync($"/api/participants/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }
}
