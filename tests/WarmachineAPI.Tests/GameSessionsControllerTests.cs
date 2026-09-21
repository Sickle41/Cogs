using System.Net;
using System.Net.Http.Json;
using WarmachineAPI.Models;

namespace WarmachineAPI.Tests;

public class GameSessionsControllerTests : IClassFixture<WarmachineApiFactory>
{
    private readonly HttpClient _client;

    public GameSessionsControllerTests(WarmachineApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<Map> CreateMapAsync(string name)
    {
        var response = await _client.PostAsJsonAsync("/api/maps", new Map { Name = name }, TestJson.Options);
        return (await response.Content.ReadFromJsonAsync<Map>(TestJson.Options))!;
    }

    [Fact]
    public async Task Create_WithInvalidMap_ReturnsBadRequest()
    {
        var session = new GameSession { MapId = Guid.NewGuid(), Status = SessionStatus.Setup };

        var response = await _client.PostAsJsonAsync("/api/sessions", session, TestJson.Options);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithValidMap_ReturnsCreated()
    {
        var map = await CreateMapAsync("Session Board");
        var session = new GameSession { MapId = map.Id, Status = SessionStatus.Setup, CurrentRound = 1 };

        var response = await _client.PostAsJsonAsync("/api/sessions", session, TestJson.Options);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<GameSession>(TestJson.Options);
        Assert.Equal(SessionStatus.Setup, created!.Status);
    }

    [Fact]
    public async Task Update_ChangesStatusAndActiveParticipant()
    {
        var map = await CreateMapAsync("Session Update Board");
        var createResponse = await _client.PostAsJsonAsync("/api/sessions", new GameSession { MapId = map.Id }, TestJson.Options);
        var created = await createResponse.Content.ReadFromJsonAsync<GameSession>(TestJson.Options);

        var participantId = Guid.NewGuid();
        created!.Status = SessionStatus.InProgress;
        created.ActiveParticipantId = participantId;

        var updateResponse = await _client.PutAsJsonAsync($"/api/sessions/{created.Id}", created, TestJson.Options);
        Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);

        var getResponse = await _client.GetAsync($"/api/sessions/{created.Id}");
        var fetched = await getResponse.Content.ReadFromJsonAsync<GameSession>(TestJson.Options);
        Assert.Equal(SessionStatus.InProgress, fetched!.Status);
        Assert.Equal(participantId, fetched.ActiveParticipantId);
    }
}
