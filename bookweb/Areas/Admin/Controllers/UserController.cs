using bookweb.Data;
using BookWeb.Models;
using BookWeb.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace bookweb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class UserController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public UserController(ApplicationDbContext db, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _db = db;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var users = await _db.ApplicationUsers.ToListAsync();
            var userList = new List<object>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userList.Add(new
                {
                    id = user.Id,
                    name = user.Name,
                    email = user.Email,
                    phoneNumber = user.PhoneNumber ?? "-",
                    role = roles.FirstOrDefault() ?? "No Role",
                    isLocked = user.LockoutEnd != null && user.LockoutEnd > DateTimeOffset.Now
                });
            }

            return Json(new { data = userList });
        }

        [HttpPost]
        public async Task<IActionResult> LockUnlock([FromBody] LockUnlockRequest request)
        {
            var user = await _db.ApplicationUsers.FirstOrDefaultAsync(u => u.Id == request.Id);
            if (user == null)
            {
                return Json(new { success = false, message = "User not found." });
            }

            if (user.LockoutEnd != null && user.LockoutEnd > DateTimeOffset.Now)
            {
                // Unlock
                user.LockoutEnd = DateTimeOffset.Now;
                await _db.SaveChangesAsync();
                return Json(new { success = true, message = "User unlocked successfully." });
            }
            else
            {
                // Lock for 1000 years (effectively permanent)
                user.LockoutEnd = DateTimeOffset.Now.AddYears(1000);
                await _db.SaveChangesAsync();
                return Json(new { success = true, message = "User locked successfully." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> RoleManagement(string id)
        {
            var user = await _db.ApplicationUsers.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) return NotFound();

            var roles = await _userManager.GetRolesAsync(user);
            var allRoles = await _roleManager.Roles.ToListAsync();

            ViewBag.UserName = user.Name;
            ViewBag.UserEmail = user.Email;
            ViewBag.UserId = user.Id;
            ViewBag.CurrentRole = roles.FirstOrDefault() ?? "";
            ViewBag.RoleList = allRoles.Select(r => new SelectListItem
            {
                Text = r.Name,
                Value = r.Name,
                Selected = roles.Contains(r.Name!)
            }).ToList();

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> RoleManagement(string userId, string role)
        {
            var user = await _db.ApplicationUsers.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return NotFound();

            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            await _userManager.AddToRoleAsync(user, role);

            TempData["success"] = $"Role updated to '{role}' for {user.Name}.";
            return RedirectToAction(nameof(Index));
        }
    }

    public class LockUnlockRequest
    {
        public string Id { get; set; } = string.Empty;
    }
}
