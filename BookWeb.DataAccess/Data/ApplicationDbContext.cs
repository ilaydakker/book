using BookWeb.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace bookweb.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) 
        {
        }

        public DbSet<Category>Categories { get; set; }
        public DbSet<Product>Products { get; set; }
        public DbSet<ApplicationUser> ApplicationUsers { get; set; }
        public DbSet<Wishlist> Wishlists { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<Shelf> Shelves { get; set; }
        public DbSet<ShelfItem> ShelfItems { get; set; }
        public DbSet<ChatConversation> ChatConversations { get; set; }
        public DbSet<ChatMessageEntity> ChatMessages { get; set; }
        public DbSet<ShoppingCart> ShoppingCarts { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Review>()
                .HasIndex(r => new { r.ApplicationUserId, r.ProductId })
                .IsUnique();

            modelBuilder.Entity<ShelfItem>()
                .HasIndex(si => new { si.ShelfId, si.ProductId })
                .IsUnique();
        }
    }
}
