namespace ParrotsAPI2.Dtos.AiDtos
{
    public class AiQueryRecordDto
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string UserQuery { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string VehicleType { get; set; } = string.Empty;
        public string Duration { get; set; } = string.Empty;
        public string Vibe { get; set; } = string.Empty;
        public string SpotType { get; set; } = string.Empty;
        public string ModelRequested { get; set; } = string.Empty;
        public string ModelUsed { get; set; } = string.Empty;
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
        public int DurationMs { get; set; }
        public int InputTokens { get; set; }
        public int OutputTokens { get; set; }
        public int? CachedInputTokens { get; set; }
        public int PlannedSpotCount { get; set; }
        public int PlacesVerified { get; set; }
        public string? PlannedSpotsJson { get; set; }
        public string? PlacesApiAuditJson { get; set; }
        public string? DraftNarrative { get; set; }
        public string? FinalSanitizedNarrative { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class AiQueryPageDto
    {
        public List<AiQueryRecordDto> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }
}
