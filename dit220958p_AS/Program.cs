using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using dit220958p_AS.Data;
using dit220958p_AS.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using System.IO.Compression;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton(new BackupService(builder.Configuration.GetConnectionString("DefaultConnection")));


builder.Services.AddRazorPages()
    .AddRazorRuntimeCompilation()  // Enable runtime compilation if needed
    .AddMvcOptions(options =>
    {
        options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()); // Automatically validate antiforgery tokens
    });

// Configure MSSQL and Identity
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configure Identity with password and lockout settings
builder.Services.AddDefaultIdentity<IdentityUser>(options =>
{
    // Password requirements
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 12;  // Enforce strong passwords

    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(1);  // Lockout duration
    options.Lockout.MaxFailedAccessAttempts = 3;  // Lock account after 3 failed attempts
    options.Lockout.AllowedForNewUsers = true;  // Apply lockout for new users
})
.AddEntityFrameworkStores<AppDbContext>();

// Add EncryptionHelper before building the app
builder.Services.AddSingleton<EncryptionHelper>();
// Add email service
builder.Services.AddSingleton<EmailService>();

builder.Services.AddSingleton<GoogleDriveService>();  // ✅ Register GoogleDriveService

builder.Services.AddMemoryCache();

builder.Services.AddHttpContextAccessor();


// Add session services
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(1);  // Session timeout after 20 mins of inactivity
    options.Cookie.HttpOnly = true;                  // Prevent access via client-side scripts
    options.Cookie.IsEssential = true;               // Ensure session cookie is always active
});

// Configure application cookies
builder.Services.ConfigureApplicationCookie(options =>
{
    options.ExpireTimeSpan = TimeSpan.FromMinutes(1);  // Cookie expiration matches session timeout
    options.SlidingExpiration = true;                   // Extend session if user is active
});

builder.Services.AddScoped<ReCaptchaService>();  // Register reCAPTCHA service

builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();


builder.Services.AddHttpContextAccessor();  // Enable HTTP context access
builder.Services.AddScoped<AuditLogService>();  // Register AuditLogService

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    // Handle unhandled exceptions (500)
    app.UseExceptionHandler("/Error");

    // Handle status code errors like 404, 403, etc.
    app.UseStatusCodePagesWithReExecute("/Error", "?code={0}");

    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();


app.UseAuthentication();  // Enable authentication
app.UseAuthorization();

app.UseSession();  // Enable session after authentication

app.UseMiddleware<PageVisitLoggingMiddleware>();

app.UseEndpoints(endpoints =>
{
    endpoints.MapRazorPages();

    // Redirect root URL to Login page if not authenticated
    endpoints.MapGet("/", async context =>
    {
        if (!context.User.Identity.IsAuthenticated)
        {
            context.Response.Redirect("/Account/Login");
        }
        else
        {
            context.Response.Redirect("/Home");
        }
        await Task.CompletedTask;
    });

    // Fallback for any unmatched routes
    endpoints.MapFallback(async context =>
    {
        context.Response.Redirect("/Error?code=404");
        await Task.CompletedTask;
    });
});

app.Lifetime.ApplicationStopping.Register(() =>
{
    Console.WriteLine("🔴 Application is stopping... Running backup before exit.");

    try
    {
        var backupService = app.Services.GetRequiredService<BackupService>();

        Task.Run(async () =>
        {
            string backupFilePath = await backupService.BackupDatabaseAsync(); // ✅ Now correctly returns a string

            if (!string.IsNullOrEmpty(backupFilePath))
            {
                Console.WriteLine($"📁 Backup file created: {backupFilePath}");

                var googleDriveService = app.Services.GetRequiredService<GoogleDriveService>();
                await googleDriveService.UploadFile(backupFilePath); // ✅ Upload the file to Google Drive

                Console.WriteLine("✅ Backup successfully uploaded to Google Drive!");
            }
            else
            {
                Console.WriteLine("❌ Backup failed. No file to upload.");
            }
        }).Wait();  // 🔥 Forces execution before shutdown
    }
    catch (Exception ex)
    {
        Console.WriteLine($"🔥 ERROR during shutdown backup: {ex.Message}");
    }
});

 app.Run();