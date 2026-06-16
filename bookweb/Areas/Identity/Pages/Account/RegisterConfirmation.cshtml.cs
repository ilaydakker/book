using BookWeb.Models;
using BookWeb.Utility;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace bookweb.Areas.Identity.Pages.Account
{
    public class RegisterConfirmationModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public RegisterConfirmationModel(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string? Email { get; set; }

        public class InputModel
        {
            [Required]
            [StringLength(6, MinimumLength = 6, ErrorMessage = "Code must be 6 digits.")]
            [Display(Name = "Verification Code")]
            public string Code { get; set; } = string.Empty;
        }

        public void OnGet(string? email)
        {
            Email = email;
        }

        public async Task<IActionResult> OnPostAsync(string? email)
        {
            Email = email;

            if (!ModelState.IsValid)
            {
                return Page();
            }

            var (userId, token) = VerificationCodeStore.GetToken(Input.Code);

            if (userId == null || token == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid or expired verification code.");
                return Page();
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "User not found.");
                return Page();
            }

            var result = await _userManager.ConfirmEmailAsync(user, token);
            if (result.Succeeded)
            {
                await _signInManager.SignInAsync(user, isPersistent: false);
                return RedirectToPage("/Account/ConfirmEmail", new { confirmed = true });
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return Page();
        }
    }
}
