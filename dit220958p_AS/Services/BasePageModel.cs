using dit220958p_AS.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;

namespace dit220958p_AS.Services
{
    public class BasePageModel : PageModel
    {
        protected readonly AppDbContext _context;  // Changed from private to protected
        protected readonly SignInManager<IdentityUser> _signInManager;  // Changed from private to protected
        protected readonly AuditLogService _auditLogService;

        public BasePageModel(AppDbContext context, SignInManager<IdentityUser> signInManager, AuditLogService auditLogService)
        {
            _context = context;
            _signInManager = signInManager;
            _auditLogService = auditLogService;  // Initialize AuditLogService
        }

        public async Task<IActionResult> ValidateSessionAsync()
        {
            var sessionId = HttpContext.Session.GetString("SessionId");
            var userEmail = HttpContext.Session.GetString("UserEmail");

            if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(userEmail))
            {
                return RedirectToPage("/Account/Login"); // No session data, redirect to login
            }

            var member = _context.Members.FirstOrDefault(m => m.Email == userEmail);

            if (member == null || member.CurrentSessionId != sessionId)
            {
                // Session mismatch detected, log out the user
                await _auditLogService.LogAsync("Session Conflict", $"User {userEmail} was logged out due to a session conflict (multiple logins).");
                // Session mismatch detected, log out the user
                await _signInManager.SignOutAsync();
                HttpContext.Session.Clear();
                return RedirectToPage("/Account/Login");
            }

            return null;  // Session is valid, continue to the requested page
        }
    }
}

