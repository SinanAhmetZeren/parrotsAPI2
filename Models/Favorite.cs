namespace ParrotsAPI2.Models
{
    public class Favorite
    {

        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public int ItemId { get; set; }
        
    }
}
