using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookWeb.Models
{
    public enum ReadingStatus
    {
        WantToRead = 0,
        CurrentlyReading = 1,
        Read = 2
    }

    public class Wishlist
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string ApplicationUserId { get; set; } = string.Empty;

        [ForeignKey("ApplicationUserId")]
        public ApplicationUser? ApplicationUser { get; set; }

        [Required]
        public int ProductId { get; set; }

        [ForeignKey("ProductId")]
        public Product? Product { get; set; }

        public ReadingStatus Status { get; set; } = ReadingStatus.WantToRead;

        public DateTime DateAdded { get; set; } = DateTime.Now;
    }
}
