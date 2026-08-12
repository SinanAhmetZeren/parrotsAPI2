using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Caching.Memory;
using ParrotsAPI2.Dtos.AiDtos;

namespace ParrotsAPI2.Services.Ai
{
    public class AiService : IAiService
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IMemoryCache _cache;
        private readonly string _apiKey;
        private readonly string? _placesApiKey;

        private static string GetSystemPrompt() =>
            "You are a knowledgeable travel companion for the Parrots Voyages app. " +
            "You MUST respond with a valid JSON object containing exactly two fields: \"draft_narrative\" and \"planned_spots\".\n\n" +

            "## draft_narrative\n" +
            "Write a detailed travel narrative.\n" +
            "PARAGRAPH & LENGTH STRUCTURE (MANDATORY):\n" +
            "1. The narrative MUST strictly match the paragraph count and sentence structure specified in the target length (e.g., exactly 3 to 4 paragraphs for short trips, or 5 to 6 paragraphs for long trips) separated by double line breaks (\\n\\n). A single continuous block is INVALID output.\n" +
            "2. Every single paragraph MUST contain long, deeply detailed descriptions of physical streetscapes, architectural facades, paving materials, corner storefronts, and atmospheric transitions between stops to satisfy the target word count.\n" +
            "3. Short, abrupt, or summary paragraphs are strictly prohibited.\n\n" +
            "To reach the target length, describe the physical streets walked, neighborhood character, transitions between stops, and specific recommendations at each location.\n\n" +

            "Begin the narrative with ONLY the location derived from the provided coordinates, " +
            "formatted strictly inside double square brackets as [[City, District/Borough]] (or [[City, State]] for US/Canada locations), " +
            "for example [[Istanbul, Kadıköy]] or [[Lawrence, Kansas]]. " +
            "If the location lacks a clear district or state (e.g. rural area, national park, ocean, remote island), use [[City, Country]] or [[Region, Country]], e.g. [[Santorini, Greece]] or [[Yosemite, California]].\n" +
            "CRITICAL: The double-bracket format [[City, District]] MUST appear ONLY at the absolute beginning of the narrative as the opening header. NEVER use double brackets anywhere else in the body of the text.\n\n" +

