using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using dit220958p_AS.Data;

namespace dit220958p_AS.Pages.Account
{
    public class ResetPasswordModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly AppDbContext _context;

        public ResetPasswordModel(UserManager<IdentityUser> userManager, AppDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        [BindProperty]
        public ResetPasswordViewModel Input { get; set; }
        public string ErrorMessage { get; set; }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                ErrorMessage = "Please fix the errors.";
                return Page();
            }

            var user = await _userManager.FindByEmailAsync(Input.Email);
            if (user == null)
            {
                ErrorMessage = "User not found.";
                return Page();
            }

            // Fetch the last 2 passwords from history
            var previousPasswords = _context.PasswordHistories
                .Where(ph => ph.UserId == user.Id)
                .OrderByDescending(ph => ph.ChangedDate)
                .Take(2)
                .Select(ph => ph.PasswordHash)
                .ToList();

            // Check if new password is a reuse of the last 2 passwords
            foreach (var oldPasswordHash in previousPasswords)
            {
                if (await _userManager.CheckPasswordAsync(user, Input.NewPassword))
                {
                    ErrorMessage = "You cannot reuse your last 2 passwords.";
                    return Page();
                }
            }

            // Reset Password
            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, resetToken, Input.NewPassword);

            if (!result.Succeeded)
            {
                ErrorMessage = string.Join(" ", result.Errors.Select(e => e.Description));
                return Page();
            }

            // Save new password in history
            _context.PasswordHistories.Add(new PasswordHistory
            {
                UserId = user.Id,
                PasswordHash = user.PasswordHash,
                ChangedDate = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            return RedirectToPage("/Account/Login");
        }
    }

    public class ResetPasswordViewModel
    {
        [Required]
        public string Email { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 12, ErrorMessage = "Password must be at least 12 characters.")]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; }

        [Required]
        [Compare("NewPassword", ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; }
    }
}
