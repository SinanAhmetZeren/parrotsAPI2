using System.Net;
using System.Net.Http.Json;
using ParrotsAPI2.Dtos.BidDtos;
using ParrotsAPI2.Dtos.VoyageDtos;
using ParrotsAPI2.Models;
using parrotsAPI2.Tests.Helpers;

namespace parrotsAPI2.Tests;

public class BidControllerTests : IClassFixture<ParrotsWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly ParrotsWebApplicationFactory _factory;

    public BidControllerTests(ParrotsWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    // --- CreateBid ---

    [Fact]
    public async Task CreateBid_WithoutToken_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/Bid/createBid", new { });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateBid_WrongUserId_Returns403()
    {
        var (token, _) = await ApiTestHelper.CreateConfirmedUserAsync(_client, _factory);
        var authedClient = _factory.CreateClient();
        ApiTestHelper.SetBearer(authedClient, token);

        var payload = new { UserId = "someone-else", VoyageId = 1, PersonCount = 2, OfferPrice = 100, Message = "Hi" };
        var response = await authedClient.PostAsJsonAsync("/api/Bid/createBid", payload);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateBid_ValidPayload_Returns200()
    {
        var (ownerToken, ownerId) = await ApiTestHelper.CreateConfirmedUserAsync(_client, _factory);
        var (bidderToken, bidderId) = await ApiTestHelper.CreateConfirmedUserAsync(_client, _factory);

        _factory.GiveCoins(ownerId, 500);
        var voyageId = await CreateConfirmedVoyageAsync(ownerToken, ownerId);

        var authedClient = _factory.CreateClient();
        ApiTestHelper.SetBearer(authedClient, bidderToken);

        var payload = new { UserId = bidderId, VoyageId = voyageId, PersonCount = 2, OfferPrice = 100, Message = "Hi" };
        var response = await authedClient.PostAsJsonAsync("/api/Bid/createBid", payload);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // --- ChangeBid ---

    [Fact]
    public async Task ChangeBid_NotBidOwner_Returns403()
    {
        var (ownerToken, ownerId) = await ApiTestHelper.CreateConfirmedUserAsync(_client, _factory);
        var (bidderToken, bidderId) = await ApiTestHelper.CreateConfirmedUserAsync(_client, _factory);
        var (otherToken, _) = await ApiTestHelper.CreateConfirmedUserAsync(_client, _factory);

        _factory.GiveCoins(ownerId, 500);
        var voyageId = await CreateConfirmedVoyageAsync(ownerToken, ownerId);
        var bidId = await CreateBidAsync(bidderToken, bidderId, voyageId);

        var authedClient = _factory.CreateClient();
        ApiTestHelper.SetBearer(authedClient, otherToken);

        var payload = new { Id = bidId, PersonCount = 3, OfferPrice = 200, Message = "Updated" };
        var response = await authedClient.PostAsJsonAsync("/api/Bid/changeBid", payload);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ChangeBid_BidOwner_Returns200()
    {
        var (ownerToken, ownerId) = await ApiTestHelper.CreateConfirmedUserAsync(_client, _factory);
        var (bidderToken, bidderId) = await ApiTestHelper.CreateConfirmedUserAsync(_client, _factory);

        _factory.GiveCoins(ownerId, 500);
        var voyageId = await CreateConfirmedVoyageAsync(ownerToken, ownerId);
        var bidId = await CreateBidAsync(bidderToken, bidderId, voyageId);

        var authedClient = _factory.CreateClient();
        ApiTestHelper.SetBearer(authedClient, bidderToken);

        var payload = new { Id = bidId, PersonCount = 3, OfferPrice = 200, Message = "Updated" };
        var response = await authedClient.PostAsJsonAsync("/api/Bid/changeBid", payload);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // --- AcceptBid ---

    [Fact]
    public async Task AcceptBid_NotVoyageOwner_ReturnsBadRequest()
    {
        var (ownerToken, ownerId) = await ApiTestHelper.CreateConfirmedUserAsync(_client, _factory);
        var (bidderToken, bidderId) = await ApiTestHelper.CreateConfirmedUserAsync(_client, _factory);

        _factory.GiveCoins(ownerId, 500);
        var voyageId = await CreateConfirmedVoyageAsync(ownerToken, ownerId);
        var bidId = await CreateBidAsync(bidderToken, bidderId, voyageId);

        var authedClient = _factory.CreateClient();
        ApiTestHelper.SetBearer(authedClient, bidderToken); // bidder tries to accept

        var response = await authedClient.PostAsync($"/api/Bid/acceptbid?bidId={bidId}", null);
        Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AcceptBid_VoyageOwner_Returns200()
    {
        var (ownerToken, ownerId) = await ApiTestHelper.CreateConfirmedUserAsync(_client, _factory);
        var (bidderToken, bidderId) = await ApiTestHelper.CreateConfirmedUserAsync(_client, _factory);

        _factory.GiveCoins(ownerId, 500);
        var voyageId = await CreateConfirmedVoyageAsync(ownerToken, ownerId);
        var bidId = await CreateBidAsync(bidderToken, bidderId, voyageId);

        var authedClient = _factory.CreateClient();
        ApiTestHelper.SetBearer(authedClient, ownerToken);

        var response = await authedClient.PostAsync($"/api/Bid/acceptbid?bidId={bidId}", null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // --- DeleteBid ---

    [Fact]
    public async Task DeleteBid_NotVoyageOwner_ReturnsBadRequest()
    {
        var (ownerToken, ownerId) = await ApiTestHelper.CreateConfirmedUserAsync(_client, _factory);
        var (bidderToken, bidderId) = await ApiTestHelper.CreateConfirmedUserAsync(_client, _factory);

        _factory.GiveCoins(ownerId, 500);
        var voyageId = await CreateConfirmedVoyageAsync(ownerToken, ownerId);
        var bidId = await CreateBidAsync(bidderToken, bidderId, voyageId);

        var authedClient = _factory.CreateClient();
        ApiTestHelper.SetBearer(authedClient, bidderToken); // bidder tries to delete

        var response = await authedClient.DeleteAsync($"/api/Bid/deletebid?bidId={bidId}");
        Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task DeleteBid_VoyageOwner_Returns200()
    {
        var (ownerToken, ownerId) = await ApiTestHelper.CreateConfirmedUserAsync(_client, _factory);
        var (bidderToken, bidderId) = await ApiTestHelper.CreateConfirmedUserAsync(_client, _factory);

        _factory.GiveCoins(ownerId, 500);
        var voyageId = await CreateConfirmedVoyageAsync(ownerToken, ownerId);
        var bidId = await CreateBidAsync(bidderToken, bidderId, voyageId);

        var authedClient = _factory.CreateClient();
        ApiTestHelper.SetBearer(authedClient, ownerToken);

        var response = await authedClient.DeleteAsync($"/api/Bid/deletebid?bidId={bidId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // --- Helpers ---

    private async Task<int> CreateConfirmedVoyageAsync(string token, string userId)
    {
        var authedClient = _factory.CreateClient();
        ApiTestHelper.SetBearer(authedClient, token);

        var response = await authedClient.PostAsync("/api/Voyage/AddVoyage", VoyageControllerTests.BuildVoyageForm("Bid Test Voyage", _factory.GetSeededVehicleId(), userId));
        var body = await ApiTestHelper.DeserializeAsync<ServiceResponse<GetVoyageDto>>(response);
        var voyageId = body!.Data!.Id;

        await authedClient.PostAsync($"/api/Voyage/ConfirmVoyage/{voyageId}", null);
        return voyageId;
    }

    private async Task<int> CreateBidAsync(string token, string userId, int voyageId)
    {
        var authedClient = _factory.CreateClient();
        ApiTestHelper.SetBearer(authedClient, token);

        var payload = new { UserId = userId, VoyageId = voyageId, PersonCount = 2, OfferPrice = 100, Message = "Hi" };
        var response = await authedClient.PostAsJsonAsync("/api/Bid/createBid", payload);
        var body = await ApiTestHelper.DeserializeAsync<ServiceResponse<BidDto>>(response);
        return body!.Data!.Id;
    }
}