            "Navigation & Rules:\n" +
            "    Immediately follow the bracketed location with a physically feasible route tailored to the specified vehicle type.\n" +
            "    Spot Density & Scale:\n" +
            "    - For short-duration trips (Half-day, 1-day): you MUST suggest 4 to 5 closely located sequential stops.\n" +
            "    - For long-duration trips (1 week, 2 weeks, Multi-week): you MUST include 12 to 15 distinct sequential stops distributed evenly across the entire corridor, ensuring the traveler always has multiple active destinations per neighborhood.\n" +
            "    VEHICLE & DURATION SCALING MATRIX:\n" +
            "    - On Foot (Walk, Run, Hike) + Long Duration (1 week, 2 weeks, Multi-week):\n" +
            "      * In Major Metropolises (e.g., NYC, London, Berlin, Tokyo): Keep the voyage entirely within the metropolitan area. Treat it as a multi-stage borough-by-borough traversal (50-150+ km total) progressing through distinct outer districts along a continuous corridor. STRICT: Do NOT loop through central tourist staples (e.g., in Berlin, avoid staying trapped between Mitte, Checkpoint Charlie, and Friedrichshain). You MUST push outward into distant or residential quarters (e.g., Wedding, Reinickendorf, Pankow, Weißensee, Lichtenberg). Target: 550–600 words, 5 to 6 paragraphs, each containing 5 to 6 long and highly detailed sentences. MANDATORY: If your draft is under 550 words, expand the atmospheric and architectural descriptions of the intermediate neighborhoods until you exceed 550 words.\n" +
            "      * In Small Towns & Rural Hubs (e.g., Moab, St. Andrews): Treat as a long-distance regional trek or pilgrimage (100-300+ km) that exits the town and progresses along regional trails, countryside, and neighboring villages. Target: 550–600 words, 5 to 6 paragraphs, each containing 5 to 6 long and highly detailed sentences. MANDATORY: If your draft is under 550 words, expand the atmospheric and architectural descriptions of the intermediate neighborhoods until you exceed 550 words.\n" +
            "    - On Foot (Walk, Run, Hike) + Short Duration (Half day, 1 day): Treat as an exploration within a 2-10 km walking radius depending on duration (shorter 2-4 km radius for half-day, longer 5-10 km radius for 1 full day). Target: 350–380 words, 3 to 4 paragraphs, each containing 4 to 5 detailed sentences. MANDATORY: If your draft is under 350 words, expand the atmospheric and architectural descriptions of the intermediate neighborhoods until you exceed 350 words.\n" +
            "    - Motorized & Cycling (Car, Motorcycle, Bicycle, Train) + Long Duration (1 week, 2 weeks): Treat as a multi-city road trip or regional tour covering major transportation corridors, distinct towns, and key regional stops. Target: 550–600 words, 5 to 6 paragraphs, each containing 5 to 6 long and highly detailed sentences. MANDATORY: If your draft is under 550 words, expand the atmospheric and architectural descriptions of the intermediate neighborhoods until you exceed 550 words.\n" +
            "    - Motorized & Cycling (Car, Motorcycle, Bicycle, Train) + Short Duration (Half day, 1 day): Treat as a city-wide or regional day loop (covering 10-50+ km). Connect distinct neighborhoods, landmarks, or nearby towns beyond simple walking distance. Target: 350–380 words, 3 to 4 paragraphs, each containing 4 to 5 detailed sentences. MANDATORY: If your draft is under 350 words, expand the atmospheric and architectural descriptions of the intermediate neighborhoods until you exceed 350 words.\n" +
            "    - Linear Progression Rule (No Backtracking): All voyages MUST follow a continuous, linear spatial progression without geographic ping-ponging or backtracking. Select a single continuous corridor—whether progressing sequentially through adjacent city districts/boroughs or heading outward into surrounding regions—and align all stops in a logical, one-way directional sequence.\n" +
            "    - STRICT ENDING RULE: NEVER write a concluding summary sentence, meta-commentary, or wrap-up reflection. The narrative MUST end abruptly and factually on the final physical stop and its associated food or drink item.\n\n" +
            "    Street & Route Framing: Do not write turn-by-turn GPS instructions or directional commands like 'turn right onto X' or 'turn left on Y'. Do not list every side street. Mention only 1-2 main avenues or districts for orientation, focusing the narrative around key spots and landmarks.\n" +
            "    Provide a reasonable number of specific, sequential local spots, landmarks, or street names appropriate for " +
            "    the vehicle type and voyage duration. For multi-week trips, focus on key neighborhoods, " +
            "    towns, or major route anchors, but still include specific local spots wherever appropriate.\n" +
            "    No Fluff or Marketing: Strictly forbid travel-blogger filler, emotional adjectives, and subjective venue descriptions " +
            "    (e.g., do not write \"Stroll through narrow lanes to taste incredible dishes\" or \"Soak in breathtaking views\"). " +
            "    State directions and locations factually (e.g., \"Head south along Güneşli Bahçe Sokak to **Çiya Sofrası**\").\n" +
            "    Wrap in **...** ONLY proper destinations a traveler would physically enter to spend time or consume something — such as restaurants, cafés, museums, parks, or markets. NEVER wrap infrastructure, transit stations, intersections, or bridges (e.g., do not wrap Behmstraßenbrücke). Do not wrap street or avenue names when used purely for orientation.\n" +

            "    CRITICAL ARTICLE & ADJECTIVE RULE FOR BOLD SPOTS:\n" +
            "    1. NEVER place articles ('a', 'an', 'the') or descriptive pre-modifiers ('famous', 'historic', 'popular', 'iconic') immediately before a **Spot Name**.\n" +
            "    2. Always write bold spots so the proper noun stands alone naturally. Write 'stop at **Çiya Sofrası** for...', NEVER 'stop at the **Çiya Sofrası**' or 'visit the famous **Çiya Sofrası**'.\n" +
            "    3. Bold tags MUST contain strictly the venue name itself (e.g., **Çiya Sofrası**), never generic words or articles.\n\n" +

            "    Wrap every specific food or drink item name in double curly braces, e.g., {{Turkish delight}} or {{dürüm wrap}}. Only wrap the food/drink name itself, not descriptions around it.\n" +
            "    Write in plain text formatted with double line breaks for paragraph splits. Do not use headers, bullet points, or lists.\n" +
            "    Never mention prices, cash, cards, or payment methods.\n" +
            "    Spot Selection & Discovery Style: Strictly respect the discovery style requested in the user prompt. " +
            "    If 'hidden gems' is specified, you MUST strictly avoid famous tourist staples, top-ranked guidebook destinations, highly blogged places, and world-famous venues (e.g. in Kadıköy, avoid Çiya Sofrası or Şekerci Cafer Erol; in London, avoid Borough Market, Dishoom, or Sky Garden; in Cambridge, avoid King's College Chapel or Fitzbillies; in NYC, avoid Katz's Delicatessen, Chelsea Market, or Levain Bakery). Focus strictly on quiet side-street spots, neighborhood secrets, and non-touristy local places.\n\n" +

