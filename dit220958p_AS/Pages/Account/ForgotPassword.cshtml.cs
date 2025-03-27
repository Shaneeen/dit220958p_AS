using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Memory;
using dit220958p_AS.Services;

namespace dit220958p_AS.Pages.Account
{
    public class ForgotPasswordModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly EmailService _emailService;
        private readonly IMemoryCache _cache;

        public ForgotPasswordModel(UserManager<IdentityUser> userManager, EmailService emailService, IMemoryCache cache)
        {
            _userManager = userManager;
            _emailService = emailService;
            _cache = cache;
        }

        [BindProperty]
        public string Email { get; set; }
        public string ErrorMessage { get; set; }
        public string SuccessMessage { get; set; }

        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrEmpty(Email))
            {
                ErrorMessage = "Please enter your email.";
                return Page();
            }

            var user = await _userManager.FindByEmailAsync(Email);
            if (user == null)
            {
                ErrorMessage = "Email not found.";
                return Page();
            }

            // Generate OTP
            string otp = new Random().Next(100000, 999999).ToString();
            _cache.Set($"OTP_{Email}", otp, TimeSpan.FromMinutes(5));

            // Send OTP via Email
            string emailBody = $"Your password reset OTP is: <b>{otp}</b>. It expires in 5 minutes.";
            await _emailService.SendEmailAsync(Email, "Reset Password OTP", emailBody);

            SuccessMessage = "An OTP has been sent to your email.";
            return RedirectToPage("VerifyOtp", new { email = Email, reset = true });
        }
    }
}
