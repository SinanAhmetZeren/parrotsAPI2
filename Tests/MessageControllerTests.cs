using System.Net;
using parrotsAPI2.Tests.Helpers;

namespace parrotsAPI2.Tests;

public class MessageControllerTests : IClassFixture<ParrotsWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly ParrotsWebApplicationFactory _factory;

    public MessageControllerTests(ParrotsWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    // --- getMessageByUserId ---

    [Fact]
    public async Task GetMessagesByUserId_WithoutToken_Returns401()
    {
        var response = await _client.GetAsync("/api/Message/getMessageByuserId/someuser");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetMessagesByUserId_WrongUser_Returns403()
    {
        var (token, _) = await ApiTestHelper.CreateConfirmedUserAsync(_client, _factory);
        var authedClient = _factory.CreateClient();
        ApiTestHelper.SetBearer(authedClient, token);

        var response = await authedClient.GetAsync("/api/Message/getMessageByuserId/someone-else");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetMessagesByUserId_OwnUser_Returns200()
    {
        var (token, userId) = await ApiTestHelper.CreateConfirmedUserAsync(_client, _factory);
        var authedClient = _factory.CreateClient();
        ApiTestHelper.SetBearer(authedClient, token);

        var response = await authedClient.GetAsync($"/api/Message/getMessageByuserId/{userId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // --- getMessagesBetweenUsers ---

    [Fact]
    public async Task GetMessagesBetweenUsers_WithoutToken_Returns401()
    {
        var response = await _client.GetAsync("/api/Message/getMessagesBetweenUsers/user1/user2");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetMessagesBetweenUsers_NotInConversation_Returns403()
    {
        var (token, _) = await ApiTestHelper.CreateConfirmedUserAsync(_client, _factory);
        var authedClient = _factory.CreateClient();
        ApiTestHelper.SetBearer(authedClient, token);

        var response = await authedClient.GetAsync("/api/Message/getMessagesBetweenUsers/user1/user2");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetMessagesBetweenUsers_AsUser1_Returns200()
    {
        var (token, userId) = await ApiTestHelper.CreateConfirmedUserAsync(_client, _factory);
        var (_, userId2) = await ApiTestHelper.CreateConfirmedUserAsync(_client, _factory);
        var authedClient = _factory.CreateClient();
        ApiTestHelper.SetBearer(authedClient, token);

        var response = await authedClient.GetAsync($"/api/Message/getMessagesBetweenUsers/{userId}/{userId2}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetMessagesBetweenUsers_AsUser2_Returns200()
    {
        var (token, userId) = await ApiTestHelper.CreateConfirmedUserAsync(_client, _factory);
        var (token2, userId2) = await ApiTestHelper.CreateConfirmedUserAsync(_client, _factory);
        var authedClient = _factory.CreateClient();
        ApiTestHelper.SetBearer(authedClient, token2);

        var response = await authedClient.GetAsync($"/api/Message/getMessagesBetweenUsers/{userId}/{userId2}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