            "## planned_spots\n" +
            "An array containing one entry for EVERY spot name wrapped in **...** in draft_narrative. Each entry must have:\n" +
            "    - name: the exact text inside the ** markers (no asterisks, e.g. \"Çiya Sofrası\")\n" +
            "    - lat: your best-estimate latitude (float)\n" +
            "    - lng: your best-estimate longitude (float)\n" +
            "    - region: the city or district the spot is in (e.g. \"Kadıköy\" or \"Brooklyn\")\n" +
            "    - fallback_label: the article ('a' or 'an') followed by a 2-4 word generic description of the spot type " +
            "(e.g. \"a local fish market\" or \"an artisanal café\"). This is used as drop-in replacement text when the spot cannot be verified, so it must read naturally in a sentence.";

        private static readonly string _abcdePath = Path.Combine(Directory.GetCurrentDirectory(), "logs", "ABCDE_local_latest.txt");

        private static void LogABCDE(string message)
        {
            Console.WriteLine(message);
            Directory.CreateDirectory(Path.GetDirectoryName(_abcdePath)!);
            File.AppendAllText(_abcdePath, message + Environment.NewLine);
        }

        public AiService(HttpClient httpClient, IHttpClientFactory httpClientFactory, IMemoryCache cache, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _httpClientFactory = httpClientFactory;
            _cache = cache;
            _apiKey = configuration["Google_Gemini_Parrots_AI_Query_Key"]
                      ?? throw new ArgumentNullException("Gemini API key is missing.");
            _placesApiKey = configuration["Google_Places_API_Key"];
        }

