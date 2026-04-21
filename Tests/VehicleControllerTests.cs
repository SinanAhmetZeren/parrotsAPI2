using System.Net;
using System.Net.Http.Json;
using ParrotsAPI2.Dtos.VehicleDtos;
using ParrotsAPI2.Models;
using parrotsAPI2.Tests.Helpers;

namespace parrotsAPI2.Tests;

public class VehicleControllerTests : IClassFixture<ParrotsWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly ParrotsWebApplicationFactory _factory;

    public VehicleControllerTests(ParrotsWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    // --- GetVehicleById (anonymous) ---

    [Fact]
    public async Task GetVehicleById_Anonymous_Returns200()
    {
        var response = await _client.GetAsync($"/api/Vehicle/GetVehicleById/{_factory.GetSeededVehicleId()}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // --- GetVehiclesByUserId (anonymous) ---

    [Fact]
    public async Task GetVehiclesByUserId_Anonymous_Returns200()
    {
        var response = await _client.GetAsync("/api/Vehicle/GetVehiclesByUserId/someuser");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // --- AddVehicle ---

    [Fact]
    public async Task AddVehicle_WithoutToken_Returns401()
    {
        var response = await _client.PostAsync("/api/Vehicle/addVehicle", BuildVehicleForm("Boat", "5", "Test", "wrong-user"));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AddVehicle_WrongUserId_Returns403()
    {
        var (token, _) = await ApiTestHelper.CreateConfirmedUserAsync(_client, _factory);
        var authedClient = _factory.CreateClient();
        ApiTestHelper.SetBearer(authedClient, token);

        var response = await authedClient.PostAsync("/api/Vehicle/addVehicle", BuildVehicleForm("My Boat", "5", "Test", "wrong-user"));
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AddVehicle_ValidPayload_Returns200()
    {
        var (token, userId) = await ApiTestHelper.CreateConfirmedUserAsync(_client, _factory);
        var authedClient = _factory.CreateClient();
        ApiTestHelper.SetBearer(authedClient, token);

        var response = await authedClient.PostAsync("/api/Vehicle/addVehicle", BuildVehicleForm("My Boat", "5", "Test", userId));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // --- ConfirmVehicle ---

    [Fact]
    public async Task ConfirmVehicle_NotOwner_Returns403()
    {
        var (token1, userId1) = await ApiTestHelper.CreateConfirmedUserAsync(_client, _factory);
        var (token2, _) = await ApiTestHelper.CreateConfirmedUserAsync(_client, _factory);
        var vehicleId = await CreateVehicleAsync(token1, userId1);

        var authedClient = _factory.CreateClient();
        ApiTestHelper.SetBearer(authedClient, token2);

        var response = await authedClient.PostAsync($"/api/Vehicle/confirmVehicle/{vehicleId}", null);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ConfirmVehicle_Owner_Returns200()
    {
        var (token, userId) = await ApiTestHelper.CreateConfirmedUserAsync(_client, _factory);
        var vehicleId = await CreateVehicleAsync(token, userId);

        var authedClient = _factory.CreateClient();
        ApiTestHelper.SetBearer(authedClient, token);

        var response = await authedClient.PostAsync($"/api/Vehicle/confirmVehicle/{vehicleId}", null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // --- DeleteVehicle ---

    [Fact]
    public async Task DeleteVehicle_NotOwner_Returns403()
    {
        var (token1, userId1) = await ApiTestHelper.CreateConfirmedUserAsync(_client, _factory);
        var (token2, _) = await ApiTestHelper.CreateConfirmedUserAsync(_client, _factory);
        var vehicleId = await CreateVehicleAsync(token1, userId1);

        var authedClient = _factory.CreateClient();
        ApiTestHelper.SetBearer(authedClient, token2);

        var response = await authedClient.DeleteAsync($"/api/Vehicle/deleteVehicle/{vehicleId}");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeleteVehicle_Owner_Returns200()
    {
        var (token, userId) = await ApiTestHelper.CreateConfirmedUserAsync(_client, _factory);
        var vehicleId = await CreateVehicleAsync(token, userId);

        var authedClient = _factory.CreateClient();
        ApiTestHelper.SetBearer(authedClient, token);

        var response = await authedClient.DeleteAsync($"/api/Vehicle/deleteVehicle/{vehicleId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // --- Helpers ---

    private static MultipartFormDataContent BuildVehicleForm(string name, string capacity, string description, string userId)
    {
        var form = new MultipartFormDataContent();
        form.Add(new StringContent(name), "Name");
        form.Add(new StringContent("Boat"), "Type");
        form.Add(new StringContent(capacity), "Capacity");
        form.Add(new StringContent(description), "Description");
        form.Add(new StringContent(userId), "UserId");
        return form;
    }

    private async Task<int> CreateVehicleAsync(string token, string userId)
    {
        var authedClient = _factory.CreateClient();
        ApiTestHelper.SetBearer(authedClient, token);

        var response = await authedClient.PostAsync("/api/Vehicle/addVehicle", BuildVehicleForm("Test Boat", "5", "Test", userId));
        var body = await ApiTestHelper.DeserializeAsync<ServiceResponse<GetVehicleDto>>(response);
        var vehicleId = body!.Data!.Id;

        await authedClient.PostAsync($"/api/Vehicle/confirmVehicle/{vehicleId}", null);
        return vehicleId;
    }
}
