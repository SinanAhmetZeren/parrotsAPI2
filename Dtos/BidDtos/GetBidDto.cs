namespace ParrotsAPI2.Dtos.BidDtos
{
    public class GetBidDto
    {
        public int Id { get; set; }
        public bool Accepted { get; set; }
        public int PersonCount { get; set; }
        public string Message { get; set; } = string.Empty;
        public decimal OfferPrice { get; set; }
        // public string Currency { get; set; } = string.Empty;
        public DateTime DateTime { get; set; }
        public int VoyageId { get; set; }
        public string VoyageName { get; set; } = string.Empty;
        public string VoyageImageUrl { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string UserImageUrl { get; set; } = string.Empty;
    }
}
