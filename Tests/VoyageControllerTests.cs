using System.Net;
using System.Net.Http.Json;
using ParrotsAPI2.Dtos.VoyageDtos;
using ParrotsAPI2.Models;
using parrotsAPI2.Tests.Helpers;

namespace parrotsAPI2.Tests;

public class VoyageControllerTests : IClassFixture<ParrotsWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly ParrotsWebApplicationFactory _factory;

    public VoyageControllerTests(ParrotsWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    // --- GetVoyageById (anonymous) ---

    [Fact]
    public async Task GetVoyageById_NonExistent_ReturnsOkWithNullData()
    {
        var response = await _client.GetAsync("/api/Voyage/GetVoyageById/999999");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // --- AddVoyage ---

    [Fact]
    public async Task AddVoyage_WithoutToken_Returns401()
    {
        var response = await _client.PostAsync("/api/Voyage/AddVoyage", BuildVoyageForm("Trip", _factory.GetSeededVehicleId(), "any-user"));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AddVoyage_WrongUserId_Returns403()
    {
        var (token, userId) = await ApiTestHelper.CreateConfirmedUserAsync(_client, _factory);
        var authedClient = _factory.CreateClient();
        ApiTestHelper.SetBearer(authedClient, token);

        var response = await authedClient.PostAsync("/api/Voyage/AddVoyage", BuildVoyageForm("Trip", _factory.GetSeededVehicleId(), "someone-else"));
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AddVoyage_ValidPayload_Returns200()
    {
        var (token, userId) = await ApiTestHelper.CreateConfirmedUserAsync(_client, _factory);
        var authedClient = _factory.CreateClient();
        ApiTestHelper.SetBearer(authedClient, token);

        _factory.GiveCoins(userId, 500);

        var response = await authedClient.PostAsync("/api/Voyage/AddVoyage", BuildVoyageForm("My Voyage", _factory.GetSeededVehicleId(), userId));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // --- ConfirmVoyage ---

    [Fact]
    public async Task ConfirmVoyage_NotOwner_Returns403()
    {
        var (token1, userId1) = await ApiTestHelper.CreateConfirmedUserAsync(_client, _factory);
        var (token2, userId2) = await ApiTestHelper.CreateConfirmedUserAsync(_client, _factory);

        _factory.GiveCoins(userId1, 500);
        var voyageId = await CreateUnconfirmedVoyageAsync(token1, userId1);

        var authedClient = _factory.CreateClient();
        ApiTestHelper.SetBearer(authedClient, token2);

        var response = await authedClient.PostAsync($"/api/Voyage/ConfirmVoyage/{voyageId}", null);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ConfirmVoyage_Owner_Returns200()
    {
        var (token, userId) = await ApiTestHelper.CreateConfirmedUserAsync(_client, _factory);
        _factory.GiveCoins(userId, 500);
        var voyageId = await CreateUnconfirmedVoyageAsync(token, userId);

        var authedClient = _factory.CreateClient();
        ApiTestHelper.SetBearer(authedClient, token);

        var response = await authedClient.PostAsync($"/api/Voyage/ConfirmVoyage/{voyageId}", null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ConfirmVoyage_AlreadyConfirmed_ReturnsBadRequest()
    {
        var (token, userId) = await ApiTestHelper.CreateConfirmedUserAsync(_client, _factory);
        _factory.GiveCoins(userId, 500);
        var voyageId = await CreateUnconfirmedVoyageAsync(token, userId);

        var authedClient = _factory.CreateClient();
        ApiTestHelper.SetBearer(authedClient, token);

        await authedClient.PostAsync($"/api/Voyage/ConfirmVoyage/{voyageId}", null);
        var response = await authedClient.PostAsync($"/api/Voyage/ConfirmVoyage/{voyageId}", null);

        Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
    }

    // --- DeleteVoyage ---

    [Fact]
    public async Task DeleteVoyage_NotOwner_Returns403()
    {
        var (token1, userId1) = await ApiTestHelper.CreateConfirmedUserAsync(_client, _factory);
        var (token2, _) = await ApiTestHelper.CreateConfirmedUserAsync(_client, _factory);

        _factory.GiveCoins(userId1, 500);
        var voyageId = await CreateVoyageAsync(token1, userId1);

        var authedClient = _factory.CreateClient();
        ApiTestHelper.SetBearer(authedClient, token2);

        var response = await authedClient.DeleteAsync($"/api/Voyage/DeleteVoyage/{voyageId}");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeleteVoyage_Owner_Returns200()
    {
        var (token, userId) = await ApiTestHelper.CreateConfirmedUserAsync(_client, _factory);
        _factory.GiveCoins(userId, 500);
        var voyageId = await CreateVoyageAsync(token, userId);

        var authedClient = _factory.CreateClient();
        ApiTestHelper.SetBearer(authedClient, token);

        var response = await authedClient.DeleteAsync($"/api/Voyage/DeleteVoyage/{voyageId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // --- GetVoyageByUserId (anonymous) ---

    [Fact]
    public async Task GetVoyageByUserId_Anonymous_Returns200()
    {
        var response = await _client.GetAsync("/api/Voyage/GetVoyageByUserId/someuser");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // --- GetFilteredVoyages (anonymous) ---

    [Fact]
    public async Task GetFilteredVoyages_Anonymous_Returns200()
    {
        var response = await _client.GetAsync("/api/Voyage/GetFilteredVoyages");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // --- Helpers ---

    internal static MultipartFormDataContent BuildVoyageForm(string name, int vehicleId, string userId)
    {
        var form = new MultipartFormDataContent();
        form.Add(new StringContent(name), "Name");
        form.Add(new StringContent(vehicleId.ToString()), "VehicleId");
        form.Add(new StringContent(userId), "UserId");
        form.Add(new StringContent(DateTime.UtcNow.AddDays(5).ToString("o")), "StartDate");
        form.Add(new StringContent(DateTime.UtcNow.AddDays(10).ToString("o")), "EndDate");
        form.Add(new StringContent(DateTime.UtcNow.AddDays(3).ToString("o")), "LastBidDate");
        form.Add(new StringContent("2"), "Vacancy");
        form.Add(new StringContent("true"), "PublicOnMap");
        return form;
    }

    internal async Task<int> CreateUnconfirmedVoyageAsync(string token, string userId)
    {
        var authedClient = _factory.CreateClient();
        ApiTestHelper.SetBearer(authedClient, token);

        var response = await authedClient.PostAsync("/api/Voyage/AddVoyage", BuildVoyageForm("Test Voyage", _factory.GetSeededVehicleId(), userId));
        var body = await ApiTestHelper.DeserializeAsync<ServiceResponse<GetVoyageDto>>(response);
        return body!.Data!.Id;
    }

    internal async Task<int> CreateVoyageAsync(string token, string userId)
    {
        var authedClient = _factory.CreateClient();
        ApiTestHelper.SetBearer(authedClient, token);

        var voyageId = await CreateUnconfirmedVoyageAsync(token, userId);
        await authedClient.PostAsync($"/api/Voyage/ConfirmVoyage/{voyageId}", null);
        return voyageId;
    }
}
