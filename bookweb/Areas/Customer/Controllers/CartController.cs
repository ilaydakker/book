using System.Security.Claims;
using bookweb.Data;
using BookWeb.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace bookweb.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize]
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _db;

        public CartController(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var cartItems = await _db.ShoppingCarts
                .Include(c => c.Product)
                .ThenInclude(p => p.Category)
                .Where(c => c.ApplicationUserId == userId)
                .ToListAsync();

            double orderTotal = 0;
            foreach (var item in cartItems)
            {
                item.Price = GetPriceBasedOnQuantity(item);
                orderTotal += item.Price * item.Count;
            }

            ViewBag.OrderTotal = orderTotal;
            return View(cartItems);
        }

        [HttpPost]
        public async Task<IActionResult> AddToCart(int productId, int count = 1)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Challenge();

            var existingCart = await _db.ShoppingCarts
                .FirstOrDefaultAsync(c => c.ApplicationUserId == userId && c.ProductId == productId);

            if (existingCart != null)
            {
                existingCart.Count += count;
                _db.ShoppingCarts.Update(existingCart);
            }
            else
            {
                var cart = new ShoppingCart
                {
                    ProductId = productId,
                    ApplicationUserId = userId,
                    Count = count
                };
                await _db.ShoppingCarts.AddAsync(cart);
            }

            await _db.SaveChangesAsync();
            TempData["success"] = "Product added to cart successfully!";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> UpdateQuantity(int cartId, int count)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var cart = await _db.ShoppingCarts
                .FirstOrDefaultAsync(c => c.Id == cartId && c.ApplicationUserId == userId);

            if (cart == null) return NotFound();

            if (count <= 0)
            {
                _db.ShoppingCarts.Remove(cart);
            }
            else
            {
                cart.Count = count;
                _db.ShoppingCarts.Update(cart);
            }

            await _db.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Remove(int cartId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var cart = await _db.ShoppingCarts
                .FirstOrDefaultAsync(c => c.Id == cartId && c.ApplicationUserId == userId);

            if (cart == null) return NotFound();

            _db.ShoppingCarts.Remove(cart);
            await _db.SaveChangesAsync();
            TempData["success"] = "Item removed from cart.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Clear()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var cartItems = await _db.ShoppingCarts
                .Where(c => c.ApplicationUserId == userId)
                .ToListAsync();

            _db.ShoppingCarts.RemoveRange(cartItems);
            await _db.SaveChangesAsync();
            TempData["success"] = "Cart cleared.";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> GetCartCount()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Json(new { count = 0 });

            var count = await _db.ShoppingCarts
                .Where(c => c.ApplicationUserId == userId)
                .SumAsync(c => c.Count);

            return Json(new { count });
        }

        private double GetPriceBasedOnQuantity(ShoppingCart cart)
        {
            if (cart.Count <= 50)
                return cart.Product.Price;
            else if (cart.Count <= 100)
                return cart.Product.Price50;
            else
                return cart.Product.Price100;
        }
    }
}
