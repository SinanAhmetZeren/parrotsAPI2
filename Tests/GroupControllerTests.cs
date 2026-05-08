using System.Net;
using System.Net.Http.Json;
using Newtonsoft.Json;
using parrotsAPI2.Tests.Helpers;

namespace parrotsAPI2.Tests;

public class GroupControllerTests : IClassFixture<ParrotsWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly ParrotsWebApplicationFactory _factory;

    public GroupControllerTests(ParrotsWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<int> CreateGroupAsync(HttpClient authedClient, string userId, string name = "G")
    {
        var resp = await authedClient.PostAsJsonAsync("/api/Group", new { Name = name, CreatorId = userId });
        var body = await resp.Content.ReadAsStringAsync();
        Assert.True(resp.IsSuccessStatusCode, $"CreateGroup failed ({resp.StatusCode}): {body}");
        dynamic? data = JsonConvert.DeserializeObject(body);
        return (int)data!.id;
    }

    // --- CreateGroup ---

    [Fact]
    public async Task CreateGroup_WithoutToken_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/Group", new { Name = "G", CreatorId = "user1" });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateGroup_WithToken_Returns200()
    {
        var (token, userId) = await ApiTestHelper.CreateConfirmedUserAsync(_client, _factory, "grp_create");
        var authedClient = _factory.CreateClient();
        ApiTestHelper.SetBearer(authedClient, token);

        var response = await authedClient.PostAsJsonAsync("/api/Group", new { Name = "Test Group", CreatorId = userId });
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, $"Expected OK but got {response.StatusCode}: {body}");
    }

    // --- AddMember ---

    [Fact]
    public async Task AddMember_WithoutToken_Returns401()
    {
        var response = await _client.PostAsync("/api/Group/1/add/user1?requesterId=user2", null);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AddMember_ByCreator_Returns200()
    {
        var (token1, userId1) = await ApiTestHelper.CreateConfirmedUserAsync(_client, _factory, "grp_adm1");
        var (_, userId2) = await ApiTestHelper.CreateConfirmedUserAsync(_client, _factory, "grp_adm2");
        var authedClient = _factory.CreateClient();
        ApiTestHelper.SetBearer(authedClient, token1);

        var groupId = await CreateGroupAsync(authedClient, userId1);
        var response = await authedClient.PostAsync($"/api/Group/{groupId}/add/{userId2}?requesterId={userId1}", null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AddMember_ByNonCreator_Returns400()
    {
        var (token1, userId1) = await ApiTestHelper.CreateConfirmedUserAsync(_client, _factory, "grp_nc1");
        var (token2, userId2) = await ApiTestHelper.CreateConfirmedUserAsync(_client, _factory, "grp_nc2");
        var (_, userId3) = await ApiTestHelper.CreateConfirmedUserAsync(_client, _factory, "grp_nc3");
        var authedClient1 = _factory.CreateClient();
        ApiTestHelper.SetBearer(authedClient1, token1);
        var authedClient2 = _factory.CreateClient();
        ApiTestHelper.SetBearer(authedClient2, token2);

        var groupId = await CreateGroupAsync(authedClient1, userId1);
        var response = await authedClient2.PostAsync($"/api/Group/{groupId}/add/{userId3}?requesterId={userId2}", null);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // --- RemoveMember ---

    [Fact]
    public async Task RemoveMember_WithoutToken_Returns401()
    {
        var response = await _client.DeleteAsync("/api/Group/1/remove/user1?requesterId=user2");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RemoveMember_ByCreator_Returns200()
    {
        var (token1, userId1) = await ApiTestHelper.CreateConfirmedUserAsync(_client, _factory, "grp_rm1");
        var (_, userId2) = await ApiTestHelper.CreateConfirmedUserAsync(_client, _factory, "grp_rm2");
        var authedClient = _factory.CreateClient();
        ApiTestHelper.SetBearer(authedClient, token1);

        var groupId = await CreateGroupAsync(authedClient, userId1);
        await authedClient.PostAsync($"/api/Group/{groupId}/add/{userId2}?requesterId={userId1}", null);
        var response = await authedClient.DeleteAsync($"/api/Group/{groupId}/remove/{userId2}?requesterId={userId1}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // --- ExitGroup ---

    [Fact]
    public async Task ExitGroup_WithoutToken_Returns401()
    {
        var response = await _client.DeleteAsync("/api/Group/1/exit/user1");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ExitGroup_AsMember_Returns200()
    {
        var (token1, userId1) = await ApiTestHelper.CreateConfirmedUserAsync(_client, _factory, "grp_exit1");
        var (token2, userId2) = await ApiTestHelper.CreateConfirmedUserAsync(_client, _factory, "grp_exit2");
        var authedClient1 = _factory.CreateClient();
        ApiTestHelper.SetBearer(authedClient1, token1);
        var authedClient2 = _factory.CreateClient();
        ApiTestHelper.SetBearer(authedClient2, token2);

        var groupId = await CreateGroupAsync(authedClient1, userId1);
        await authedClient1.PostAsync($"/api/Group/{groupId}/add/{userId2}?requesterId={userId1}", null);
        var response = await authedClient2.DeleteAsync($"/api/Group/{groupId}/exit/{userId2}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ExitGroup_NotMember_Returns400()
    {
        var (token1, userId1) = await ApiTestHelper.CreateConfirmedUserAsync(_client, _factory, "grp_exnm1");
        var (token2, userId2) = await ApiTestHelper.CreateConfirmedUserAsync(_client, _factory, "grp_exnm2");
        var authedClient1 = _factory.CreateClient();
        ApiTestHelper.SetBearer(authedClient1, token1);
        var authedClient2 = _factory.CreateClient();
        ApiTestHelper.SetBearer(authedClient2, token2);

        var groupId = await CreateGroupAsync(authedClient1, userId1);
        var response = await authedClient2.DeleteAsync($"/api/Group/{groupId}/exit/{userId2}");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // --- GetGroupById ---

    [Fact]
    public async Task GetGroupById_WithoutToken_Returns401()
    {
        var response = await _client.GetAsync("/api/Group/1?userId=user1");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetGroupById_AsMember_Returns200()
    {
        var (token, userId) = await ApiTestHelper.CreateConfirmedUserAsync(_client, _factory, "grp_get1");
        var authedClient = _factory.CreateClient();
        ApiTestHelper.SetBearer(authedClient, token);

        var groupId = await CreateGroupAsync(authedClient, userId);
        var response = await authedClient.GetAsync($"/api/Group/{groupId}?userId={userId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetGroupById_AsNonMember_Returns400()
    {
        var (token1, userId1) = await ApiTestHelper.CreateConfirmedUserAsync(_client, _factory, "grp_nm1");
        var (token2, userId2) = await ApiTestHelper.CreateConfirmedUserAsync(_client, _factory, "grp_nm2");
        var authedClient1 = _factory.CreateClient();
        ApiTestHelper.SetBearer(authedClient1, token1);
        var authedClient2 = _factory.CreateClient();
        ApiTestHelper.SetBearer(authedClient2, token2);

        var groupId = await CreateGroupAsync(authedClient1, userId1);
        var response = await authedClient2.GetAsync($"/api/Group/{groupId}?userId={userId2}");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // --- GetGroupMessages ---

    [Fact]
    public async Task GetGroupMessages_WithoutToken_Returns401()
    {
        var response = await _client.GetAsync("/api/Group/1/messages/user1");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetGroupMessages_AsMember_Returns200()
    {
        var (token, userId) = await ApiTestHelper.CreateConfirmedUserAsync(_client, _factory, "grp_msg1");
        var authedClient = _factory.CreateClient();
        ApiTestHelper.SetBearer(authedClient, token);

        var groupId = await CreateGroupAsync(authedClient, userId);
        var response = await authedClient.GetAsync($"/api/Group/{groupId}/messages/{userId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetGroupMessages_AsNonMember_Returns400()
    {
        var (token1, userId1) = await ApiTestHelper.CreateConfirmedUserAsync(_client, _factory, "grp_mnm1");
        var (token2, userId2) = await ApiTestHelper.CreateConfirmedUserAsync(_client, _factory, "grp_mnm2");
        var authedClient1 = _factory.CreateClient();
        ApiTestHelper.SetBearer(authedClient1, token1);
        var authedClient2 = _factory.CreateClient();
        ApiTestHelper.SetBearer(authedClient2, token2);

        var groupId = await CreateGroupAsync(authedClient1, userId1);
        var response = await authedClient2.GetAsync($"/api/Group/{groupId}/messages/{userId2}");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
