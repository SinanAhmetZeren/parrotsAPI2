namespace ParrotsAPI2.Dtos.FavoriteDtos
{
    public class AddFavoriteDto
    {
        public string UserId { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public int ItemId { get; set; }
    }
}
