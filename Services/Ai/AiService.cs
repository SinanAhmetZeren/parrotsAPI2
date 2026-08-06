using System.Text;
using System.Text.Json;
using ParrotsAPI2.Dtos.AiDtos;

namespace ParrotsAPI2.Services.Ai
{
    public class AiService : IAiService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        private const string SystemPrompt =
            "You are a knowledgeable travel companion for the Parrots Voyages app. " +
            "Respond in 400-600 characters total. End each sentence with a newline. " +
            "Mention specific place names, one or two practical tips, and keep a conversational tone. " +
            "No bullet points, no headers, no fluff. Think experienced traveller talking to a friend. " +
            "Before suggesting a route, verify it is physically feasible for the given vehicle type — " +
            "for example, do not suggest a boat route on water that is not navigable, or a walking route that is impossibly long. " +
            "Wrap every place name (streets, landmarks, towns, lakes, bridges etc.) in double asterisks like **King's College**.";

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
                    parts = new[] { new { text = SystemPrompt } }
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

            return $"I have a {dto.VehicleType} and {dto.Duration} available. " +
                   $"{vibePart}, {locationPart}. " +
                   $"What voyage would you suggest?";
        }
    }
}
