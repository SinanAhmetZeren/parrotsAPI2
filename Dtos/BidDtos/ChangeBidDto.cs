namespace ParrotsAPI2.Dtos.BidDtos
{
    public class ChangeBidDto
    {   
        public int Id { get; set; }
        public bool Accepted { get; set; }
        public int PersonCount { get; set; }
        public string Message { get; set; } = string.Empty;
        public decimal OfferPrice { get; set; }
        public DateTime DateTime { get; set; }

    }
}
