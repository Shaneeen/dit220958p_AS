using System;
using System.Threading.Tasks;
using dit220958p_AS.Data;
using dit220958p_AS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;

namespace dit220958p_AS.Services
{
    public class AuditLogService
    {
        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly UserManager<IdentityUser> _userManager;

        public AuditLogService(AppDbContext context, IHttpContextAccessor httpContextAccessor, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _userManager = userManager;
        }

        // Modified to accept email for failed login attempts
        public async Task LogAsync(string action, string details = null, string email = null)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            var user = await _userManager.GetUserAsync(httpContext.User);

            var log = new AuditLog
            {
                UserId = user?.Id,  // This will be null for failed login attempts
                UserEmail = user?.Email ?? email ?? "Unknown",  // Use provided email if user is null
                Action = action,
                IPAddress = httpContext.Connection.RemoteIpAddress?.ToString(),
                Timestamp = DateTime.UtcNow,
                Details = details
            };

            _context.AuditLogs.Add(log);
            await _context.SaveChangesAsync();
        }
    }
}
