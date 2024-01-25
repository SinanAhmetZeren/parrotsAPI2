namespace ParrotsAPI2.Dtos.BidDtos
{
    public class BidDto
    {
        public int PersonCount { get; set; }
        public string Message { get; set; }
        public decimal OfferPrice { get; set; }
        public string Currency { get; set; }
        public DateTime DateTime { get; set; }
        public int VoyageId { get; set; }
        public int UserId { get; set; }
    }
}
