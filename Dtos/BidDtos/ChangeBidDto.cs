namespace ParrotsAPI2.Dtos.BidDtos
{
    public class ChangeBidDto
    {   
        public int Id { get; set; }
        public int PersonCount { get; set; }
        public string Message { get; set; }
        public decimal OfferPrice { get; set; }
        public string Currency { get; set; }
        public DateTime DateTime { get; set; }

    }
}
