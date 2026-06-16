using BookWeb.Models;
using BookWeb.Utility;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace bookweb.Areas.Identity.Pages.Account
{
    public class ForgotPasswordModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailSender _emailSender;

        public ForgotPasswordModel(UserManager<ApplicationUser> userManager, IEmailSender emailSender)
        {
            _userManager = userManager;
            _emailSender = emailSender;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; } = string.Empty;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(Input.Email);
                if (user == null)
                {
                    // Don't reveal that the user does not exist
                    return RedirectToPage("./ResetPassword", new { email = Input.Email });
                }

                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var verificationCode = VerificationCodeStore.GenerateCode(user.Id, token);

                await _emailSender.SendEmailAsync(
                    Input.Email,
                    "Reset Password",
                    $"Your password reset code is: <strong>{verificationCode}</strong>");

                return RedirectToPage("./ResetPassword", new { email = Input.Email });
            }

            return Page();
        }
    }
}
