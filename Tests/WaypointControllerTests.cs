using System.Net;
using System.Net.Http.Json;
using ParrotsAPI2.Dtos.VoyageDtos;
using ParrotsAPI2.Dtos.WaypointDtos;
using ParrotsAPI2.Models;
using parrotsAPI2.Tests.Helpers;

namespace parrotsAPI2.Tests;

public class WaypointControllerTests : IClassFixture<ParrotsWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly ParrotsWebApplicationFactory _factory;

    public WaypointControllerTests(ParrotsWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    // --- AddWaypointNoImage ---

    [Fact]
    public async Task AddWaypointNoImage_WithoutToken_Returns401()
    {
        var response = await _client.PostAsync("/api/Waypoint/AddWaypointNoImage", BuildWaypointForm(1));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AddWaypointNoImage_NotVoyageOwner_Returns403()
    {
        var (token1, userId1) = await ApiTestHelper.CreateConfirmedUserAsync(_client, _factory);
        var (token2, _) = await ApiTestHelper.CreateConfirmedUserAsync(_client, _factory);
        _factory.GiveCoins(userId1, 500);
        var voyageId = await CreateVoyageAsync(token1, userId1);

        var authedClient = _factory.CreateClient();
        ApiTestHelper.SetBearer(authedClient, token2);

        var response = await authedClient.PostAsync("/api/Waypoint/AddWaypointNoImage", BuildWaypointForm(voyageId));
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AddWaypointNoImage_Owner_Returns200()
    {
        var (token, userId) = await ApiTestHelper.CreateConfirmedUserAsync(_client, _factory);
        _factory.GiveCoins(userId, 500);
        var voyageId = await CreateVoyageAsync(token, userId);

        var authedClient = _factory.CreateClient();
        ApiTestHelper.SetBearer(authedClient, token);

        var response = await authedClient.PostAsync("/api/Waypoint/AddWaypointNoImage", BuildWaypointForm(voyageId));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // --- DeleteWaypoint ---

    [Fact]
    public async Task DeleteWaypoint_NotOwner_Returns403()
    {
        var (token1, userId1) = await ApiTestHelper.CreateConfirmedUserAsync(_client, _factory);
        var (token2, _) = await ApiTestHelper.CreateConfirmedUserAsync(_client, _factory);
        _factory.GiveCoins(userId1, 500);
        var voyageId = await CreateVoyageAsync(token1, userId1);
        var waypointId = await CreateWaypointAsync(token1, voyageId);

        var authedClient = _factory.CreateClient();
        ApiTestHelper.SetBearer(authedClient, token2);

        var response = await authedClient.DeleteAsync($"/api/Waypoint/DeleteWaypoint/{waypointId}");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeleteWaypoint_Owner_Returns200()
    {
        var (token, userId) = await ApiTestHelper.CreateConfirmedUserAsync(_client, _factory);
        _factory.GiveCoins(userId, 500);
        var voyageId = await CreateVoyageAsync(token, userId);
        var waypointId = await CreateWaypointAsync(token, voyageId);

        var authedClient = _factory.CreateClient();
        ApiTestHelper.SetBearer(authedClient, token);

        var response = await authedClient.DeleteAsync($"/api/Waypoint/DeleteWaypoint/{waypointId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // --- Helpers ---

    private static MultipartFormDataContent BuildWaypointForm(int voyageId)
    {
        var form = new MultipartFormDataContent();
        form.Add(new StringContent(voyageId.ToString()), "VoyageId");
        form.Add(new StringContent("41.0"), "Latitude");
        form.Add(new StringContent("29.0"), "Longitude");
        form.Add(new StringContent("Stop 1"), "Title");
        form.Add(new StringContent("1"), "Order");
        return form;
    }

    private async Task<int> CreateVoyageAsync(string token, string userId)
    {
        var authedClient = _factory.CreateClient();
        ApiTestHelper.SetBearer(authedClient, token);

        var response = await authedClient.PostAsync("/api/Voyage/AddVoyage", VoyageControllerTests.BuildVoyageForm("Waypoint Test Voyage", _factory.GetSeededVehicleId(), userId));
        var body = await ApiTestHelper.DeserializeAsync<ServiceResponse<GetVoyageDto>>(response);
        var voyageId = body!.Data!.Id;

        await authedClient.PostAsync($"/api/Voyage/ConfirmVoyage/{voyageId}", null);
        return voyageId;
    }

    private async Task<int> CreateWaypointAsync(string token, int voyageId)
    {
        var authedClient = _factory.CreateClient();
        ApiTestHelper.SetBearer(authedClient, token);

        var response = await authedClient.PostAsync("/api/Waypoint/AddWaypointNoImage", BuildWaypointForm(voyageId));
        var body = await ApiTestHelper.DeserializeAsync<ServiceResponse<int>>(response);
        return body!.Data;
    }
}
