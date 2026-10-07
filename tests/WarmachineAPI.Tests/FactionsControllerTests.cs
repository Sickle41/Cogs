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
        var page = await listResponse.Content.ReadFromJsonAsync<PagedResult<Faction>>(TestJson.Options);

        Assert.Contains(page!.Items, f => f.Id == created!.Id);
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

    [Fact]
    public async Task Create_WithEmptyName_ReturnsBadRequest()
    {
        var faction = new Faction { Name = "", Description = "No name." };

        var response = await _client.PostAsJsonAsync("/api/factions", faction, TestJson.Options);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_WithoutPageParams_DefaultsToPageOneSize50()
    {
        var response = await _client.GetAsync("/api/factions");
        var page = await response.Content.ReadFromJsonAsync<PagedResult<Faction>>(TestJson.Options);

        Assert.Equal(1, page!.Page);
        Assert.Equal(50, page.PageSize);
    }

    [Fact]
    public async Task GetAll_WithPageSize_LimitsItemCount()
    {
        for (var i = 0; i < 3; i++)
        {
            await _client.PostAsJsonAsync("/api/factions", new Faction { Name = $"PageSizeTest-{Guid.NewGuid()}" }, TestJson.Options);
        }

        var response = await _client.GetAsync("/api/factions?page=1&pageSize=2");
        var page = await response.Content.ReadFromJsonAsync<PagedResult<Faction>>(TestJson.Options);

        Assert.Equal(2, page!.Items.Count);
        Assert.Equal(1, page.Page);
        Assert.Equal(2, page.PageSize);
        Assert.True(page.TotalCount >= 3);
    }

    [Fact]
    public async Task GetAll_DifferentPages_ReturnDifferentItems()
    {
        await _client.PostAsJsonAsync("/api/factions", new Faction { Name = $"PagingDistinctA-{Guid.NewGuid()}" }, TestJson.Options);
        await _client.PostAsJsonAsync("/api/factions", new Faction { Name = $"PagingDistinctB-{Guid.NewGuid()}" }, TestJson.Options);

        var page1Response = await _client.GetAsync("/api/factions?page=1&pageSize=1");
        var page1 = await page1Response.Content.ReadFromJsonAsync<PagedResult<Faction>>(TestJson.Options);

        var page2Response = await _client.GetAsync("/api/factions?page=2&pageSize=1");
        var page2 = await page2Response.Content.ReadFromJsonAsync<PagedResult<Faction>>(TestJson.Options);

        Assert.Single(page1!.Items);
        Assert.Single(page2!.Items);
        Assert.NotEqual(page1.Items[0].Id, page2.Items[0].Id);
    }

    [Fact]
    public async Task GetAll_PageSizeOver200_IsCappedAt200()
    {
        var response = await _client.GetAsync("/api/factions?pageSize=500");
        var page = await response.Content.ReadFromJsonAsync<PagedResult<Faction>>(TestJson.Options);

        Assert.Equal(200, page!.PageSize);
    }
}
