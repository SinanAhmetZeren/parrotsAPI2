using System.Text;
using System.Text.Json;
using ParrotsAPI2.Dtos.AiDtos;

namespace ParrotsAPI2.Services.Ai
{
    public class AiService : IAiService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        private const string SystemPromptTemplate =
            "You are a knowledgeable travel companion for the Parrots Voyages app. Write a natural, " +
            "dense travel narrative in a single cohesive paragraph.\n\n" +
            "Begin your response with ONLY the location derived from these coordinates: {coordinates}, " +
            "formatted strictly inside double square brackets as [[City, District/Borough]] (or [[City, State]] for US/Canada locations)," +
            " for example [[Istanbul, Kadıköy]] or [[Lawrence, Kansas]].\n\n" +
            "Navigation & Rules:\n" +
            "    Immediately follow the bracketed location with a physically feasible route tailored to {vehicle_type}.\n" +
            "    Street & Route Framing: Do not write turn-by-turn GPS instructions or directional commands like 'turn right onto X' or 'turn left on Y'. Do not list every side street. Mention only 1-2 main avenues or districts for orientation, focusing the narrative around key spots and landmarks.\n" +
            "    Provide a reasonable number of specific, sequential local spots, landmarks, or street names appropriate for" +
            "    {vehicle_type} and the voyage duration ({duration}). For multi-week trips, focus on key neighborhoods, " +
            "    towns, or major route anchors, but still include specific local spots wherever appropriate.\n" +
            "    No Fluff or Marketing: Strictly forbid travel-blogger filler, emotional adjectives, and subjective venue descriptions " +
            "    (e.g., do not write \"Stroll through narrow lanes to taste incredible dishes\" or \"Soak in breathtaking views\"). " +
            "    State directions and locations factually (e.g., \"Head south along Güneşli Bahçe Sokak past Çiya Sofrası\").\n" +
            "    Wrap every specific place name, landmark, or street name in double asterisks, e.g., **Kadıköy Market**.\n" +
            "    Write in plain text without headers, bullet points, or lists.\n" +
            "    Never mention prices, cash, cards, or payment methods.";

        public AiService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        public async Task<string?> AskAsync(AiQueryDto dto)
        {
            var apiKey = _configuration["Google_Gemini_Parrots_AI_Query_Key"];
            var prompt = BuildPrompt(dto);

            var requestBody = new
            {
                contents = new[]
                {
                    new { parts = new[] { new { text = prompt } } }
                },
                systemInstruction = new
                {
                    parts = new[] { new { text = BuildSystemPrompt(dto) } }
                }
            };

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("X-goog-api-key", apiKey);
            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var url = "https://generativelanguage.googleapis.com/v1beta/models/gemini-flash-latest:generateContent";

            var response = await client.PostAsync(url, content);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Gemini error {response.StatusCode}: {error}");
                return null;
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseJson);
            return doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();
        }

        private static string BuildSystemPrompt(AiQueryDto dto) =>
            SystemPromptTemplate
                .Replace("{coordinates}", $"{dto.Latitude}, {dto.Longitude}")
                .Replace("{vehicle_type}", dto.VehicleType)
                .Replace("{duration}", dto.Duration);

        private static string BuildPrompt(AiQueryDto dto)
        {
            var locationPart = dto.RadiusKm == "Anywhere"
                ? "anywhere in the world"
                : $"starting within {dto.RadiusKm}km of coordinates ({dto.Latitude}, {dto.Longitude})";

            var vibeDescriptions = new Dictionary<string, string>
            {
                { "Culture", "Culture (focused on cultural sights and history)" },
                { "Food", "Food (focused on local food and dining)" },
                { "Nature", "Nature (focused on nature and outdoor scenery)" },
                { "Chill", "Chill (relaxed and laid-back)" },
                { "Adventure", "Adventure (adventurous and off the beaten path)" },
                { "Budget", "Budget (budget-friendly)" },
                { "Scenic", "Scenic (focused on scenic landscapes and views)" },
            };
            var vibePart = dto.Vibe == "Any"
                ? "I'm open to any vibe"
                : $"I'm looking for a {vibeDescriptions[dto.Vibe]} experience";

            var isOnFoot = dto.VehicleType == "Walk" || dto.VehicleType == "Run";
            var vehiclePart = isOnFoot
                ? $"I want to go for a {dto.VehicleType} for {dto.Duration}."
                : $"I have a {dto.VehicleType} and {dto.Duration} available.";

            return $"{vehiclePart} {vibePart}, {locationPart}. What voyage would you suggest?";
        }
    }
}
