using System.Net;
using parrotsAPI2.Tests.Helpers;

namespace parrotsAPI2.Tests;

public class HealthControllerTests : IClassFixture<ParrotsWebApplicationFactory>
{
    private readonly HttpClient _client;

    public HealthControllerTests(ParrotsWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Health_Anonymous_Returns200()
    {
        var response = await _client.GetAsync("/api/Health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}

public class MetricsControllerTests : IClassFixture<ParrotsWebApplicationFactory>
{
    private readonly HttpClient _client;

    public MetricsControllerTests(ParrotsWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Theory]
    [InlineData("/api/Metrics/weeklyPurchases")]
    [InlineData("/api/Metrics/weeklyTransactions")]
    [InlineData("/api/Metrics/weeklyVoyages")]
    [InlineData("/api/Metrics/weeklyVehicles")]
    [InlineData("/api/Metrics/weeklyUsers")]
    [InlineData("/api/Metrics/weeklyBids")]
    [InlineData("/api/Metrics/weeklyMessages")]
    public async Task MetricsEndpoint_WithoutToken_Returns401(string url)
    {
        var response = await _client.GetAsync(url);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
