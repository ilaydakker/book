using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookWeb.Models
{
    public class ChatConversation
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string ApplicationUserId { get; set; } = string.Empty;

        [ForeignKey("ApplicationUserId")]
        public ApplicationUser ApplicationUser { get; set; } = null!;

        [StringLength(200)]
        public string Title { get; set; } = "New Chat";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<ChatMessageEntity> Messages { get; set; } = new List<ChatMessageEntity>();
    }
}
