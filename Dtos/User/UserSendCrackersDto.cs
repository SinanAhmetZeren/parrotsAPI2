public class UserSendCrackersDto
{
    public string UserId { get; set; } = default!;
    public string? ReceiverId { get; set; }
    public int Crackers { get; set; }
}
