namespace ParrotsAPI2.Models
{
    public class TermsVersion
    {
        public int Id { get; set; }
        public string Version { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime PublishedAt { get; set; }
        public bool IsCurrent { get; set; }
    }
}
