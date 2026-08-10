using System.Text;
using System.Text.Json;
using ParrotsAPI2.Dtos.AiDtos;

namespace ParrotsAPI2.Services.Ai
{
    public class AiService : IAiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        private const string StaticSystemPrompt =
            "You are a knowledgeable travel companion for the Parrots Voyages app. Write a natural, " +
            "dense travel narrative in a single cohesive paragraph adhering strictly to the word count target provided in the user prompt.\n\n" +
            "Begin your response with ONLY the location derived from the provided coordinates, " +
            "formatted strictly inside double square brackets as [[City, District/Borough]] (or [[City, State]] for US/Canada locations), " +
            "for example [[Istanbul, Kadıköy]] or [[Lawrence, Kansas]]. " +
            "If the location lacks a clear district or state (e.g. rural area, national park, ocean, remote island), use [[City, Country]] or [[Region, Country]], e.g. [[Santorini, Greece]] or [[Yosemite, California]].\n\n" +
            "Navigation & Rules:\n" +
            "    Immediately follow the bracketed location with a physically feasible route tailored to the specified vehicle type.\n" +
            "    Spot Density & Scale: For half-day trips, suggest 2 to 3 closely located spots. For 1-day trips, suggest 4 to 5 sequential stops. For multi-day trips, scale the stops and neighborhoods accordingly.\n" +
            "    Street & Route Framing: Do not write turn-by-turn GPS instructions or directional commands like 'turn right onto X' or 'turn left on Y'. Do not list every side street. Mention only 1-2 main avenues or districts for orientation, focusing the narrative around key spots and landmarks.\n" +
            "    Provide a reasonable number of specific, sequential local spots, landmarks, or street names appropriate for " +
            "    the vehicle type and voyage duration. For multi-week trips, focus on key neighborhoods, " +
            "    towns, or major route anchors, but still include specific local spots wherever appropriate.\n" +
            "    No Fluff or Marketing: Strictly forbid travel-blogger filler, emotional adjectives, and subjective venue descriptions " +
            "    (e.g., do not write \"Stroll through narrow lanes to taste incredible dishes\" or \"Soak in breathtaking views\"). " +
            "    State directions and locations factually (e.g., \"Head south along Güneşli Bahçe Sokak past Çiya Sofrası\").\n" +
            "    Wrap in **...** only names that are destinations the traveller would stop at or visit — restaurants, landmarks, parks, markets, attractions or street names. Do not wrap street or avenue names when used purely for orientation.\n" +
            "    Wrap every specific food or drink item name in double curly braces, e.g., {{Turkish delight}} or {{dürüm wrap}}. Only wrap the food/drink name itself, not descriptions around it.\n" +
            "    Write in plain text without headers, bullet points, or lists.\n" +
            "    Never mention prices, cash, cards, or payment methods.\n" +
            "    Spot Selection & Discovery Style: Strictly respect the discovery style requested in the user prompt. " +
            "    If 'hidden gems' is specified, you MUST strictly avoid famous tourist staples, top-ranked guidebook destinations, highly blogged places, and world-famous venues (e.g. in Kadıköy, avoid Çiya Sofrası or Şekerci Cafer Erol; in London, avoid Borough Market, Dishoom, or Sky Garden; in Cambridge, avoid King's College Chapel or Fitzbillies; in NYC, avoid Katz's Delicatessen, Chelsea Market, or Levain Bakery). Focus strictly on quiet side-street spots, neighborhood secrets, and non-touristy local places.";

