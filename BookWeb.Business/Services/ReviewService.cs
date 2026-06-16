using bookweb.Data;
using BookWeb.Business.Services.IServices;
using BookWeb.Models;
using Microsoft.EntityFrameworkCore;

namespace BookWeb.Business.Services
{
    public class ReviewService : IReviewService
    {
        private readonly ApplicationDbContext _db;

        public ReviewService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<Review?> GetUserReviewAsync(string userId, int productId)
        {
            return await _db.Reviews
                .Include(r => r.ApplicationUser)
                .FirstOrDefaultAsync(r => r.ApplicationUserId == userId && r.ProductId == productId);
        }

        public async Task<IEnumerable<Review>> GetReviewsForProductAsync(int productId)
        {
            return await _db.Reviews
                .Where(r => r.ProductId == productId)
                .Include(r => r.ApplicationUser)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<double> GetAverageRatingAsync(int productId)
        {
            var ratings = await _db.Reviews
                .Where(r => r.ProductId == productId)
                .Select(r => r.Rating)
                .ToListAsync();

            return ratings.Count > 0 ? ratings.Average() : 0;
        }

        public async Task<int> GetReviewCountAsync(int productId)
        {
            return await _db.Reviews.CountAsync(r => r.ProductId == productId);
        }

        public async Task<Review> CreateOrUpdateReviewAsync(string userId, int productId, int rating, string? comment)
        {
            var existing = await _db.Reviews
                .FirstOrDefaultAsync(r => r.ApplicationUserId == userId && r.ProductId == productId);

            if (existing != null)
            {
                existing.Rating = rating;
                existing.Comment = comment;
                existing.UpdatedAt = DateTime.Now;
                await _db.SaveChangesAsync();
                return existing;
            }
            else
            {
                var review = new Review
                {
                    ApplicationUserId = userId,
                    ProductId = productId,
                    Rating = rating,
                    Comment = comment
                };
                _db.Reviews.Add(review);
                await _db.SaveChangesAsync();
                return review;
            }
        }

        public async Task<bool> DeleteReviewAsync(int reviewId, string userId)
        {
            var review = await _db.Reviews
                .FirstOrDefaultAsync(r => r.Id == reviewId && r.ApplicationUserId == userId);

            if (review == null) return false;

            _db.Reviews.Remove(review);
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<Dictionary<int, double>> GetAverageRatingsForProductsAsync(IEnumerable<int> productIds)
        {
            var ids = productIds.ToList();
            return await _db.Reviews
                .Where(r => ids.Contains(r.ProductId))
                .GroupBy(r => r.ProductId)
                .Select(g => new { ProductId = g.Key, Avg = g.Average(r => r.Rating) })
                .ToDictionaryAsync(x => x.ProductId, x => x.Avg);
        }

        public async Task<Dictionary<int, int>> GetReviewCountsForProductsAsync(IEnumerable<int> productIds)
        {
            var ids = productIds.ToList();
            return await _db.Reviews
                .Where(r => ids.Contains(r.ProductId))
                .GroupBy(r => r.ProductId)
                .Select(g => new { ProductId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.ProductId, x => x.Count);
        }
    }
}
