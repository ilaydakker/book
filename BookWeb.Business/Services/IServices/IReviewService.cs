using BookWeb.Models;

namespace BookWeb.Business.Services.IServices
{
    public interface IReviewService
    {
        Task<Review?> GetUserReviewAsync(string userId, int productId);
        Task<IEnumerable<Review>> GetReviewsForProductAsync(int productId);
        Task<double> GetAverageRatingAsync(int productId);
        Task<int> GetReviewCountAsync(int productId);
        Task<Review> CreateOrUpdateReviewAsync(string userId, int productId, int rating, string? comment);
        Task<bool> DeleteReviewAsync(int reviewId, string userId);
        Task<Dictionary<int, double>> GetAverageRatingsForProductsAsync(IEnumerable<int> productIds);
        Task<Dictionary<int, int>> GetReviewCountsForProductsAsync(IEnumerable<int> productIds);
    }
}
