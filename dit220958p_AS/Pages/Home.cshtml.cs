using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using dit220958p_AS.Services;
using dit220958p_AS.Data;
using System.Threading.Tasks;
using dit220958p_AS.Models;

namespace dit220958p_AS.Pages
{
    public class HomeModel : BasePageModel  // Inherit from BasePageModel for session validation
    {
        private readonly EncryptionHelper _encryptionHelper;

        private readonly AuditLogService _auditLogService;  
        public string UserEmail { get; set; }
        public string SessionId { get; set; }
        public Member MemberDetails { get; set; }

        public HomeModel(AppDbContext context, SignInManager<IdentityUser> signInManager, AuditLogService auditLogService, EncryptionHelper encryptionHelper)
            : base(context, signInManager, auditLogService)
        {
            _auditLogService = auditLogService;
            _encryptionHelper = encryptionHelper;
        }

        public async Task<IActionResult> OnGet()
        {
            var result = await ValidateSessionAsync();  // Validate session on page load
            if (result != null) return result;          // Redirect to login if session is invalid

            // Retrieve user information from session
            UserEmail = HttpContext.Session.GetString("UserEmail");
            SessionId = HttpContext.Session.GetString("SessionId");

            // Fetch member details from the database
            MemberDetails = _context.Members.FirstOrDefault(m => m.Email == UserEmail);

            if (MemberDetails != null)
            {
                // Decrypt the NRIC before displaying
                MemberDetails.NRIC = _encryptionHelper.Decrypt(MemberDetails.NRIC, MemberDetails.NRIC_IV);
            }

            return Page();
        }

        public async Task<IActionResult> OnPostLogoutAsync()
        {
            await _auditLogService.LogAsync("Logout", $"User {HttpContext.Session.GetString("UserEmail")} logged out.");
            await _signInManager.SignOutAsync();
            HttpContext.Session.Clear();
            return RedirectToPage("/Account/Login");
        }


    }
}
