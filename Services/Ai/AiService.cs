using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Caching.Memory;
using ParrotsAPI2.Data;
using ParrotsAPI2.Dtos.AiDtos;
using ParrotsAPI2.Models;

namespace ParrotsAPI2.Services.Ai
{
    public class AiService : IAiService
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IMemoryCache _cache;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly string _apiKey;
        private readonly string? _placesApiKey;


        private static string GetSystemPrompt() =>
            "You are a knowledgeable travel companion for the Parrots Voyages app. " +
            "You MUST respond with a valid JSON object containing exactly two fields: \"draft_narrative\" and \"planned_spots\".\n\n" +

            "## draft_narrative\n" +
            "Write a detailed travel narrative.\n" +
            "PARAGRAPH & LENGTH STRUCTURE (MANDATORY):\n" +
            "1. The narrative MUST strictly match the paragraph count and sentence structure specified in the target length (e.g., exactly 3 to 4 paragraphs for short trips, 4 to 5 paragraphs for medium trips, or 5 to 6 paragraphs for long trips) separated by double line breaks (\\n\\n). A single continuous block is INVALID output.\n" +
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
            "    - For short-duration trips (Half-day, 1-day): you MUST suggest exactly 4 to 5 closely located sequential stops.\n" +
            "    - For medium-duration trips (2-3 days): you MUST suggest exactly 6 to 8 distinct sequential stops balanced across adjacent districts without clustering them into a single congested block.\n" +
            "    - For long-duration trips (1 week, 2 weeks, Multi-week): you MUST include exactly 12 to 15 distinct sequential stops distributed evenly across the entire corridor, ensuring the traveler always has multiple active destinations per neighborhood.\n" +
            "    UNIVERSAL QUANTITY OVERRIDE RULE: Regardless of trip duration, prioritize the requested discovery style (popular spots, hidden gems, local favorites, mixed picks), but if qualifying options run scarce, you are required to fill the remaining slots with alternative local spots along the sequential route to strictly hit the mandatory waypoint count (4 to 5 stops for short trips, 6 to 8 stops for medium trips, or 12 to 15 stops for long trips).\n" +
            "    VEHICLE & DURATION SCALING MATRIX:\n" +
            "    - On Foot (Walk, Run, Hike) + Long Duration (1 week, 2 weeks, Multi-week):\n" +
            "      * In Major Metropolises (e.g., NYC, London, Berlin, Tokyo): Keep the voyage entirely within the metropolitan area. Treat it as a multi-stage borough-by-borough traversal (50-150+ km total) progressing through distinct outer districts along a continuous corridor. STRICT: Do NOT loop through central tourist staples (e.g., in Berlin, avoid staying trapped between Mitte, Checkpoint Charlie, and Friedrichshain). You MUST push outward into distant or residential quarters (e.g., Wedding, Reinickendorf, Pankow, Weißensee, Lichtenberg). MANDATORY: Exactly 12 to 15 distinct sequential stops. Prioritize the requested discovery style (popular spots, hidden gems, local favorites, mixed picks), but if qualifying options run scarce, seamlessly fill remaining slots with alternative local spots along the sequential route to strictly hit the 12-15 count. Target: 550–600 words, 5 to 6 paragraphs, each containing 5 to 6 long and highly detailed sentences. MANDATORY: If your draft is under 550 words, expand the atmospheric and architectural descriptions of the intermediate neighborhoods until you exceed 550 words.\n" +
            "      * In Small Towns & Rural Hubs (e.g., Moab, St. Andrews): Treat as a long-distance regional trek or pilgrimage (100-300+ km) that exits the town and progresses along regional trails, countryside, and neighboring villages. MANDATORY: Exactly 12 to 15 distinct sequential stops. Prioritize the requested discovery style (popular spots, hidden gems, local favorites, mixed picks), but if qualifying options run scarce, seamlessly fill remaining slots with alternative local spots along the sequential route to strictly hit the 12-15 count. Target: 550–600 words, 5 to 6 paragraphs, each containing 5 to 6 long and highly detailed sentences. MANDATORY: If your draft is under 550 words, expand the atmospheric and architectural descriptions of the intermediate neighborhoods until you exceed 550 words.\n" +
            "    - On Foot (Walk, Run, Hike) + Medium Duration (2-3 days): Treat as an extended neighborhood exploration covering 10-25 km across adjacent districts, ensuring a balanced pacing rather than a congested single-area cluster. MANDATORY: Exactly 6 to 8 distinct sequential stops. Prioritize the requested discovery style (popular spots, hidden gems, local favorites, mixed picks), but if qualifying options run scarce, fill remaining slots with alternative local spots along the sequential route to strictly hit the 6-8 count. Target: 450–500 words, 4 to 5 paragraphs, each containing 4 to 5 long and detailed sentences. MANDATORY: If your draft is under 450 words, expand the atmospheric and architectural descriptions of the intermediate neighborhoods until you exceed 450 words.\n" +
            "    - On Foot (Walk, Run, Hike) + Short Duration (Half day, 1 day): Treat as an exploration within a 2-10 km walking radius depending on duration (shorter 2-4 km radius for half-day, longer 5-10 km radius for 1 full day). MANDATORY: Exactly 4 to 5 distinct sequential stops. Prioritize the requested discovery style (popular spots, hidden gems, local favorites, mixed picks), but if qualifying options run scarce, fill remaining slots with alternative local spots along the sequential route to strictly hit the 4-5 count. Target: 350–380 words, 3 to 4 paragraphs, each containing 4 to 5 detailed sentences. MANDATORY: If your draft is under 350 words, expand the atmospheric and architectural descriptions of the intermediate neighborhoods until you exceed 350 words.\n" +
            "    - Motorized & Cycling (Car, Motorcycle, Bicycle, Train) + Long Duration (1 week, 2 weeks): Treat as a multi-city road trip or regional tour covering major transportation corridors, distinct towns, and key regional stops. MANDATORY: Exactly 12 to 15 distinct sequential stops. Prioritize the requested discovery style (popular spots, hidden gems, local favorites, mixed picks), but if qualifying options run scarce, seamlessly fill remaining slots with alternative local spots along the sequential route to strictly hit the 12-15 count. Target: 550–600 words, 5 to 6 paragraphs, each containing 5 to 6 long and highly detailed sentences. MANDATORY: If your draft is under 550 words, expand the atmospheric and architectural descriptions of the intermediate neighborhoods until you exceed 550 words.\n" +
            "    - Motorized & Cycling (Car, Motorcycle, Bicycle, Train) + Medium Duration (2-3 days): Treat as a regional weekend tour or multi-town corridor (covering 30-100+ km) with well-spaced progression across adjacent towns or zones. MANDATORY: Exactly 6 to 8 distinct sequential stops. Prioritize the requested discovery style (popular spots, hidden gems, local favorites, mixed picks), but if qualifying options run scarce, fill remaining slots with alternative local spots along the sequential route to strictly hit the 6-8 count. Target: 450–500 words, 4 to 5 paragraphs, each containing 4 to 5 long and detailed sentences. MANDATORY: If your draft is under 450 words, expand the atmospheric and architectural descriptions of the intermediate neighborhoods until you exceed 450 words.\n" +
            "    - Motorized & Cycling (Car, Motorcycle, Bicycle, Train) + Short Duration (Half day, 1 day): Treat as a city-wide or regional day loop (covering 10-50+ km). Connect distinct neighborhoods, landmarks, or nearby towns beyond simple walking distance. MANDATORY: Exactly 4 to 5 distinct sequential stops. Prioritize the requested discovery style (popular spots, hidden gems, local favorites, mixed picks), but if qualifying options run scarce, fill remaining slots with alternative local spots along the sequential route to strictly hit the 4-5 count. Target: 350–380 words, 3 to 4 paragraphs, each containing 4 to 5 detailed sentences. MANDATORY: If your draft is under 350 words, expand the atmospheric and architectural descriptions of the intermediate neighborhoods until you exceed 350 words.\n" +
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
            "    1. NEVER precede a bold spot name (wrapped in **...**) with articles ('a', 'an', 'the') or descriptive adjectives/pre-modifiers ('famous', 'historic', 'popular', 'iconic', 'legendary', 'traditional').\n" +
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
            // Console.WriteLine(message);
            Directory.CreateDirectory(Path.GetDirectoryName(_abcdePath)!);
            File.AppendAllText(_abcdePath, message + Environment.NewLine);
        }

        public AiService(HttpClient httpClient, IHttpClientFactory httpClientFactory, IMemoryCache cache, IServiceScopeFactory scopeFactory, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _httpClientFactory = httpClientFactory;
            _cache = cache;
            _scopeFactory = scopeFactory;
            _apiKey = configuration["Google_Gemini_Parrots_AI_Query_Key"]
                      ?? throw new ArgumentNullException("Gemini API key is missing.");
            _placesApiKey = configuration["Google_Places_API_Key"];
        }

        public async Task<string?> AskAsync(AiQueryDto dto, string userId)
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
                    temperature = 0.7,
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

            var sw = Stopwatch.StartNew();

            foreach (var modelRequested in models)
            {
                try
                {
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    var url = $"https://generativelanguage.googleapis.com/v1beta/models/{modelRequested}:generateContent?key={_apiKey}";
                    var response = await _httpClient.PostAsync(url, content);

                    if (!response.IsSuccessStatusCode)
                    {
                        var error = await response.Content.ReadAsStringAsync();
                        // Console.WriteLine($"Gemini warning on model '{modelRequested}' ({response.StatusCode}): {error}. Trying next fallback...");
                        continue;
                    }

                    var responseJson = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(responseJson);

                    int inputTokens = 0, outputTokens = 0;
                    int? cachedInputTokens = null;
                    string modelUsed = modelRequested;
                    if (doc.RootElement.TryGetProperty("modelVersion", out var mv)) modelUsed = mv.GetString() ?? modelRequested;
                    if (doc.RootElement.TryGetProperty("usageMetadata", out var usage))
                    {
                        if (usage.TryGetProperty("promptTokenCount", out var pt)) inputTokens = pt.GetInt32();
                        if (usage.TryGetProperty("candidatesTokenCount", out var ct)) outputTokens = ct.GetInt32();
                        if (usage.TryGetProperty("cachedContentTokenCount", out var cc)) cachedInputTokens = cc.GetInt32();
                    }

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
                            // Console.WriteLine($"Gemini response from {modelRequested} missing expected JSON fields.");
                            await SaveQueryAsync(dto, userId, userPrompt, modelRequested, modelUsed, responseJson, null, null, null, null, inputTokens, outputTokens, cachedInputTokens, (int)sw.ElapsedMilliseconds, false, "Missing JSON fields");
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

                        var (sanitized, auditJson) = await SanitizeNarrativeAsync(narrative, plannedSpots, dto.VehicleType ?? "");
                        var sanitizedWordCount = sanitized.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
                        var sanitizedCharCount = sanitized.Length;
                        LogABCDE($"[AskParrots] E. Final narrative ({sanitizedWordCount} words, {sanitizedCharCount} chars): {sanitized}");

                        var plannedSpotsJson = JsonSerializer.Serialize(plannedSpots.Select(s => new { s.Name, s.Lat, s.Lng, s.Region, s.FallbackLabel }));
                        await SaveQueryAsync(dto, userId, userPrompt, modelRequested, modelUsed, responseJson, narrative, plannedSpotsJson, auditJson, sanitized, inputTokens, outputTokens, cachedInputTokens, (int)sw.ElapsedMilliseconds, true, null);

                        return sanitized;
                    }

                    // Console.WriteLine($"Gemini response from {modelRequested} returned no valid text candidates.");
                    await SaveQueryAsync(dto, userId, userPrompt, modelRequested, modelUsed, responseJson, null, null, null, null, inputTokens, outputTokens, cachedInputTokens, (int)sw.ElapsedMilliseconds, false, "No valid candidates");
                    return null;
                }
                catch (Exception)
                {
                    // Console.WriteLine($"Exception calling Gemini API on {modelRequested}: {ex.Message}");
                }
            }

            // Console.WriteLine("All Gemini models exhausted.");
            return null;
        }

        private async Task SaveQueryAsync(AiQueryDto dto, string userId, string userPrompt, string modelRequested, string modelUsed, string? rawResponseJson,
            string? draftNarrative, string? plannedSpotsJson, string? placesAuditJson, string? finalNarrative,
            int inputTokens, int outputTokens, int? cachedInputTokens, int durationMs, bool isSuccess, string? errorMessage)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<DataContext>();

                var plannedSpotCount = 0;
                if (!string.IsNullOrEmpty(plannedSpotsJson))
                {
                    using var doc = JsonDocument.Parse(plannedSpotsJson);
                    plannedSpotCount = doc.RootElement.GetArrayLength();
                }

                db.AiQueries.Add(new AiQueryEntity
                {
                    UserId = userId,
                    UserQuery = userPrompt,
                    Latitude = dto.Latitude,
                    Longitude = dto.Longitude,
                    VehicleType = dto.VehicleType ?? string.Empty,
                    Duration = dto.Duration ?? string.Empty,
                    Vibe = dto.Vibe ?? string.Empty,
                    SpotType = dto.SpotType ?? string.Empty,
                    ModelRequested = modelRequested,
                    ModelUsed = modelUsed,
                    IsSuccess = isSuccess,
                    ErrorMessage = errorMessage,
                    DurationMs = durationMs,
                    InputTokens = inputTokens,
                    OutputTokens = outputTokens,
                    CachedInputTokens = cachedInputTokens,
                    RawAiResponseJson = rawResponseJson,
                    DraftNarrative = draftNarrative,
                    PlannedSpotCount = plannedSpotCount,
                    PlannedSpotsJson = plannedSpotsJson,
                    PlacesApiAuditJson = placesAuditJson,
                    FinalSanitizedNarrative = finalNarrative,
                    CreatedAt = DateTime.UtcNow
                });

                await db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AskParrots] Failed to save AiQuery: {ex.Message}");
            }
        }

        private async Task<(string Narrative, string AuditJson)> SanitizeNarrativeAsync(string narrative, List<PlannedSpot> spots, string vehicleType)
        {
            if (spots.Count == 0)
                return (narrative, "[]");

            var verifyTasks = spots.Select(s => VerifySpotAsync(s, vehicleType, CancellationToken.None)).ToArray();
            var results = await Task.WhenAll(verifyTasks);

            for (int i = 0; i < spots.Count; i++)
            {
                if (!results[i].Exists)
                    narrative = narrative.Replace($"**{spots[i].Name}**", spots[i].FallbackLabel, StringComparison.Ordinal);
            }

            narrative = Regex.Replace(narrative, @"\b(a|an|the)\s+(a|an)\b", "$2", RegexOptions.IgnoreCase);

            var audit = spots.Select((s, i) => new { spotName = s.Name, placeId = results[i].PlaceId });
            var auditJson = JsonSerializer.Serialize(audit);

            return (narrative, auditJson);
        }

        private async Task<(bool Exists, string? PlaceId)> VerifySpotAsync(PlannedSpot spot, string vehicleType, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(_placesApiKey))
                return (true, null);

            var cacheKey = $"spot_exists_{spot.Name.Trim().ToLowerInvariant()}_{spot.Region.Trim().ToLowerInvariant()}";

            if (_cache.TryGetValue(cacheKey, out (bool Exists, string? PlaceId) cached))
                return cached;

            try
            {
                var vt = vehicleType.Trim().ToLowerInvariant();
                double radiusMeters = vt is "car" or "motorcycle" or "train" or "bus" or "caravan" or "airplane" or "tinyhouse" or "boat" ? 25000.0 : 3000.0;

                var client = _httpClientFactory.CreateClient("places");

                using var request = new HttpRequestMessage(HttpMethod.Post, "https://places.googleapis.com/v1/places:searchText");
                request.Headers.Add("X-Goog-Api-Key", _placesApiKey);
                request.Headers.Add("X-Goog-FieldMask", "places.id,places.location");
                request.Content = new StringContent(
                    JsonSerializer.Serialize(new
                    {
                        textQuery = $"{spot.Name}, {spot.Region}",
                        locationBias = new
                        {
                            circle = new
                            {
                                center = new { latitude = spot.Lat, longitude = spot.Lng },
                                radius = radiusMeters
                            }
                        }
                    }),
                    Encoding.UTF8, "application/json");

                using var response = await client.SendAsync(request, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var errorJson = await response.Content.ReadAsStringAsync(cancellationToken);
                    LogABCDE($"[AskParrots] D. Places error: {errorJson}");
                    return (false, null);
                }

                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                using var doc = JsonDocument.Parse(json);

                string? placeId = null;
                if (doc.RootElement.TryGetProperty("places", out var places) && places.GetArrayLength() > 0)
                {
                    var first = places[0];
                    placeId = first.TryGetProperty("id", out var idEl) ? idEl.GetString() : null;

                    if (placeId != null && first.TryGetProperty("location", out var loc))
                    {
                        var matchLat = loc.GetProperty("latitude").GetDouble();
                        var matchLng = loc.GetProperty("longitude").GetDouble();
                        var distanceKm = CalculateHaversine(spot.Lat, spot.Lng, matchLat, matchLng);
                        if (distanceKm > radiusMeters / 1000.0 * 1.1)
                        {
                            LogABCDE($"[AskParrots] D. Places false positive rejected: \"{spot.Name}\" distance={distanceKm:F1}km");
                            placeId = null;
                        }
                    }
                }

                var exists = placeId != null;
                LogABCDE($"[AskParrots] D. Places check: \"{spot.Name}, {spot.Region}\" → {(exists ? "VERIFIED" : "NOT FOUND")}");

                var result = (exists, placeId);
                _cache.Set(cacheKey, result, TimeSpan.FromHours(24));
                return result;
            }
            catch
            {
                return (true, null);
            }
        }

        private static double CalculateHaversine(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371.0;
            double dLat = (lat2 - lat1) * (Math.PI / 180.0);
            double dLon = (lon2 - lon1) * (Math.PI / 180.0);
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(lat1 * (Math.PI / 180.0)) * Math.Cos(lat2 * (Math.PI / 180.0)) *
                       Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        }

        private static (string WordCount, string SentenceStructure) GetWordCountTarget(string? duration)
        {
            var shortTrips = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Half a Day", "Half day", "1 day", "1 Day" };
            var mediumTrips = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "2-3 days", "2 to 3 days" };
            var d = duration ?? string.Empty;
            if (shortTrips.Contains(d))
                return ("350–380 words", "3 to 4 paragraphs, each containing 4 to 5 detailed sentences");
            if (mediumTrips.Contains(d))
                return ("450–500 words", "4 to 5 paragraphs, each containing 4 to 5 long and detailed sentences");
            return ("550–600 words", "5 to 6 paragraphs, each containing 5 to 6 long and highly detailed sentences");
        }

        private static string BuildPrompt(AiQueryDto dto, string wordCountTarget)
        {

            var locationPart = $"around coordinates ({dto.Latitude}, {dto.Longitude})";

            var vibeConfigs = new Dictionary<string, (string Label, string Detail)>(StringComparer.OrdinalIgnoreCase)
            {
                { "Culture",   ("culture-focused", "cultural sights, historical depth, and heritage") },
                { "Food",      ("food-focused", "culinary experiences, local dining, and eateries") },
                { "Nature",    ("nature-focused", "outdoor scenery, green spaces, and natural landscapes") },
                { "Chill",     ("relaxed", "laid-back, slow-paced atmosphere") },
                { "Adventure", ("adventurous", "dynamic, off-the-beaten-path exploration") },
                { "Budget",    ("budget-friendly", "low-cost, high-value stops") },
                { "Scenic",    ("scenic", "visual aesthetics and viewpoints") },
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
                { "Popular Spots",   "popular spots (renowned, high-profile highlights relative to the chosen theme)" },
                { "Local Favorites", "local favorites (authentic neighborhood staples favored by locals relative to the chosen theme)" },
                { "Hidden Gems",     "hidden gems (lesser-known, off-the-beaten-path secret spots relative to the chosen theme)" },
                { "Mixed Picks",     "mixed picks (a curated mix of popular spots, local favorites, and hidden gems relative to the chosen theme)" },
            };

            var spotPart = !string.IsNullOrWhiteSpace(dto.SpotType) && spotDescriptions.TryGetValue(dto.SpotType, out var spotDesc)
                ? $", focusing on {spotDesc}"
                : "";

            return $"{vehiclePart} {vibePart}{spotPart}, {locationPart}. Coordinates: ({dto.Latitude}, {dto.Longitude}). Target length: {wordCountTarget}. What voyage would you suggest? [ref:{Guid.NewGuid():N}]";
        }
    }

    internal record PlannedSpot(string Name, double Lat, double Lng, string Region, string FallbackLabel);
}
