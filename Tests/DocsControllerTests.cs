using System.Net;
using System.Net.Http.Json;
using parrotsAPI2.Tests.Helpers;

namespace parrotsAPI2.Tests;

public class DocsControllerTests : IClassFixture<ParrotsWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly ParrotsWebApplicationFactory _factory;

    public DocsControllerTests(ParrotsWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    // --- List docs ---

    [Fact]
    public async Task ListDocs_WithoutToken_Returns401()
    {
        var response = await _client.GetAsync("/api/account/admin/docs");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ListDocs_NonAdmin_Returns403()
    {
        var (token, _) = await ApiTestHelper.CreateConfirmedUserAsync(_client, _factory);
        var authedClient = _factory.CreateClient();
        ApiTestHelper.SetBearer(authedClient, token);

        var response = await authedClient.GetAsync("/api/account/admin/docs");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ListDocs_Admin_Returns200()
    {
        var (token, _) = await ApiTestHelper.CreateAdminUserAsync(_client, _factory);
        var authedClient = _factory.CreateClient();
        ApiTestHelper.SetBearer(authedClient, token);

        var response = await authedClient.GetAsync("/api/account/admin/docs");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ListDocs_Admin_DoesNotIncludeServerGuide()
    {
        var (token, _) = await ApiTestHelper.CreateAdminUserAsync(_client, _factory);
        var authedClient = _factory.CreateClient();
        ApiTestHelper.SetBearer(authedClient, token);

        var response = await authedClient.GetAsync("/api/account/admin/docs");
        var files = await response.Content.ReadFromJsonAsync<List<string>>();

        Assert.NotNull(files);
        Assert.DoesNotContain(files, f => f.Contains("server-guide", StringComparison.OrdinalIgnoreCase));
    }

    // --- Get doc ---

    [Fact]
    public async Task GetDoc_WithoutToken_Returns401()
    {
        var response = await _client.GetAsync("/api/account/admin/docs/docs/TODO.txt");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetDoc_NonAdmin_Returns403()
    {
        var (token, _) = await ApiTestHelper.CreateConfirmedUserAsync(_client, _factory);
        var authedClient = _factory.CreateClient();
        ApiTestHelper.SetBearer(authedClient, token);

        var response = await authedClient.GetAsync("/api/account/admin/docs/docs/TODO.txt");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetDoc_ServerGuide_Returns403()
    {
        var (token, _) = await ApiTestHelper.CreateAdminUserAsync(_client, _factory);
        var authedClient = _factory.CreateClient();
        ApiTestHelper.SetBearer(authedClient, token);

        var response = await authedClient.GetAsync("/api/account/admin/docs/docs/server-guide.txt");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetDoc_NonExistentFile_Returns404()
    {
        var (token, _) = await ApiTestHelper.CreateAdminUserAsync(_client, _factory);
        var authedClient = _factory.CreateClient();
        ApiTestHelper.SetBearer(authedClient, token);

        var response = await authedClient.GetAsync("/api/account/admin/docs/docs/does-not-exist.txt");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // --- Save doc ---

    [Fact]
    public async Task SaveDoc_WithoutToken_Returns401()
    {
        var response = await _client.PutAsJsonAsync("/api/account/admin/docs/docs/TODO.txt", new { content = "x" });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SaveDoc_NonAdmin_Returns403()
    {
        var (token, _) = await ApiTestHelper.CreateConfirmedUserAsync(_client, _factory);
        var authedClient = _factory.CreateClient();
        ApiTestHelper.SetBearer(authedClient, token);

        var response = await authedClient.PutAsJsonAsync("/api/account/admin/docs/docs/TODO.txt", new { content = "x" });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task SaveDoc_ServerGuide_Returns403()
    {
        var (token, _) = await ApiTestHelper.CreateAdminUserAsync(_client, _factory);
        var authedClient = _factory.CreateClient();
        ApiTestHelper.SetBearer(authedClient, token);

        var response = await authedClient.PutAsJsonAsync("/api/account/admin/docs/docs/server-guide.txt", new { content = "x" });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
