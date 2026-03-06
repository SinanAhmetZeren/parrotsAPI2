// DTO for request
public class UserSendCoinsDto
{
    public string UserId { get; set; } = default!;
    public string? ReceiverId { get; set; }
    public int Coins { get; set; }


}