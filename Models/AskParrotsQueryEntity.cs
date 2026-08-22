namespace ParrotsAPI2.Models;

public class AskParrotsQueryEntity
{
    public int Id { get; set; }

    // Request Inputs
    public string UserId { get; set; } = string.Empty;
    public string UserQuery { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string VehicleType { get; set; } = string.Empty;
    public string Duration { get; set; } = string.Empty;
    public string Vibe { get; set; } = string.Empty;
    public string SpotType { get; set; } = string.Empty;

    // Execution Metadata
    public string ModelRequested { get; set; } = string.Empty;
    public string ModelUsed { get; set; } = string.Empty;
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public int DurationMs { get; set; }

    // Token Usage
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public int? CachedInputTokens { get; set; }

    // AI Outputs
    public string? RawAiResponseJson { get; set; }
    public string? DraftNarrative { get; set; }
    public int PlannedSpotCount { get; set; }
    public string? PlannedSpotsJson { get; set; }

    // Places API Audit Trail
    public string? PlacesApiAuditJson { get; set; }

    // Final Output
    public string? FinalSanitizedNarrative { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
