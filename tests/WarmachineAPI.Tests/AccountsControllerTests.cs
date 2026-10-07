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
    public async Task Delete_WithOwnApiKey_ThenGetById_ReturnsNotFound()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/accounts", new Account { DisplayName = "Temp Account" }, TestJson.Options);
        var created = await createResponse.Content.ReadFromJsonAsync<Account>(TestJson.Options);

        var deleteRequest = new HttpRequestMessage(HttpMethod.Delete, $"/api/accounts/{created!.Id}");
        deleteRequest.Headers.Add("X-Api-Key", created.ApiKey);
        var deleteResponse = await _client.SendAsync(deleteRequest);
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await _client.GetAsync($"/api/accounts/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task Delete_WithoutApiKey_ReturnsUnauthorized()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/accounts", new Account { DisplayName = "Unauthorized Delete Target" }, TestJson.Options);
        var created = await createResponse.Content.ReadFromJsonAsync<Account>(TestJson.Options);

        var deleteResponse = await _client.DeleteAsync($"/api/accounts/{created!.Id}");
        Assert.Equal(HttpStatusCode.Unauthorized, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task Delete_WithAnotherAccountsApiKey_ReturnsForbidden()
    {
        var targetResponse = await _client.PostAsJsonAsync("/api/accounts", new Account { DisplayName = "Forbidden Delete Target" }, TestJson.Options);
        var target = await targetResponse.Content.ReadFromJsonAsync<Account>(TestJson.Options);

        var otherResponse = await _client.PostAsJsonAsync("/api/accounts", new Account { DisplayName = "Other Account" }, TestJson.Options);
        var other = await otherResponse.Content.ReadFromJsonAsync<Account>(TestJson.Options);

        var deleteRequest = new HttpRequestMessage(HttpMethod.Delete, $"/api/accounts/{target!.Id}");
        deleteRequest.Headers.Add("X-Api-Key", other!.ApiKey);
        var deleteResponse = await _client.SendAsync(deleteRequest);

        Assert.Equal(HttpStatusCode.Forbidden, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task Update_WithOwnApiKey_ChangesDisplayName()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/accounts", new Account { DisplayName = "Before Update" }, TestJson.Options);
        var created = await createResponse.Content.ReadFromJsonAsync<Account>(TestJson.Options);

        var updateRequest = new HttpRequestMessage(HttpMethod.Put, $"/api/accounts/{created!.Id}")
        {
            Content = JsonContent.Create(new Account { DisplayName = "After Update" }, options: TestJson.Options)
        };
        updateRequest.Headers.Add("X-Api-Key", created.ApiKey);
        var updateResponse = await _client.SendAsync(updateRequest);
        Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);

        var getResponse = await _client.GetAsync($"/api/accounts/{created.Id}");
        var fetched = await getResponse.Content.ReadFromJsonAsync<Account>(TestJson.Options);
        Assert.Equal("After Update", fetched!.DisplayName);
        Assert.Equal(created.ApiKey, fetched.ApiKey);
    }

    [Fact]
    public async Task Create_WithEmptyDisplayName_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync("/api/accounts", new Account { DisplayName = "" }, TestJson.Options);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_ReturnsNonEmptyApiKey()
    {
        var response = await _client.PostAsJsonAsync("/api/accounts", new Account { DisplayName = "Key Check" }, TestJson.Options);
        var created = await response.Content.ReadFromJsonAsync<Account>(TestJson.Options);

        Assert.False(string.IsNullOrWhiteSpace(created!.ApiKey));
    }
}
