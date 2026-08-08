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
            "    Never mention prices, cash, cards, or payment methods.";

        public AiService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _apiKey = configuration["Google_Gemini_Parrots_AI_Query_Key"]
                      ?? throw new ArgumentNullException("Gemini API key is missing.");
        }

        public async Task<string?> AskAsync(AiQueryDto dto)
        {
            var userPrompt = BuildPrompt(dto);

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

            var vibeDescriptions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "Culture", "Culture (focused on cultural sights and history)" },
                { "Food", "Food (focused on local food and dining)" },
                { "Nature", "Nature (focused on nature and outdoor scenery)" },
                { "Chill", "Chill (relaxed and laid-back)" },
                { "Adventure", "Adventure (adventurous and off the beaten path)" },
                { "Budget", "Budget (budget-friendly)" },
                { "Scenic", "Scenic (focused on scenic landscapes and views)" },
            };

            var vibePart = dto.Vibe == "Any" || !vibeDescriptions.ContainsKey(dto.Vibe)
                ? "I'm open to any vibe"
                : $"I'm looking for a {vibeDescriptions[dto.Vibe]} experience";

            var isOnFoot = string.Equals(dto.VehicleType, "Walk", StringComparison.OrdinalIgnoreCase) ||
                           string.Equals(dto.VehicleType, "Run", StringComparison.OrdinalIgnoreCase);

            var vehiclePart = isOnFoot
                ? $"I want to go for a {dto.VehicleType} for {dto.Duration}."
                : $"I have a {dto.VehicleType} and {dto.Duration} available.";

            return $"{vehiclePart} {vibePart}, {locationPart}. Coordinates: ({dto.Latitude}, {dto.Longitude}). Target length: {wordCountTarget}. What voyage would you suggest?";
        }
    }
}
