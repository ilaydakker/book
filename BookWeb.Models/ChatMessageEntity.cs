using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookWeb.Models
{
    public class ChatMessageEntity
    {
        [Key]
        public int Id { get; set; }

        public int ConversationId { get; set; }

        [ForeignKey("ConversationId")]
        public ChatConversation Conversation { get; set; } = null!;

        [Required]
        [StringLength(20)]
        public string Role { get; set; } = string.Empty; // "user" or "assistant"

        [Required]
        public string Content { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
