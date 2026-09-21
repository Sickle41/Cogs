using System.Net;
using System.Net.Http.Json;
using WarmachineAPI.Models;

namespace WarmachineAPI.Tests;

public class AccountsControllerTests : IClassFixture<WarmachineApiFactory>
{
    private readonly HttpClient _client;

    public AccountsControllerTests(WarmachineApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Create_Then_Get_ReturnsAccount()
    {
        var account = new Account { DisplayName = "Sickle41", Email = "sickle41@example.com" };

        var createResponse = await _client.PostAsJsonAsync("/api/accounts", account, TestJson.Options);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content.ReadFromJsonAsync<Account>(TestJson.Options);
        var getResponse = await _client.GetAsync($"/api/accounts/{created!.Id}");
        var fetched = await getResponse.Content.ReadFromJsonAsync<Account>(TestJson.Options);

        Assert.Equal("Sickle41", fetched!.DisplayName);
    }

    [Fact]
    public async Task Delete_ThenGetById_ReturnsNotFound()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/accounts", new Account { DisplayName = "Temp Account" }, TestJson.Options);
        var created = await createResponse.Content.ReadFromJsonAsync<Account>(TestJson.Options);

        var deleteResponse = await _client.DeleteAsync($"/api/accounts/{created!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await _client.GetAsync($"/api/accounts/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }
}