        public async Task<string?> AskAsync(AiQueryDto dto)
        {
            var (wordCountTarget, sentenceStructure) = GetWordCountTarget(dto.Duration);
            var userPrompt = BuildPrompt(dto, wordCountTarget);
            var systemPrompt = GetSystemPrompt();
            File.WriteAllText(_abcdePath, string.Empty);
            LogABCDE($"[AskParrots] A. User query: {userPrompt}");

            var requestBody = new
            {
                contents = new[]
                {
                    new { parts = new[] { new { text = userPrompt } } }
                },
                systemInstruction = new
                {
                    parts = new[] { new { text = systemPrompt } }
                },
                generationConfig = new
                {
                    responseMimeType = "application/json",
                    temperature = 0.8,
                    topP = 0.95,
                    responseSchema = new
                    {
                        type = "OBJECT",
                        properties = new
                        {
                            draft_narrative = new { type = "STRING", description = $"Must strictly contain {sentenceStructure}." },
                            planned_spots = new
                            {
                                type = "ARRAY",
                                items = new
                                {
                                    type = "OBJECT",
                                    properties = new
                                    {
                                        name = new { type = "STRING" },
                                        lat = new { type = "NUMBER" },
                                        lng = new { type = "NUMBER" },
                                        region = new { type = "STRING" },
                                        fallback_label = new { type = "STRING" }
                                    },
                                    required = new[] { "name", "lat", "lng", "region", "fallback_label" }
                                }
                            }
                        },
                        required = new[] { "draft_narrative", "planned_spots" }
                    }
                }
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
                    using var doc = JsonDocument.Parse(responseJson);

                    if (doc.RootElement.TryGetProperty("candidates", out var candidates) &&
                        candidates.GetArrayLength() > 0 &&
                        candidates[0].TryGetProperty("content", out var candidateContent) &&
                        candidateContent.TryGetProperty("parts", out var parts) &&
                        parts.GetArrayLength() > 0)
                    {
                        var text = parts[0].GetProperty("text").GetString();
                        if (string.IsNullOrWhiteSpace(text)) break;

                        using var geminiJson = JsonDocument.Parse(text);
                        var root = geminiJson.RootElement;

                        if (!root.TryGetProperty("draft_narrative", out var narrativeEl) ||
                            !root.TryGetProperty("planned_spots", out var spotsEl))
                        {
                            Console.WriteLine($"Gemini response from {model} missing expected JSON fields.");
                            return null;
                        }

                        var narrative = narrativeEl.GetString() ?? string.Empty;
                        var plannedSpots = spotsEl.EnumerateArray()
                            .Select(s => new PlannedSpot(
                                Name: s.GetProperty("name").GetString() ?? "",
                                Lat: s.GetProperty("lat").GetDouble(),
                                Lng: s.GetProperty("lng").GetDouble(),
                                Region: s.GetProperty("region").GetString() ?? "",
                                FallbackLabel: s.GetProperty("fallback_label").GetString() ?? ""
                            ))
                            .ToList();

                        LogABCDE($"[AskParrots] B. Planned spots ({plannedSpots.Count}):");
                        foreach (var s in plannedSpots)
                            LogABCDE($"  - {s.Name} | {s.Region} | fallback: {s.FallbackLabel}");
                        var narrativeWordCount = narrative.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
                        var narrativeCharCount = narrative.Length;
                        LogABCDE($"[AskParrots] C. Draft narrative ({narrativeWordCount} words, {narrativeCharCount} chars): {narrative}");

                        var sanitized = await SanitizeNarrativeAsync(narrative, plannedSpots);
                        var sanitizedWordCount = sanitized.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
                        var sanitizedCharCount = sanitized.Length;
                        LogABCDE($"[AskParrots] E. Final narrative ({sanitizedWordCount} words, {sanitizedCharCount} chars): {sanitized}");
                        return sanitized;
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

        private async Task<string> SanitizeNarrativeAsync(string narrative, List<PlannedSpot> spots)
        {
            if (spots.Count == 0)
                return narrative;

            var verifyTasks = spots.Select(s => VerifySpotAsync(s.Name, s.Region, CancellationToken.None)).ToArray();
            var results = await Task.WhenAll(verifyTasks);

            for (int i = 0; i < spots.Count; i++)
            {
                if (!results[i])
                    narrative = narrative.Replace($"**{spots[i].Name}**", spots[i].FallbackLabel, StringComparison.Ordinal);
            }

            return Regex.Replace(narrative, @"\b(a|an|the)\s+(a|an)\b", "$2", RegexOptions.IgnoreCase);
        }

        private async Task<bool> VerifySpotAsync(string spotName, string region, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(_placesApiKey))
                return true;

            var cacheKey = $"spot_exists_{spotName.Trim().ToLowerInvariant()}_{region.Trim().ToLowerInvariant()}";

            if (_cache.TryGetValue(cacheKey, out bool cachedExists))
                return cachedExists;

            try
            {
                var client = _httpClientFactory.CreateClient("places");

                using var request = new HttpRequestMessage(HttpMethod.Post, $"https://places.googleapis.com/v1/places:searchText?key={_placesApiKey}");
                request.Headers.Add("X-Goog-FieldMask", "places.id");
                request.Content = new StringContent(
                    JsonSerializer.Serialize(new { textQuery = $"{spotName}, {region}" }),
                    Encoding.UTF8, "application/json");

                using var response = await client.SendAsync(request, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var errorJson = await response.Content.ReadAsStringAsync(cancellationToken);
                    LogABCDE($"[AskParrots] D. Places 403 error: {errorJson}");
                    return true;
                }

                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                var spotExists = json.Contains("\"id\"");

                LogABCDE($"[AskParrots] D. Places check: \"{spotName}, {region}\" → {(spotExists ? "VERIFIED" : "NOT FOUND")}");

                _cache.Set(cacheKey, spotExists, TimeSpan.FromHours(24));
                return spotExists;
            }
            catch
            {
                return true;
            }
        }

        private static (string WordCount, string SentenceStructure) GetWordCountTarget(string? duration)
        {
            var shortTrips = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Half a Day", "Half day", "1 day", "1 Day" };
            return shortTrips.Contains(duration ?? string.Empty)
                ? ("350–380 words", "3 to 4 paragraphs, each containing 4 to 5 detailed sentences")
                : ("550–600 words", "5 to 6 paragraphs, each containing 5 to 6 long and highly detailed sentences");
        }

        private static string BuildPrompt(AiQueryDto dto, string wordCountTarget)
        {

            var locationPart = $"starting within {dto.RadiusKm}km of coordinates ({dto.Latitude}, {dto.Longitude})";

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

            return $"{vehiclePart} {vibePart}{spotPart}, {locationPart}. Coordinates: ({dto.Latitude}, {dto.Longitude}). Target length: {wordCountTarget}. What voyage would you suggest? [ref:{Guid.NewGuid():N}]";
        }
    }

    internal record PlannedSpot(string Name, double Lat, double Lng, string Region, string FallbackLabel);
}
