using System.Net;
using System.Net.Http.Json;
using parrotsAPI2.Tests.Helpers;

namespace parrotsAPI2.Tests;

public class UserControllerTests : IClassFixture<ParrotsWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly ParrotsWebApplicationFactory _factory;

    public UserControllerTests(ParrotsWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    // --- GetUserById ---

    [Fact]
    public async Task GetUserById_WithoutToken_Returns401()
    {
        var response = await _client.GetAsync("/api/User/getUserById/someuser");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetUserById_WithToken_Returns200()
    {
        var (token, userId) = await ApiTestHelper.CreateConfirmedUserAsync(_client, _factory);
        var authedClient = _factory.CreateClient();
        ApiTestHelper.SetBearer(authedClient, token);

        var response = await authedClient.GetAsync($"/api/User/getUserById/{userId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // --- SearchUsers (anonymous) ---

    [Fact]
    public async Task SearchUsers_Anonymous_Returns200()
    {
        var response = await _client.GetAsync("/api/User/searchUsers/test");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // --- PatchUser ---

    [Fact]
    public async Task PatchUser_WithoutToken_Returns401()
    {
        var response = await _client.PatchAsync("/api/User/PatchUser/someuser",
            new StringContent("[]", System.Text.Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PatchUser_WrongUser_Returns403()
    {
        var (token, _) = await ApiTestHelper.CreateConfirmedUserAsync(_client, _factory);
        var authedClient = _factory.CreateClient();
        ApiTestHelper.SetBearer(authedClient, token);

        var response = await authedClient.PatchAsync("/api/User/PatchUser/someone-else",
            new StringContent("[]", System.Text.Encoding.UTF8, "application/json-patch+json"));
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // --- ClaimFreeCoins ---

    [Fact]
    public async Task ClaimFreeCoins_WithoutToken_Returns401()
    {
        var response = await _client.PostAsync("/api/User/ClaimFreeCoins", null);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ClaimFreeCoins_WithToken_Returns200()
    {
        var (token, _) = await ApiTestHelper.CreateConfirmedUserAsync(_client, _factory);
        var authedClient = _factory.CreateClient();
        ApiTestHelper.SetBearer(authedClient, token);

        var response = await authedClient.PostAsync("/api/User/ClaimFreeCoins", null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // --- SendParrotCoins ---

    [Fact]
    public async Task SendParrotCoins_InsufficientBalance_ReturnsBadRequest()
    {
        var (token, userId) = await ApiTestHelper.CreateConfirmedUserAsync(_client, _factory);
        var (_, receiverId) = await ApiTestHelper.CreateConfirmedUserAsync(_client, _factory);

        var authedClient = _factory.CreateClient();
        ApiTestHelper.SetBearer(authedClient, token);

        var payload = new { UserId = userId, ReceiverId = receiverId, Coins = 99999 };
        var response = await authedClient.PostAsJsonAsync("/api/User/SendParrotCoins", payload);
        Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task SendParrotCoins_WrongUser_Returns403()
    {
        var (token, _) = await ApiTestHelper.CreateConfirmedUserAsync(_client, _factory);
        var (_, receiverId) = await ApiTestHelper.CreateConfirmedUserAsync(_client, _factory);

        var authedClient = _factory.CreateClient();
        ApiTestHelper.SetBearer(authedClient, token);

        var payload = new { UserId = "someone-else", ReceiverId = receiverId, Coins = 10 };
        var response = await authedClient.PostAsJsonAsync("/api/User/SendParrotCoins", payload);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task SendParrotCoins_WithSufficientBalance_Returns200()
    {
        var (token, userId) = await ApiTestHelper.CreateConfirmedUserAsync(_client, _factory);
        var (_, receiverId) = await ApiTestHelper.CreateConfirmedUserAsync(_client, _factory);

        _factory.GiveCoins(userId, 100);

        var authedClient = _factory.CreateClient();
        ApiTestHelper.SetBearer(authedClient, token);

        var payload = new { UserId = userId, ReceiverId = receiverId, Coins = 10 };
        var response = await authedClient.PostAsJsonAsync("/api/User/SendParrotCoins", payload);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // --- ParrotCoinBalance ---

    [Fact]
    public async Task ParrotCoinBalance_OtherUser_Returns403()
    {
        var (token, _) = await ApiTestHelper.CreateConfirmedUserAsync(_client, _factory);
        var authedClient = _factory.CreateClient();
        ApiTestHelper.SetBearer(authedClient, token);

        var response = await authedClient.GetAsync("/api/User/parrotCoinBalance/someone-else");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ParrotCoinBalance_OwnUser_Returns200()
    {
        var (token, userId) = await ApiTestHelper.CreateConfirmedUserAsync(_client, _factory);
        var authedClient = _factory.CreateClient();
        ApiTestHelper.SetBearer(authedClient, token);

        var response = await authedClient.GetAsync($"/api/User/parrotCoinBalance/{userId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
