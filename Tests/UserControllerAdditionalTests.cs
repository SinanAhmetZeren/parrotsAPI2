using System.Net;
using System.Net.Http.Json;
using parrotsAPI2.Tests.Helpers;

namespace parrotsAPI2.Tests;

public class UserControllerAdditionalTests : IClassFixture<ParrotsWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly ParrotsWebApplicationFactory _factory;

    public UserControllerAdditionalTests(ParrotsWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    // --- getUserByPublicId ---

    [Fact]
    public async Task GetUserByPublicId_WithToken_Returns200()
    {
        var (token, _) = await ApiTestHelper.CreateConfirmedUserAsync(_client, _factory);
        var authedClient = _factory.CreateClient();
        ApiTestHelper.SetBearer(authedClient, token);

        // Non-existent public ID still returns 200 with null data (service pattern)
        var response = await authedClient.GetAsync("/api/User/getUserByPublicId/nonexistent-public-id");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetUserByPublicId_WithoutToken_Returns401()
    {
        var response = await _client.GetAsync("/api/User/getUserByPublicId/someid");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // --- UpdateUser (PUT) ---

    [Fact]
    public async Task UpdateUser_WithoutToken_Returns401()
    {
        var response = await _client.PutAsJsonAsync("/api/User/UpdateUser", new { Id = "someuser", UserName = "newname" });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UpdateUser_WrongUser_Returns403()
    {
        var (token, _) = await ApiTestHelper.CreateConfirmedUserAsync(_client, _factory);
        var authedClient = _factory.CreateClient();
        ApiTestHelper.SetBearer(authedClient, token);

        var response = await authedClient.PutAsJsonAsync("/api/User/UpdateUser", new { Id = "someone-else", UserName = "newname" });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UpdateUser_OwnUser_Returns200()
    {
        var (token, userId) = await ApiTestHelper.CreateConfirmedUserAsync(_client, _factory);
        var authedClient = _factory.CreateClient();
        ApiTestHelper.SetBearer(authedClient, token);

        var form = new MultipartFormDataContent();
        form.Add(new StringContent(userId), "Id");
        form.Add(new StringContent($"u_{Guid.NewGuid():N}".Substring(0, 10)), "UserName");
        var response = await authedClient.PutAsync("/api/User/UpdateUser", form);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // --- PurchaseCoins ---

    [Fact]
    public async Task PurchaseCoins_WithoutToken_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/User/PurchaseCoins", new { });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PurchaseCoins_WrongUser_Returns403()
    {
        var (token, _) = await ApiTestHelper.CreateConfirmedUserAsync(_client, _factory);
        var authedClient = _factory.CreateClient();
        ApiTestHelper.SetBearer(authedClient, token);

        var payload = new { UserId = "someone-else", Coins = 100, EurAmount = 1.0, PaymentProviderId = "pay_test" };
        var response = await authedClient.PostAsJsonAsync("/api/User/PurchaseCoins", payload);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task PurchaseCoins_OwnUser_Returns200()
    {
        var (token, userId) = await ApiTestHelper.CreateConfirmedUserAsync(_client, _factory);
        var authedClient = _factory.CreateClient();
        ApiTestHelper.SetBearer(authedClient, token);

        var payload = new { UserId = userId, Coins = 100, EurAmount = 1.0, PaymentProviderId = $"pay_{Guid.NewGuid():N}" };
        var response = await authedClient.PostAsJsonAsync("/api/User/PurchaseCoins", payload);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
