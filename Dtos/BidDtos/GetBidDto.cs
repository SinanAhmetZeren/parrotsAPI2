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
        public string UserId { get; set; }
    }
}
