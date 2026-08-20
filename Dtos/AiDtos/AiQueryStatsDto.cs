namespace ParrotsAPI2.Dtos.AiDtos
{
    public class AiDailyDurationBreakdown
    {
        public string Duration { get; set; } = string.Empty;
        public double AvgPlacesSuggested { get; set; }
    }

    public class AiQueryDayDto
    {
        public string Date { get; set; } = string.Empty;
        public int QueryCount { get; set; }
        public double AvgDurationMs { get; set; }
        public double AvgInputTokens { get; set; }
        public double AvgOutputTokens { get; set; }
        public double AvgTotalTokens { get; set; }
        public List<AiDailyDurationBreakdown> DurationBreakdown { get; set; } = new();
    }
}
