using bookweb.Data;
using BookWeb.Business.Services.IServices;
using BookWeb.Models;
using Microsoft.EntityFrameworkCore;

namespace BookWeb.Business.Services
{
    public class ShelfService : IShelfService
    {
        private readonly ApplicationDbContext _db;

        public ShelfService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IEnumerable<Shelf>> GetUserShelvesAsync(string userId)
        {
            return await _db.Shelves
                .Where(s => s.ApplicationUserId == userId)
                .Include(s => s.ShelfItems)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();
        }

        public async Task<Shelf?> GetShelfByIdAsync(int shelfId, string userId)
        {
            return await _db.Shelves
                .FirstOrDefaultAsync(s => s.Id == shelfId && s.ApplicationUserId == userId);
        }

        public async Task<Shelf?> GetShelfWithItemsAsync(int shelfId, string userId)
        {
            return await _db.Shelves
                .Include(s => s.ShelfItems)
                    .ThenInclude(si => si.Product!)
                        .ThenInclude(p => p.Category)
                .FirstOrDefaultAsync(s => s.Id == shelfId && s.ApplicationUserId == userId);
        }

        public async Task<Shelf> CreateShelfAsync(string userId, string name, string? description)
        {
            var shelf = new Shelf
            {
                ApplicationUserId = userId,
                Name = name,
                Description = description
            };
            _db.Shelves.Add(shelf);
            await _db.SaveChangesAsync();
            return shelf;
        }

        public async Task UpdateShelfAsync(int shelfId, string userId, string name, string? description)
        {
            var shelf = await _db.Shelves
                .FirstOrDefaultAsync(s => s.Id == shelfId && s.ApplicationUserId == userId);
            if (shelf == null) return;

            shelf.Name = name;
            shelf.Description = description;
            await _db.SaveChangesAsync();
        }

        public async Task<bool> DeleteShelfAsync(int shelfId, string userId)
        {
            var shelf = await _db.Shelves
                .FirstOrDefaultAsync(s => s.Id == shelfId && s.ApplicationUserId == userId);
            if (shelf == null) return false;

            _db.Shelves.Remove(shelf);
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> AddToShelfAsync(int shelfId, int productId, string userId)
        {
            var shelf = await _db.Shelves
                .FirstOrDefaultAsync(s => s.Id == shelfId && s.ApplicationUserId == userId);
            if (shelf == null) return false;

            var exists = await _db.ShelfItems
                .AnyAsync(si => si.ShelfId == shelfId && si.ProductId == productId);
            if (exists) return true;

            _db.ShelfItems.Add(new ShelfItem
            {
                ShelfId = shelfId,
                ProductId = productId
            });
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RemoveFromShelfAsync(int shelfId, int productId, string userId)
        {
            var shelf = await _db.Shelves
                .FirstOrDefaultAsync(s => s.Id == shelfId && s.ApplicationUserId == userId);
            if (shelf == null) return false;

            var item = await _db.ShelfItems
                .FirstOrDefaultAsync(si => si.ShelfId == shelfId && si.ProductId == productId);
            if (item == null) return false;

            _db.ShelfItems.Remove(item);
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<List<int>> GetShelfIdsForProductAsync(int productId, string userId)
        {
            return await _db.ShelfItems
                .Where(si => si.Shelf!.ApplicationUserId == userId && si.ProductId == productId)
                .Select(si => si.ShelfId)
                .ToListAsync();
        }
    }
}
