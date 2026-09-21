using System.Net;
using System.Net.Http.Json;
using WarmachineAPI.Models;

namespace WarmachineAPI.Tests;

public class ScenariosControllerTests : IClassFixture<WarmachineApiFactory>
{
    private readonly HttpClient _client;

    public ScenariosControllerTests(WarmachineApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Create_Then_Get_ReturnsScenario()
    {
        var scenario = new Scenario { Name = "Incursion", Description = "Control zones and flags." };

        var createResponse = await _client.PostAsJsonAsync("/api/scenarios", scenario, TestJson.Options);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content.ReadFromJsonAsync<Scenario>(TestJson.Options);
        var getResponse = await _client.GetAsync($"/api/scenarios/{created!.Id}");
        var fetched = await getResponse.Content.ReadFromJsonAsync<Scenario>(TestJson.Options);

        Assert.Equal("Incursion", fetched!.Name);
    }

    [Fact]
    public async Task Delete_ThenGetById_ReturnsNotFound()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/scenarios", new Scenario { Name = "Recon" }, TestJson.Options);
        var created = await createResponse.Content.ReadFromJsonAsync<Scenario>(TestJson.Options);

        var deleteResponse = await _client.DeleteAsync($"/api/scenarios/{created!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await _client.GetAsync($"/api/scenarios/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }
}
