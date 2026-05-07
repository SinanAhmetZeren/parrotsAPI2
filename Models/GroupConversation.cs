using System.ComponentModel.DataAnnotations;

namespace ParrotsAPI2.Models
{
    public class GroupConversation
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string CreatorId { get; set; } = string.Empty;

        [Required]
        [MaxLength(73)]
        public string EncryptionKey { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastMessageDate { get; set; }

        public ICollection<GroupMember> Members { get; set; } = new List<GroupMember>();
        public ICollection<GroupMessage> Messages { get; set; } = new List<GroupMessage>();
    }
}
