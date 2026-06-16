using bookweb.Data;
using BookWeb.Models;
using BookWeb.Models.ViewModels;
using BookWeb.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace bookweb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public DashboardController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var adminUsers = await _userManager.GetUsersInRoleAsync(SD.Role_Admin);

            var vm = new DashboardVM
            {
                ProductCount = await _db.Products.CountAsync(),
                CategoryCount = await _db.Categories.CountAsync(),
                UserCount = await _db.ApplicationUsers.CountAsync(),
                AdminCount = adminUsers.Count,
                RecentProducts = await _db.Products
                    .Include(p => p.Category)
                    .OrderByDescending(p => p.Id)
                    .Take(5)
                    .ToListAsync()
            };

            return View(vm);
        }
    }
}
