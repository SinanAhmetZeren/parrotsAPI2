using System.Net;
using System.Net.Http.Json;
using ParrotsAPI2.Dtos.VoyageDtos;
using ParrotsAPI2.Models;
using parrotsAPI2.Tests.Helpers;

namespace parrotsAPI2.Tests;

public class VoyageControllerAdditionalTests : IClassFixture<ParrotsWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly ParrotsWebApplicationFactory _factory;

    public VoyageControllerAdditionalTests(ParrotsWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    // --- checkAndDeleteVoyage ---

    [Fact]
    public async Task CheckAndDeleteVoyage_WithoutToken_Returns401()
    {
        var response = await _client.DeleteAsync("/api/Voyage/checkAndDeleteVoyage/999999");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CheckAndDeleteVoyage_NotOwner_Returns403()
    {
        var (token1, userId1) = await ApiTestHelper.CreateConfirmedUserAsync(_client, _factory);
        var (token2, _) = await ApiTestHelper.CreateConfirmedUserAsync(_client, _factory);

        _factory.GiveCoins(userId1, 500);
        var voyageId = await CreateVoyageAsync(token1, userId1);

        var authedClient = _factory.CreateClient();
        ApiTestHelper.SetBearer(authedClient, token2);

        var response = await authedClient.DeleteAsync($"/api/Voyage/checkAndDeleteVoyage/{voyageId}");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CheckAndDeleteVoyage_Owner_Returns200()
    {
        var (token, userId) = await ApiTestHelper.CreateConfirmedUserAsync(_client, _factory);
        _factory.GiveCoins(userId, 500);
        var voyageId = await CreateVoyageAsync(token, userId);

        var authedClient = _factory.CreateClient();
        ApiTestHelper.SetBearer(authedClient, token);

        var response = await authedClient.DeleteAsync($"/api/Voyage/checkAndDeleteVoyage/{voyageId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // --- GetVoyagesByCoords ---

    [Fact]
    public async Task GetVoyagesByCoords_AnonymousAllowed_Returns200()
    {
        var response = await _client.GetAsync("/api/Voyage/GetVoyagesByCoords/40/42/28/30");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetVoyagesByCoords_WithToken_Returns200()
    {
        var (token, _) = await ApiTestHelper.CreateConfirmedUserAsync(_client, _factory);
        var authedClient = _factory.CreateClient();
        ApiTestHelper.SetBearer(authedClient, token);

        var response = await authedClient.GetAsync("/api/Voyage/GetVoyagesByCoords/40/42/28/30");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // --- GetVoyageIdsByCoords ---

    [Fact]
    public async Task GetVoyageIdsByCoords_AnonymousAllowed_Returns200()
    {
        var response = await _client.GetAsync("/api/Voyage/GetVoyageIdsByCoords/40/42/28/30");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetVoyageIdsByCoords_WithToken_Returns200()
    {
        var (token, _) = await ApiTestHelper.CreateConfirmedUserAsync(_client, _factory);
        var authedClient = _factory.CreateClient();
        ApiTestHelper.SetBearer(authedClient, token);

        var response = await authedClient.GetAsync("/api/Voyage/GetVoyageIdsByCoords/40/42/28/30");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // --- AddPlace ---

    [Fact]
    public async Task AddPlace_WithoutToken_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/Voyage/AddPlace", new { });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AddPlace_Admin_ReturnsSuccessTrue()
    {
        var (token, userId) = await ApiTestHelper.CreateConfirmedUserAsync(_client, _factory);
        _factory.GiveAdminRole(userId);
        var authedClient = _factory.CreateClient();
        ApiTestHelper.SetBearer(authedClient, token);

        var payload = new { Name = "Admin Place", Brief = "Brief", Description = "Desc", PublicOnMap = true, Latitude = 41.0, Longitude = 29.0, PlaceType = 1, StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddDays(30) };
        var response = await authedClient.PostAsJsonAsync("/api/Voyage/AddPlace", payload);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await ApiTestHelper.DeserializeAsync<ServiceResponse<GetVoyageDto>>(response);
        Assert.True(body!.Success);
    }

    [Fact]
    public async Task AddPlace_NonAdmin_Returns403()
    {
        var (token, _) = await ApiTestHelper.CreateConfirmedUserAsync(_client, _factory);
        var authedClient = _factory.CreateClient();
        ApiTestHelper.SetBearer(authedClient, token);

        var payload = new { Name = "Test Place", PlaceType = 1, StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddDays(30) };
        var response = await authedClient.PostAsJsonAsync("/api/Voyage/AddPlace", payload);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // --- Helpers ---

    private async Task<int> CreateVoyageAsync(string token, string userId)
    {
        var authedClient = _factory.CreateClient();
        ApiTestHelper.SetBearer(authedClient, token);

        var response = await authedClient.PostAsync("/api/Voyage/AddVoyage",
            VoyageControllerTests.BuildVoyageForm("Additional Test Voyage", _factory.GetSeededVehicleId(), userId));
        var body = await ApiTestHelper.DeserializeAsync<ServiceResponse<GetVoyageDto>>(response);
        var voyageId = body!.Data!.Id;

        await authedClient.PostAsync($"/api/Voyage/ConfirmVoyage/{voyageId}", null);
        return voyageId;
    }
}
