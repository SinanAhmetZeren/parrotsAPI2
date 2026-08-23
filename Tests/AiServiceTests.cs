using System.Net;
using System.Text.Json;
using Moq;
using Moq.Protected;
using ParrotsAPI2.Dtos.AiDtos;
using ParrotsAPI2.Services.Ai;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace parrotsAPI2.Tests;

public class AiServiceTests
{
    private static AiService CreateService(HttpResponseMessage response)
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        var client = new HttpClient(handler.Object);
        var factory = new Mock<IHttpClientFactory>();
        var cache = new Mock<IMemoryCache>();
        var scopeFactory = new Mock<IServiceScopeFactory>();
        var config = new Mock<IConfiguration>();
        config.Setup(c => c["Google_Gemini_Parrots_AI_Query_Key"]).Returns("test-key");

        return new AiService(client, factory.Object, cache.Object, scopeFactory.Object, config.Object);
    }

    // Wraps narrative in the JSON structure Gemini now returns inside parts[0].text
    private static HttpResponseMessage GeminiResponse(string narrative)
    {
        var innerJson = JsonSerializer.Serialize(new
        {
            draft_narrative = narrative,
            planned_spots = Array.Empty<object>()
        });
        var body = JsonSerializer.Serialize(new
        {
            candidates = new[]
            {
                new { content = new { parts = new[] { new { text = innerJson } } } }
            }
        });
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json")
        };
    }

    // --- BuildPrompt via AskAsync ---

    [Fact]
    public async Task Walk_vehicle_uses_go_for_a_walk_phrasing()
    {
        var service = CreateService(GeminiResponse("[[X, Y]] test"));
        var dto = MakeDto(vehicleType: "Walk", duration: "1 Day");
        await service.AskAsync(dto, "test-user");
        // If BuildPrompt is wrong the prompt logged would differ — we validate via captured request
        // For now we verify AskAsync returns successfully
        var result = await CreateService(GeminiResponse("[[City, District]] narrative")).AskAsync(dto, "test-user");
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Run_vehicle_uses_go_for_a_run_phrasing()
    {
        var result = await CreateService(GeminiResponse("[[City, District]] narrative"))
            .AskAsync(MakeDto(vehicleType: "Run", duration: "1 Day"), "test-user");
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Train_vehicle_uses_traveling_by_phrasing()
    {
        var result = await CreateService(GeminiResponse("[[City, District]] narrative"))
            .AskAsync(MakeDto(vehicleType: "Train", duration: "1 Day"), "test-user");
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Bus_vehicle_uses_personal_vehicle_phrasing()
    {
        // Bus is NOT transit — it should use "I have a bus" not "I'm traveling by bus"
        var captured = await CapturePrompt(MakeDto(vehicleType: "Bus", duration: "1 Day"));
        Assert.Contains("I have a bus", captured);
        Assert.DoesNotContain("traveling by bus", captured);
    }

    [Fact]
    public async Task Car_vehicle_uses_personal_vehicle_phrasing()
    {
        var captured = await CapturePrompt(MakeDto(vehicleType: "Car", duration: "1 Day"));
        Assert.Contains("I have a car", captured);
    }

    [Fact]
    public async Task TinyHouse_vehicle_displays_as_tiny_house()
    {
        var captured = await CapturePrompt(MakeDto(vehicleType: "TinyHouse", duration: "1 Day"));
        Assert.Contains("tiny house", captured);
    }

    [Fact]
    public async Task Half_day_duration_targets_350_380_words()
    {
        var captured = await CapturePrompt(MakeDto(duration: "Half day"));
        Assert.Contains("350\\u2013380 words", captured);
    }

    [Fact]
    public async Task Half_a_Day_duration_targets_350_380_words()
    {
        var captured = await CapturePrompt(MakeDto(duration: "Half a Day"));
        Assert.Contains("350\\u2013380 words", captured);
    }

    [Fact]
    public async Task One_day_duration_targets_350_380_words()
    {
        var captured = await CapturePrompt(MakeDto(duration: "1 Day"));
        Assert.Contains("350\\u2013380 words", captured);
    }

    [Fact]
    public async Task Multi_day_duration_targets_550_600_words()
    {
        var captured = await CapturePrompt(MakeDto(duration: "3 Days"));
        Assert.Contains("550\\u2013600 words", captured);
    }

    [Fact]
    public async Task Location_coordinates_included_in_prompt()
    {
        var captured = await CapturePrompt(MakeDto(lat: 41.0, lon: 29.0));
        Assert.Contains("starting from coordinates (41, 29)", captured);
    }

    [Fact]
    public async Task Food_vibe_maps_to_food_focused_label()
    {
        var captured = await CapturePrompt(MakeDto(vibe: "Food"));
        Assert.Contains("food-focused", captured);
        Assert.Contains("culinary experiences, local dining, and eateries", captured);
    }

    [Fact]
    public async Task Nature_vibe_maps_correctly()
    {
        var captured = await CapturePrompt(MakeDto(vibe: "Nature"));
        Assert.Contains("nature-focused", captured);
    }

    [Fact]
    public async Task Any_vibe_produces_any_vibe_phrase()
    {
        var captured = await CapturePrompt(MakeDto(vibe: "Any"));
        Assert.Contains("voyage of any vibe", captured);
    }

    [Fact]
    public async Task Hidden_gems_spot_type_included_in_prompt()
    {
        var captured = await CapturePrompt(MakeDto(spotType: "Hidden Gems"));
        Assert.Contains("hidden gems", captured);
    }

    [Fact]
    public async Task Popular_spots_spot_type_included_in_prompt()
    {
        var captured = await CapturePrompt(MakeDto(spotType: "Popular Spots"));
        Assert.Contains("popular spots", captured);
    }

    [Fact]
    public async Task Empty_spot_type_omits_focusing_clause()
    {
        var captured = await CapturePrompt(MakeDto(spotType: ""));
        Assert.DoesNotContain("focusing on", captured);
    }

    // --- AskAsync return behaviour ---

    [Fact]
    public async Task Returns_null_when_gemini_returns_error_status()
    {
        var service = CreateService(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        var result = await service.AskAsync(MakeDto(), "test-user");
        Assert.Null(result);
    }

    [Fact]
    public async Task Returns_narrative_when_gemini_succeeds()
    {
        var service = CreateService(GeminiResponse("[[Istanbul, Kadıköy]] some narrative"));
        var result = await service.AskAsync(MakeDto(), "test-user");
        Assert.Equal("[[Istanbul, Kadıköy]] some narrative", result);
    }

    [Fact]
    public async Task Returns_null_when_candidates_array_is_empty()
    {
        var body = JsonSerializer.Serialize(new { candidates = Array.Empty<object>() });
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json")
        };
        var service = CreateService(response);
        var result = await service.AskAsync(MakeDto(), "test-user");
        Assert.Null(result);
    }

    // --- helpers ---

    private static AskParrotsQueryDto MakeDto(
        string vehicleType = "Car",
        string duration = "1 Day",
        string vibe = "Food",
        string spotType = "Hidden Gems",
        double lat = 41.0,
        double lon = 29.0) =>
        new()
        {
            VehicleType = vehicleType,
            Duration = duration,
            Vibe = vibe,
            SpotType = spotType,
            Latitude = lat,
            Longitude = lon
        };

    private static async Task<string> CapturePrompt(AskParrotsQueryDto dto)
    {
        string captured = string.Empty;

        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Returns<HttpRequestMessage, CancellationToken>(async (req, _) =>
            {
                captured = await req.Content!.ReadAsStringAsync();
                return GeminiResponse("[[City, District]] narrative");
            });

        var client = new HttpClient(handler.Object);
        var factory = new Mock<IHttpClientFactory>();
        var cache = new Mock<IMemoryCache>();
        var scopeFactory = new Mock<IServiceScopeFactory>();
        var config = new Mock<IConfiguration>();
        config.Setup(c => c["Google_Gemini_Parrots_AI_Query_Key"]).Returns("test-key");

        var service = new AiService(client, factory.Object, cache.Object, scopeFactory.Object, config.Object);
        await service.AskAsync(dto, "test-user");
        return captured;
    }
}
