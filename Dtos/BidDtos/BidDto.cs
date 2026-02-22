namespace ParrotsAPI2.Dtos.BidDtos
{
    public class BidDto
    {
        public bool Accepted { get; set; }
        public int PersonCount { get; set; }
        public string Message { get; set; } = string.Empty;
        public decimal OfferPrice { get; set; }
        // public string Currency { get; set; } = string.Empty;
        public DateTime DateTime { get; set; }
        public int VoyageId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string? UserProfileImage { get; set; } = null;
        public string? UserName { get; set; } = null;

    }
}
