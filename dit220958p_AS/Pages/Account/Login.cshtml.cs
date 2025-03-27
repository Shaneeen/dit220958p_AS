using System;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Memory;
using dit220958p_AS.ViewModels;
using dit220958p_AS.Data;
using dit220958p_AS.Services;

namespace dit220958p_AS.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly AppDbContext _context;
        private readonly ReCaptchaService _reCaptchaService;
        private readonly AuditLogService _auditLogService;
        private readonly EmailService _emailService;
        private readonly IMemoryCache _cache;

        public LoginModel(SignInManager<IdentityUser> signInManager,
                          AppDbContext context,
                          ReCaptchaService reCaptchaService,
                          AuditLogService auditLogService,
                          EmailService emailService,
                          IMemoryCache cache)
        {
            _signInManager = signInManager;
            _context = context;
            _reCaptchaService = reCaptchaService;
            _auditLogService = auditLogService;
            _emailService = emailService;
            _cache = cache;
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

                // Generate OTP
                string otp = GenerateOtp();
                _cache.Set($"OTP_{Input.Email}", otp, TimeSpan.FromMinutes(5));  // Store OTP for 5 minutes
                //console the otp
                Console.WriteLine($"[DEBUG] OTP for {Input.Email}: {otp}");
                // Send OTP via Email
                string emailBody = $"Your OTP for login is: <b>{otp}</b>. It expires in 5 minutes.";
                await _emailService.SendEmailAsync(Input.Email, "Your OTP Code", emailBody);

                // Redirect to OTP verification page
                return RedirectToPage("VerifyOtp", new { email = Input.Email });
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

        private string GenerateOtp()
        {
            using (var rng = new RNGCryptoServiceProvider())
            {
                var bytes = new byte[4];
                rng.GetBytes(bytes);
                int otpValue = BitConverter.ToUInt16(bytes, 0) % 1000000;
                return otpValue.ToString("D6");  // 6-digit OTP
            }
        }
    }
}
