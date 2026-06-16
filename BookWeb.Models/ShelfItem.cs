using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookWeb.Models
{
    public class ShelfItem
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ShelfId { get; set; }

        [ForeignKey("ShelfId")]
        public Shelf? Shelf { get; set; }

        [Required]
        public int ProductId { get; set; }

        [ForeignKey("ProductId")]
        public Product? Product { get; set; }

        public DateTime DateAdded { get; set; } = DateTime.Now;
    }
}
