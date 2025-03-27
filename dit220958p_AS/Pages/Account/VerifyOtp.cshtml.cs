using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Memory;
using dit220958p_AS.Data;

namespace dit220958p_AS.Pages.Account
{
    public class VerifyOtpModel : PageModel
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly IMemoryCache _cache;
        private readonly AppDbContext _context;

        public VerifyOtpModel(SignInManager<IdentityUser> signInManager, IMemoryCache cache, AppDbContext context)
        {
            _signInManager = signInManager;
            _cache = cache;
            _context = context;
        }

        [BindProperty]
        public string OtpCode { get; set; }
        public string Email { get; set; }
        public string ErrorMessage { get; set; }
        public bool IsPasswordReset { get; set; }

        public void OnGet(string email, bool reset = false)
        {
            Email = email;
            IsPasswordReset = reset;
            Console.WriteLine($"[DEBUG] VerifyOtp OnGet: Email={email}, Reset={reset}");
        }

        public async Task<IActionResult> OnPostAsync()
        {
            Email = Request.Query["email"];

            // ?? FIX: Read "reset" from Form instead of QueryString
            IsPasswordReset = Request.Form["reset"] == "true";

            Console.WriteLine($"[DEBUG] OnPostAsync Triggered: Email={Email}, Reset={IsPasswordReset}");

            if (string.IsNullOrEmpty(OtpCode))
            {
                ErrorMessage = "Please enter the OTP.";
                Console.WriteLine("[DEBUG] Error: OTP not entered.");
                return Page();
            }

            if (!_cache.TryGetValue($"OTP_{Email}", out string correctOtp))
            {
                ErrorMessage = "OTP expired or invalid.";
                Console.WriteLine($"[DEBUG] Error: OTP expired or invalid for {Email}");
                return Page();
            }

            if (OtpCode != correctOtp)
            {
                ErrorMessage = "Incorrect OTP.";
                Console.WriteLine($"[DEBUG] Error: Incorrect OTP entered for {Email}");
                return Page();
            }

            // Remove OTP from cache after successful verification
            _cache.Remove($"OTP_{Email}");
            Console.WriteLine($"[DEBUG] OTP Verified Successfully for {Email}");

            var user = await _signInManager.UserManager.FindByEmailAsync(Email);
            if (user == null)
            {
                ErrorMessage = "User not found.";
                Console.WriteLine($"[DEBUG] Error: User not found for {Email}");
                return Page();
            }

            if (IsPasswordReset)
            {
                // ? Log Password Reset Flow
                Console.WriteLine($"[DEBUG] Redirecting to ResetPassword for {Email}");
                return RedirectToPage("ResetPassword", new { email = Email });
            }

            // ? Log Normal Login Flow
            Console.WriteLine($"[DEBUG] Normal login flow for {Email}");

            // Normal login flow
            await _signInManager.SignInAsync(user, isPersistent: false);

            // Generate unique session ID
            var sessionId = Guid.NewGuid().ToString();
            HttpContext.Session.SetString("UserEmail", Email);
            HttpContext.Session.SetString("SessionId", sessionId);
            HttpContext.Session.SetString("OtpVerified", "true");

            // Update Member table with session ID
            var member = _context.Members.FirstOrDefault(m => m.Email == Email);
            if (member != null)
            {
                member.CurrentSessionId = sessionId;
                _context.SaveChanges();
            }

            Console.WriteLine($"[DEBUG] Redirecting {Email} to Home Page after login.");
            return RedirectToPage("/Home");
        }

    }
}
