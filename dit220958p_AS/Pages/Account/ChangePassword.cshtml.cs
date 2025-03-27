using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using dit220958p_AS.Data;
using dit220958p_AS.Services;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace dit220958p_AS.Pages.Account
{
    public class ChangePasswordModel : BasePageModel  // Inherit from BasePageModel for session validation
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly AppDbContext _context;
        private readonly int _minPasswordAgeDays = 0;   // Minimum password age (e.g., 1 day)
        private readonly int _maxPasswordAgeDays = 90;  // Maximum password age (e.g., 90 days)

        public ChangePasswordModel(UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager, AppDbContext context, AuditLogService auditLogService)
            : base(context, signInManager, auditLogService)  // Pass dependencies to BasePageModel
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
        }

        [BindProperty]
        public ChangePasswordViewModel Input { get; set; }

        public string ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var result = await ValidateSessionAsync();  // Validate session on page load
            if (result != null) return result;          // Redirect to login if session is invalid

            return Page();
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostAsync()
        {
            var result = await ValidateSessionAsync();  // Validate session before processing form submission
            if (result != null) return result;          // Redirect to login if session is invalid

            if (!ModelState.IsValid)
            {
                ErrorMessage = "Please correct the errors.";
                return Page();
            }

            var userEmail = HttpContext.Session.GetString("UserEmail");
            var user = await _userManager.FindByEmailAsync(userEmail);  // Get user from session email

            if (user == null)
            {
                ErrorMessage = "User not found.";
                return RedirectToPage("/Account/Login");
            }

            // Fetch the most recent password change
            var latestPasswordHistory = _context.PasswordHistories
                .Where(ph => ph.UserId == user.Id)
                .OrderByDescending(ph => ph.ChangedDate)
                .FirstOrDefault();

            if (latestPasswordHistory != null)
            {
                var passwordAgeDays = (DateTime.UtcNow - latestPasswordHistory.ChangedDate).TotalDays;

                // Enforce minimum password age
                if (passwordAgeDays < _minPasswordAgeDays)
                {
                    ErrorMessage = $"You must wait at least {_minPasswordAgeDays} days before changing your password again.";
                    return Page();
                }

                // Enforce maximum password age (Force password change if it's too old)
                if (passwordAgeDays > _maxPasswordAgeDays)
                {
                    ErrorMessage = "Your password has expired. Please set a new password.";
                }
            }

            // Check password history (Prevent reuse of last 2 passwords)
            var previousPasswords = _context.PasswordHistories
                .Where(ph => ph.UserId == user.Id)
                .OrderByDescending(ph => ph.ChangedDate)
                .Take(2)
                .Select(ph => ph.PasswordHash)
                .ToList();

            foreach (var oldPasswordHash in previousPasswords)
            {
                if (await _userManager.CheckPasswordAsync(user, Input.NewPassword))
                {
                    ErrorMessage = "You cannot reuse your last 2 passwords.";
                    return Page();
                }
            }

            // Change password
            var resultChange = await _userManager.ChangePasswordAsync(user, Input.CurrentPassword, Input.NewPassword);

            if (!resultChange.Succeeded)
            {
                ErrorMessage = string.Join(" ", resultChange.Errors.Select(e => e.Description));
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
            await _signInManager.RefreshSignInAsync(user);

            return RedirectToPage("/Home");
        }
    }

    public class ChangePasswordViewModel
    {
        [Required]
        public string CurrentPassword { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "Password must be at least {2} characters long.", MinimumLength = 12)]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; }

        [Required]
        [Compare("NewPassword", ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; }
    }
}
