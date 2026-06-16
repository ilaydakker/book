using System.Diagnostics;
using System.Security.Claims;
using bookweb.Data;
using BookWeb.Business.Services.IServices;
using BookWeb.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace bookweb.Areas.Customer.Controllers
{ 
    [Area("Customer")]
    public class HomeController : Controller
    {
        private readonly IProductService _productService;
        private readonly ICategoryService _categoryService;
        private readonly IReviewService _reviewService;
        private readonly IShelfService _shelfService;
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public HomeController(IProductService productService, ICategoryService categoryService, IReviewService reviewService, IShelfService shelfService, ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _productService = productService;
            _categoryService = categoryService;
            _reviewService = reviewService;
            _shelfService = shelfService;
            _db = db;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(string? search, int? categoryId, string? sortBy, int page = 1)
        {
            const int pageSize = 12;

            var products = await _productService.GetAllProductsAsync(includeCategory: true);
            var productList = products.ToList();

            // Search
            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.ToLower();
                productList = productList.Where(p =>
                    p.Title.ToLower().Contains(s) ||
                    p.Author.ToLower().Contains(s)
                ).ToList();
            }

            // Category filter
            if (categoryId.HasValue && categoryId > 0)
            {
                productList = productList.Where(p => p.CategoryId == categoryId).ToList();
            }

            // Sort
            productList = sortBy switch
            {
                "price_asc" => productList.OrderBy(p => p.Price).ToList(),
                "price_desc" => productList.OrderByDescending(p => p.Price).ToList(),
                "title_asc" => productList.OrderBy(p => p.Title).ToList(),
                "title_desc" => productList.OrderByDescending(p => p.Title).ToList(),
                "newest" => productList.OrderByDescending(p => p.Id).ToList(),
                _ => productList
            };

            var totalItems = productList.Count;
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            page = Math.Max(1, Math.Min(page, totalPages));

            var pagedProducts = productList.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            var categories = await _categoryService.GetAllCategoriesAsync();
            ViewBag.Categories = categories;
            ViewBag.CurrentSearch = search;
            ViewBag.CurrentCategoryId = categoryId;
            ViewBag.CurrentSort = sortBy;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;

            // Wishlist IDs for current user
            if (User.Identity?.IsAuthenticated == true)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var userBooks = await _db.Wishlists
                    .Where(w => w.ApplicationUserId == userId)
                    .Select(w => new { w.ProductId, w.Status })
                    .ToListAsync();
                ViewBag.WishlistIds = userBooks.Select(w => w.ProductId).ToList();
                ViewBag.UserBookStatuses = userBooks.ToDictionary(w => w.ProductId, w => (int)w.Status);
            }
            else
            {
                ViewBag.WishlistIds = new List<int>();
                ViewBag.UserBookStatuses = new Dictionary<int, int>();
            }

            // Average ratings for displayed products
            var productIds = pagedProducts.Select(p => p.Id).ToList();
            ViewBag.AverageRatings = await _reviewService.GetAverageRatingsForProductsAsync(productIds);
            ViewBag.ReviewCounts = await _reviewService.GetReviewCountsForProductsAsync(productIds);

            return View(pagedProducts);
        }

        public async Task<IActionResult> Details(int productid)
        {
            var product = await _productService.GetProductByIdAsync(productid, includeCategory: true);

            if (product == null)
                return NotFound();

            if (User.Identity?.IsAuthenticated == true)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var userBook = await _db.Wishlists.FirstOrDefaultAsync(w => w.ApplicationUserId == userId && w.ProductId == productid);
                ViewBag.IsInWishlist = userBook != null;
                ViewBag.ReadingStatus = userBook?.Status;
                ViewBag.UserReview = await _reviewService.GetUserReviewAsync(userId!, productid);
            }
            else
            {
                ViewBag.IsInWishlist = false;
                ViewBag.ReadingStatus = null;
                ViewBag.UserReview = null;
            }

            ViewBag.Reviews = await _reviewService.GetReviewsForProductAsync(productid);
            ViewBag.AverageRating = await _reviewService.GetAverageRatingAsync(productid);
            ViewBag.ReviewCount = await _reviewService.GetReviewCountAsync(productid);

            // Related books: same category or same author (excluding current)
            var relatedBooks = await _db.Products
                .Include(p => p.Category)
                .Where(p => p.Id != productid && (p.CategoryId == product.CategoryId || p.Author == product.Author))
                .OrderBy(p => Guid.NewGuid())
                .Take(6)
                .ToListAsync();
            ViewBag.RelatedBooks = relatedBooks;

            return View(product);
        }

        #region Reading Status

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> UpdateReadingStatus([FromBody] ReadingStatusRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var existing = await _db.Wishlists.FirstOrDefaultAsync(w => w.ApplicationUserId == userId && w.ProductId == request.ProductId);

            if (existing != null)
            {
                existing.Status = request.Status;
                await _db.SaveChangesAsync();
                return Json(new { success = true, status = (int)existing.Status, message = $"Marked as {GetStatusLabel(request.Status)}" });
            }
            else
            {
                _db.Wishlists.Add(new Wishlist
                {
                    ApplicationUserId = userId!,
                    ProductId = request.ProductId,
                    Status = request.Status
                });
                await _db.SaveChangesAsync();
                return Json(new { success = true, status = (int)request.Status, message = $"Marked as {GetStatusLabel(request.Status)}" });
            }
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> RemoveFromLibrary([FromBody] RemoveFromLibraryRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var existing = await _db.Wishlists.FirstOrDefaultAsync(w => w.ApplicationUserId == userId && w.ProductId == request.ProductId);

            if (existing != null)
            {
                _db.Wishlists.Remove(existing);
                await _db.SaveChangesAsync();
            }

            return Json(new { success = true, message = "Removed from your library" });
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> ToggleWishlist([FromBody] WishlistToggleRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var existing = await _db.Wishlists.FirstOrDefaultAsync(w => w.ApplicationUserId == userId && w.ProductId == request.ProductId);

            if (existing != null)
            {
                _db.Wishlists.Remove(existing);
                await _db.SaveChangesAsync();
                return Json(new { success = true, added = false, message = "Removed from library" });
            }
            else
            {
                _db.Wishlists.Add(new Wishlist
                {
                    ApplicationUserId = userId!,
                    ProductId = request.ProductId,
                    Status = ReadingStatus.WantToRead
                });
                await _db.SaveChangesAsync();
                return Json(new { success = true, added = true, message = "Added to Want to Read" });
            }
        }

        [Authorize]
        public async Task<IActionResult> MyBooks(string? status)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            IQueryable<Wishlist> query = _db.Wishlists
                .Where(w => w.ApplicationUserId == userId)
                .Include(w => w.Product!)
                    .ThenInclude(p => p.Category);

            if (!string.IsNullOrEmpty(status) && Enum.TryParse<ReadingStatus>(status, out var parsedStatus))
            {
                query = query.Where(w => w.Status == parsedStatus);
            }

            var items = await query.OrderByDescending(w => w.DateAdded).ToListAsync();

            ViewBag.CurrentStatus = status;
            ViewBag.TotalCount = await _db.Wishlists.CountAsync(w => w.ApplicationUserId == userId);
            ViewBag.WantToReadCount = await _db.Wishlists.CountAsync(w => w.ApplicationUserId == userId && w.Status == ReadingStatus.WantToRead);
            ViewBag.CurrentlyReadingCount = await _db.Wishlists.CountAsync(w => w.ApplicationUserId == userId && w.Status == ReadingStatus.CurrentlyReading);
            ViewBag.ReadCount = await _db.Wishlists.CountAsync(w => w.ApplicationUserId == userId && w.Status == ReadingStatus.Read);

            return View(items);
        }

        // Keep old Wishlist action for backward compatibility (redirects to MyBooks)
        [Authorize]
        public IActionResult Wishlist()
        {
            return RedirectToAction(nameof(MyBooks));
        }

        private static string GetStatusLabel(ReadingStatus status) => status switch
        {
            ReadingStatus.WantToRead => "Want to Read",
            ReadingStatus.CurrentlyReading => "Currently Reading",
            ReadingStatus.Read => "Read",
            _ => "Unknown"
        };

        #endregion

        #region Reviews

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> SubmitReview([FromBody] SubmitReviewRequest request)
        {
            if (request.Rating < 1 || request.Rating > 5)
                return Json(new { success = false, message = "Rating must be between 1 and 5." });

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isUpdate = (await _reviewService.GetUserReviewAsync(userId!, request.ProductId)) != null;
            var review = await _reviewService.CreateOrUpdateReviewAsync(userId!, request.ProductId, request.Rating, request.Comment);
            var avgRating = await _reviewService.GetAverageRatingAsync(request.ProductId);
            var reviewCount = await _reviewService.GetReviewCountAsync(request.ProductId);
            var userName = (await _userManager.GetUserAsync(User))?.Name ?? "User";

            return Json(new
            {
                success = true,
                isUpdate,
                message = isUpdate ? "Review updated!" : "Review submitted!",
                averageRating = Math.Round(avgRating, 1),
                reviewCount,
                review = new
                {
                    review.Id,
                    review.Rating,
                    review.Comment,
                    CreatedAt = review.CreatedAt.ToString("MMM dd, yyyy"),
                    IsEdited = review.UpdatedAt.HasValue,
                    UserName = userName
                }
            });
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> DeleteReview([FromBody] DeleteReviewRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var review = await _db.Reviews.FindAsync(request.ReviewId);
            var productId = review?.ProductId;
            var result = await _reviewService.DeleteReviewAsync(request.ReviewId, userId!);

            double avgRating = 0;
            int reviewCount = 0;
            if (result && productId.HasValue)
            {
                avgRating = await _reviewService.GetAverageRatingAsync(productId.Value);
                reviewCount = await _reviewService.GetReviewCountAsync(productId.Value);
            }

            return Json(new { success = result, message = result ? "Review deleted." : "Could not delete review.", averageRating = Math.Round(avgRating, 1), reviewCount });
        }

        #endregion

        #region Shelves

        [Authorize]
        public async Task<IActionResult> Shelves()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var shelves = await _shelfService.GetUserShelvesAsync(userId!);
            return View(shelves);
        }

        [Authorize]
        public async Task<IActionResult> ShelfDetails(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var shelf = await _shelfService.GetShelfWithItemsAsync(id, userId!);
            if (shelf == null) return NotFound();
            return View(shelf);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CreateShelf([FromBody] CreateShelfRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                return Json(new { success = false, message = "Shelf name is required." });

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var shelf = await _shelfService.CreateShelfAsync(userId!, request.Name.Trim(), request.Description?.Trim());
            return Json(new { success = true, message = "Shelf created!", shelf = new { shelf.Id, shelf.Name, shelf.Description } });
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> UpdateShelf([FromBody] UpdateShelfRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                return Json(new { success = false, message = "Shelf name is required." });

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            await _shelfService.UpdateShelfAsync(request.ShelfId, userId!, request.Name.Trim(), request.Description?.Trim());
            return Json(new { success = true, message = "Shelf updated!" });
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> DeleteShelf([FromBody] DeleteShelfRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _shelfService.DeleteShelfAsync(request.ShelfId, userId!);
            return Json(new { success = result, message = result ? "Shelf deleted." : "Could not delete shelf." });
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> ToggleShelfItem([FromBody] ToggleShelfItemRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var shelfIds = await _shelfService.GetShelfIdsForProductAsync(request.ProductId, userId!);

            if (shelfIds.Contains(request.ShelfId))
            {
                await _shelfService.RemoveFromShelfAsync(request.ShelfId, request.ProductId, userId!);
                return Json(new { success = true, added = false, message = "Removed from shelf." });
            }
            else
            {
                await _shelfService.AddToShelfAsync(request.ShelfId, request.ProductId, userId!);
                return Json(new { success = true, added = true, message = "Added to shelf!" });
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetShelvesForProduct(int productId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var shelves = await _shelfService.GetUserShelvesAsync(userId!);
            var shelfIdsForProduct = await _shelfService.GetShelfIdsForProductAsync(productId, userId!);

            var result = shelves.Select(s => new
            {
                s.Id,
                s.Name,
                ItemCount = s.ShelfItems.Count,
                IsInShelf = shelfIdsForProduct.Contains(s.Id)
            });

            return Json(new { success = true, shelves = result });
        }

        #endregion

        #region Profile

        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            return View(user);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(ApplicationUser model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            user.Name = model.Name;
            user.PhoneNumber = model.PhoneNumber;
            user.StreetAddress = model.StreetAddress;
            user.City = model.City;
            user.State = model.State;
            user.PostalCode = model.PostalCode;

            await _userManager.UpdateAsync(user);
            TempData["success"] = "Profile updated successfully.";
            return RedirectToAction(nameof(Profile));
        }

        #endregion

        public IActionResult Privacy()
        {
            return View(); 
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }

    public class WishlistToggleRequest
    {
        public int ProductId { get; set; }
    }

    public class ReadingStatusRequest
    {
        public int ProductId { get; set; }
        public ReadingStatus Status { get; set; }
    }

    public class RemoveFromLibraryRequest
    {
        public int ProductId { get; set; }
    }

    public class SubmitReviewRequest
    {
        public int ProductId { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
    }

    public class DeleteReviewRequest
    {
        public int ReviewId { get; set; }
    }

    public class CreateShelfRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    public class UpdateShelfRequest
    {
        public int ShelfId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    public class DeleteShelfRequest
    {
        public int ShelfId { get; set; }
    }

    public class ToggleShelfItemRequest
    {
        public int ShelfId { get; set; }
        public int ProductId { get; set; }
    }
}
