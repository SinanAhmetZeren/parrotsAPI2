namespace ParrotsAPI2.Dtos.BidDtos
{
    public class GetBidDto
    {
        public int Id { get; set; }
        public int PersonCount { get; set; }
        public string Message { get; set; }
        public decimal OfferPrice { get; set; }
        public string Currency { get; set; }
        public DateTime DateTime { get; set; }
        public int VoyageId { get; set; }
        public string VoyageName { get; set; } = string.Empty;
        public string VoyageImageUrl { get; set; } = string.Empty;
        public string UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string UserImageUrl { get; set; } = string.Empty;
    }
}
