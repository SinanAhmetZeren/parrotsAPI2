using System.Net;
using System.Net.Http.Json;
using parrotsAPI2.Tests.Helpers;

namespace parrotsAPI2.Tests;

public class BookmarkControllerTests : IClassFixture<ParrotsWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly ParrotsWebApplicationFactory _factory;

    public BookmarkControllerTests(ParrotsWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    // --- GetBookmarks ---

    [Fact]
    public async Task GetBookmarks_WithoutToken_Returns401()
    {
        var response = await _client.GetAsync("/api/Bookmark/getBookmarks");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetBookmarks_WithToken_Returns200()
    {
        var (token, _) = await ApiTestHelper.CreateConfirmedUserAsync(_client, _factory);
        var authedClient = _factory.CreateClient();
        ApiTestHelper.SetBearer(authedClient, token);

        var response = await authedClient.GetAsync("/api/Bookmark/getBookmarks");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // --- GetBookmarkedUserIds ---

    [Fact]
    public async Task GetBookmarkedUserIds_WithoutToken_Returns401()
    {
        var response = await _client.GetAsync("/api/Bookmark/getBookmarkedUserIds");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetBookmarkedUserIds_WithToken_Returns200()
    {
        var (token, _) = await ApiTestHelper.CreateConfirmedUserAsync(_client, _factory);
        var authedClient = _factory.CreateClient();
        ApiTestHelper.SetBearer(authedClient, token);

        var response = await authedClient.GetAsync("/api/Bookmark/getBookmarkedUserIds");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // --- AddBookmark ---

    [Fact]
    public async Task AddBookmark_WithoutToken_Returns401()
    {
        var response = await _client.PostAsync("/api/Bookmark/addBookmark/someuser", null);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AddBookmark_OtherUser_Returns200()
    {
        var (token1, userId1) = await ApiTestHelper.CreateConfirmedUserAsync(_client, _factory, "bookmarker");
        var (_, userId2) = await ApiTestHelper.CreateConfirmedUserAsync(_client, _factory, "bookmarkee");
        var authedClient = _factory.CreateClient();
        ApiTestHelper.SetBearer(authedClient, token1);

        var response = await authedClient.PostAsync($"/api/Bookmark/addBookmark/{userId2}", null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AddBookmark_Self_Returns200WithFailureInBody()
    {
        var (token, userId) = await ApiTestHelper.CreateConfirmedUserAsync(_client, _factory, "selfbookmark");
        var authedClient = _factory.CreateClient();
        ApiTestHelper.SetBearer(authedClient, token);

        var response = await authedClient.PostAsync($"/api/Bookmark/addBookmark/{userId}", null);
        // Controller returns Ok() even on service failure — check body
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("false", body, StringComparison.OrdinalIgnoreCase);
    }

    // --- RemoveBookmark ---

    [Fact]
    public async Task RemoveBookmark_WithoutToken_Returns401()
    {
        var response = await _client.DeleteAsync("/api/Bookmark/removeBookmark/someuser");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RemoveBookmark_AfterAdding_Returns200()
    {
        var (token, _) = await ApiTestHelper.CreateConfirmedUserAsync(_client, _factory, "bm_rem1");
        var (_, userId2) = await ApiTestHelper.CreateConfirmedUserAsync(_client, _factory, "bm_rem2");
        var authedClient = _factory.CreateClient();
        ApiTestHelper.SetBearer(authedClient, token);

        await authedClient.PostAsync($"/api/Bookmark/addBookmark/{userId2}", null);
        var response = await authedClient.DeleteAsync($"/api/Bookmark/removeBookmark/{userId2}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task RemoveBookmark_NotFound_Returns404()
    {
        var (token, _) = await ApiTestHelper.CreateConfirmedUserAsync(_client, _factory, "bm_notfound");
        var authedClient = _factory.CreateClient();
        ApiTestHelper.SetBearer(authedClient, token);

        var response = await authedClient.DeleteAsync("/api/Bookmark/removeBookmark/nonexistent-user-id");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
