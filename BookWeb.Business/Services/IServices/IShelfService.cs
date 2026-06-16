using BookWeb.Models;

namespace BookWeb.Business.Services.IServices
{
    public interface IShelfService
    {
        Task<IEnumerable<Shelf>> GetUserShelvesAsync(string userId);
        Task<Shelf?> GetShelfByIdAsync(int shelfId, string userId);
        Task<Shelf?> GetShelfWithItemsAsync(int shelfId, string userId);
        Task<Shelf> CreateShelfAsync(string userId, string name, string? description);
        Task UpdateShelfAsync(int shelfId, string userId, string name, string? description);
        Task<bool> DeleteShelfAsync(int shelfId, string userId);
        Task<bool> AddToShelfAsync(int shelfId, int productId, string userId);
        Task<bool> RemoveFromShelfAsync(int shelfId, int productId, string userId);
        Task<List<int>> GetShelfIdsForProductAsync(int productId, string userId);
    }
}
