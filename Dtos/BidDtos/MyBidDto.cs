namespace ParrotsAPI2.Dtos.BidDtos
{
    public class MyBidDto
    {
        public int BidId { get; set; }
        public bool Accepted { get; set; }
        public decimal OfferPrice { get; set; }
        public DateTime BidDateTime { get; set; }
        public int VoyageId { get; set; }
        public string VoyageName { get; set; } = string.Empty;
        public string ProfileImage { get; set; } = string.Empty;
        public string ProfileImageThumbnail { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}
