using System.ComponentModel.DataAnnotations;

public class Conversation
{
    public int Id { get; set; }

    [Required]
    public string User1Id { get; set; } = string.Empty;

    [Required]
    public string User2Id { get; set; } = string.Empty;

    [Required]
    [MaxLength(73)]
    public string ConversationKey { get; set; } = string.Empty;
    public int? LastMessageId { get; set; }
    public DateTime? LastMessageDate { get; set; }
}

