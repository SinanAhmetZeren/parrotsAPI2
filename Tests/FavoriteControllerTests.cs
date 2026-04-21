using System.Net;
using System.Net.Http.Json;
using parrotsAPI2.Tests.Helpers;

namespace parrotsAPI2.Tests;

public class FavoriteControllerTests : IClassFixture<ParrotsWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly ParrotsWebApplicationFactory _factory;

    public FavoriteControllerTests(ParrotsWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    // --- AddFavorite ---

    [Fact]
    public async Task AddFavorite_WithoutToken_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/Favorite/addFavorite", new { });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AddFavorite_Vehicle_Returns200()
    {
        var (token, userId) = await ApiTestHelper.CreateConfirmedUserAsync(_client, _factory);
        var authedClient = _factory.CreateClient();
        ApiTestHelper.SetBearer(authedClient, token);

        var payload = new { UserId = userId, Type = "vehicle", ItemId = _factory.GetSeededVehicleId() };
        var response = await authedClient.PostAsJsonAsync("/api/Favorite/addFavorite", payload);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // --- GetFavoriteVehicleIdsByUserId ---

    [Fact]
    public async Task GetFavoriteVehicleIds_WithoutToken_Returns401()
    {
        var response = await _client.GetAsync("/api/Favorite/getFavoriteVehicleIdsByUserId/someuser");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetFavoriteVehicleIds_OwnUser_Returns200()
    {
        var (token, userId) = await ApiTestHelper.CreateConfirmedUserAsync(_client, _factory);
        var authedClient = _factory.CreateClient();
        ApiTestHelper.SetBearer(authedClient, token);

        var response = await authedClient.GetAsync($"/api/Favorite/getFavoriteVehicleIdsByUserId/{userId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // --- DeleteFavoriteVehicle ---

    [Fact]
    public async Task DeleteFavoriteVehicle_WithoutToken_Returns401()
    {
        var response = await _client.DeleteAsync("/api/Favorite/deleteFavoriteVehicle/user1/1");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DeleteFavoriteVehicle_AfterAdding_Returns200()
    {
        var (token, userId) = await ApiTestHelper.CreateConfirmedUserAsync(_client, _factory);
        var authedClient = _factory.CreateClient();
        ApiTestHelper.SetBearer(authedClient, token);

        var vehicleId = _factory.GetSeededVehicleId();
        await authedClient.PostAsJsonAsync("/api/Favorite/addFavorite",
            new { UserId = userId, Type = "vehicle", ItemId = vehicleId });

        var response = await authedClient.DeleteAsync($"/api/Favorite/deleteFavoriteVehicle/{userId}/{vehicleId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
