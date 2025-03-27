using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using dit220958p_AS.Services;
using System.Security.Claims;

public class PageVisitLoggingMiddleware
{
    private readonly RequestDelegate _next;

    public PageVisitLoggingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var auditLogService = context.RequestServices.GetRequiredService<AuditLogService>();
        var userManager = context.RequestServices.GetRequiredService<UserManager<IdentityUser>>();

        IdentityUser user = null;
        string userId = null;
        string userEmail = "Unknown";

        // Ensure HttpContext has an authenticated user
        if (context.User?.Identity?.IsAuthenticated == true)
        {
            // Retrieve user from the database based on ClaimsPrincipal
            var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userIdClaim))
            {
                user = await userManager.FindByIdAsync(userIdClaim);
                if (user != null)
                {
                    userId = user.Id;
                    userEmail = user.Email;
                }
            }
        }

        var path = context.Request.Path;
        var queryString = context.Request.QueryString.ToString();
        var userAgent = context.Request.Headers["User-Agent"].ToString();
        var referer = context.Request.Headers["Referer"].ToString();
        var ipAddress = context.Connection.RemoteIpAddress?.ToString();

        var details = $"Path: {path}, Query: {queryString}, UserAgent: {userAgent}, Referer: {referer}, IP: {ipAddress}";

        // Log page visit with user details (or "Unknown" if user is null)
        await auditLogService.LogAsync("Page Visit", details, userEmail);

        await _next(context);
    }
}
