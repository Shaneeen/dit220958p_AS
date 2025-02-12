using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using dit220958p_AS.ViewModels;
using dit220958p_AS.Data;  // Add this to access AppDbContext
using dit220958p_AS.Services;
using System.Threading.Tasks;
using System.Linq;

namespace dit220958p_AS.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly AppDbContext _context;  // Inject your DbContext
        private readonly ReCaptchaService _reCaptchaService;
        private readonly AuditLogService _auditLogService;

        public LoginModel(SignInManager<IdentityUser> signInManager, AppDbContext context, ReCaptchaService reCaptchaService, AuditLogService auditLogService)
        {
            _signInManager = signInManager;
            _context = context;
            _reCaptchaService = reCaptchaService;
            _auditLogService = auditLogService;  // Initialize AuditLogService
        }

        [BindProperty]
        public LoginView Input { get; set; }

        public string ErrorMessage { get; set; }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                ErrorMessage = "Please correct the errors in the form.";
                return Page();
            }

            // Validate reCAPTCHA
            var recaptchaResponse = Request.Form["g-recaptcha-response"];
            var isRecaptchaValid = await _reCaptchaService.ValidateTokenAsync(recaptchaResponse);

            if (!isRecaptchaValid)
            {
                ErrorMessage = "reCAPTCHA validation failed. Please try again.";
                return Page();
            }

            // Check if the account is already locked out
            var user = await _signInManager.UserManager.FindByEmailAsync(Input.Email);
            if (user != null && await _signInManager.UserManager.IsLockedOutAsync(user))
            {
                var lockoutEnd = await _signInManager.UserManager.GetLockoutEndDateAsync(user);
                if (lockoutEnd.HasValue)
                {
                    var timeRemaining = lockoutEnd.Value.UtcDateTime - DateTime.UtcNow;
                    ErrorMessage = $"Account locked. Try again in {timeRemaining.Minutes} minute(s).";
                }
                else
                {
                    ErrorMessage = "Account is currently locked. Please try again later.";
                }
                return Page();
            }

            // Enable lockout on failure by setting lockoutOnFailure to true
            var result = await _signInManager.PasswordSignInAsync(Input.Email, Input.Password, Input.RememberMe, lockoutOnFailure: true);

            if (result.Succeeded)
            {
                await _auditLogService.LogAsync("Login", $"User {Input.Email} logged in successfully.");

                // Generate unique session ID
                var sessionId = Guid.NewGuid().ToString();
                HttpContext.Session.SetString("UserEmail", Input.Email);
                HttpContext.Session.SetString("SessionId", sessionId);

                var member = _context.Members.FirstOrDefault(m => m.Email == Input.Email);
                if (member != null)
                {
                    member.CurrentSessionId = sessionId;
                    _context.SaveChanges();
                }

                return RedirectToPage("/Home");
            }
            else if (result.IsLockedOut)
            {
                ErrorMessage = "Account locked due to multiple failed login attempts.";
                await _auditLogService.LogAsync("Account Locked", $"User {Input.Email}'s account is locked.");
            }

            else
            {
                ErrorMessage = "Invalid login attempt. Please check your credentials.";
                await _auditLogService.LogAsync("Failed Login", $"Invalid login attempt for {Input.Email}. Password incorrect.");
            }


            return Page();
        }
    }
}
