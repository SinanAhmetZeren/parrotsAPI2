namespace ParrotsAPI2.Dtos.BidDtos
{
    public class VoyageBidDto
    {
        public int Id { get; set; }
        public bool Accepted { get; set; }

        public int PersonCount { get; set; }
        public string Message { get; set; }
        public decimal OfferPrice { get; set; }
        public string Currency { get; set; }
        public DateTime DateTime { get; set; }
        public int VoyageId { get; set; }
        public string UserId { get; set; }
        public string? UserProfileImage { get; set; } = null;
        public string? UserName { get; set; } = null;

    }
}