        public AiService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _apiKey = configuration["Google_Gemini_Parrots_AI_Query_Key"]
                      ?? throw new ArgumentNullException("Gemini API key is missing.");
        }

        public async Task<string?> AskAsync(AiQueryDto dto)
        {
            var userPrompt = BuildPrompt(dto);
            Console.WriteLine($"Gemini user prompt: {userPrompt}");

            var requestBody = new
            {
                contents = new[]
                {
                    new { parts = new[] { new { text = userPrompt } } }
                },
                systemInstruction = new
                {
                    parts = new[] { new { text = StaticSystemPrompt } }
                },
                generationConfig = new { }
            };

            var json = JsonSerializer.Serialize(requestBody);

            var models = new[]
            {
                "gemini-flash-lite-latest",
                "gemini-flash-latest"
            };

            foreach (var model in models)
            {
                try
                {
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={_apiKey}";
                    var response = await _httpClient.PostAsync(url, content);

                    if (!response.IsSuccessStatusCode)
                    {
                        var error = await response.Content.ReadAsStringAsync();
                        Console.WriteLine($"Gemini warning on model '{model}' ({response.StatusCode}): {error}. Trying next fallback...");
                        continue;
                    }

                    var responseJson = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Gemini raw response ({model}): {responseJson}");
                    using var doc = JsonDocument.Parse(responseJson);

                    if (doc.RootElement.TryGetProperty("candidates", out var candidates) &&
                        candidates.GetArrayLength() > 0 &&
                        candidates[0].TryGetProperty("content", out var candidateContent) &&
                        candidateContent.TryGetProperty("parts", out var parts) &&
                        parts.GetArrayLength() > 0)
                    {
                        return parts[0].GetProperty("text").GetString();
                    }

                    Console.WriteLine($"Gemini response from {model} returned no valid text candidates.");
                    return null;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception calling Gemini API on {model}: {ex.Message}");
                }
            }

            Console.WriteLine("All Gemini models exhausted.");
            return null;
        }

        private static string BuildPrompt(AiQueryDto dto)
        {
            var shortTrips = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Half a Day", "Half day", "1 day", "1 Day" };
            var wordCountTarget = shortTrips.Contains(dto.Duration) ? "110–120 words" : "140–160 words";

            var locationPart = dto.RadiusKm == "Anywhere"
                ? "anywhere in the world"
                : $"starting within {dto.RadiusKm}km of coordinates ({dto.Latitude}, {dto.Longitude})";

            var vibeConfigs = new Dictionary<string, (string Label, string Detail)>(StringComparer.OrdinalIgnoreCase)
            {
                { "Culture",   ("culture-focused", "cultural sights and history") },
                { "Food",      ("food-focused", "local food and dining") },
                { "Nature",    ("nature-focused", "outdoor scenery and nature") },
                { "Chill",     ("relaxed", "laid-back pace") },
                { "Adventure", ("adventurous", "off the beaten path") },
                { "Budget",    ("budget-friendly", "low-cost spots") },
                { "Scenic",    ("scenic", "landscapes and views") },
            };

            string vibePart;
            if (string.Equals(dto.Vibe, "Any", StringComparison.OrdinalIgnoreCase) || !vibeConfigs.TryGetValue(dto.Vibe, out var vibeConf))
            {
                vibePart = "I'm looking for a voyage of any vibe";
            }
            else
            {
                var vibeArticle = "aeiou".IndexOf(char.ToLower(vibeConf.Label[0])) >= 0 ? "an" : "a";
                var detailStr = !string.IsNullOrEmpty(vibeConf.Detail) ? $" ({vibeConf.Detail})" : "";
                vibePart = $"I'm looking for {vibeArticle} {vibeConf.Label} experience{detailStr}";
            }

            var isOnFoot = string.Equals(dto.VehicleType, "Walk", StringComparison.OrdinalIgnoreCase) ||
                           string.Equals(dto.VehicleType, "Run", StringComparison.OrdinalIgnoreCase);

            var isTransit = string.Equals(dto.VehicleType, "Train", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(dto.VehicleType, "Airplane", StringComparison.OrdinalIgnoreCase);

            var displayVehicle = string.Equals(dto.VehicleType, "TinyHouse", StringComparison.OrdinalIgnoreCase)
                ? "tiny house"
                : dto.VehicleType.ToLower();

            var displayDuration = string.Equals(dto.Duration, "Half day", StringComparison.OrdinalIgnoreCase)
                ? "half a day"
                : dto.Duration;

            string vehiclePart;
            if (isOnFoot)
            {
                vehiclePart = $"I want to go for a {displayVehicle} for {displayDuration}.";
            }
            else if (isTransit)
            {
                vehiclePart = $"I'm traveling by {displayVehicle} for {displayDuration}.";
            }
            else
            {
                var vehicleArticle = "aeiou".IndexOf(char.ToLower(displayVehicle[0])) >= 0 ? "an" : "a";
                vehiclePart = $"I have {vehicleArticle} {displayVehicle} and {displayDuration} available.";
            }

            var spotDescriptions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "Popular Spots",   "popular spots (iconic landmarks and high-profile highlights)" },
                { "Local Favorites", "local favorites (authentic neighborhood staples favored by locals)" },
                { "Hidden Gems",     "hidden gems (lesser-known, off-the-beaten-path secret spots)" },
                { "Mixed Picks",     "mixed picks (a curated mix of popular spots, local favorites, and hidden gems)" },
            };

            var spotPart = !string.IsNullOrWhiteSpace(dto.SpotType) && spotDescriptions.TryGetValue(dto.SpotType, out var spotDesc)
                ? $", focusing on {spotDesc}"
                : "";

            return $"{vehiclePart} {vibePart}{spotPart}, {locationPart}. Coordinates: ({dto.Latitude}, {dto.Longitude}). Target length: {wordCountTarget}. What voyage would you suggest?";
        }
    }
}
