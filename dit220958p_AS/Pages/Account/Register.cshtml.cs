using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using dit220958p_AS.Data;
using dit220958p_AS.Models;
using dit220958p_AS.Services;
using dit220958p_AS.ViewModels;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Net;

namespace dit220958p_AS.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly EncryptionHelper _encryptionHelper;

        public RegisterModel(AppDbContext context, UserManager<IdentityUser> userManager, EncryptionHelper encryptionHelper)
        {
            _context = context;
            _userManager = userManager;
            _encryptionHelper = encryptionHelper;
        }

        [BindProperty]
        public RegisterView Input { get; set; }
        public string ErrorMessage { get; set; }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                ErrorMessage = "Please correct the errors in the form.";
                return Page();
            }

            // Validate Gender
            var allowedGenders = new[] { "Male", "Female", "Other" };
            if (!allowedGenders.Contains(Input.Gender))
            {
                ErrorMessage = "Invalid gender selection.";
                return Page();
            }

            // Validate Password Strength on Server Side
            var passwordStrength = CheckPasswordStrength(Input.Password);
            if (passwordStrength < 4)
            {
                ErrorMessage = "Password is too weak. Please choose a stronger password.";
                return Page();
            }

            // Check for duplicate email in Members table
            if (_context.Members.Any(m => m.Email == Input.Email))
            {
                ErrorMessage = "Email is already registered.";
                return Page();
            }

            // Encrypt NRIC and get the unique IV
            var (encryptedNRIC, nricIV) = _encryptionHelper.Encrypt(Input.NRIC);

            // Handle resume file upload
            string resumePath = null;
            string resumeName = null;

            if (Input.Resume != null)
            {
                var allowedExtensions = new[] { ".pdf", ".docx" };
                var fileExtension = Path.GetExtension(Input.Resume.FileName).ToLower();

                if (!allowedExtensions.Contains(fileExtension))
                {
                    ErrorMessage = "Only PDF and DOCX files are allowed.";
                    return Page();
                }

                // Generate unique file name to avoid overwriting
                resumeName = $"{Guid.NewGuid()}{fileExtension}";

                // Set upload folder within wwwroot for web access
                string uploadsFolder = Path.Combine("wwwroot", "uploads");

                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                // Absolute path for saving the file
                string absolutePath = Path.Combine(Directory.GetCurrentDirectory(), uploadsFolder, resumeName);

                using (var stream = new FileStream(absolutePath, FileMode.Create))
                {
                    await Input.Resume.CopyToAsync(stream);
                }

                // Save relative path for easy retrieval in the application
                resumePath = $"/uploads/{resumeName}";
            }


            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // First, create IdentityUser to generate password hash
                    var user = new IdentityUser { UserName = Input.Email, Email = Input.Email };
                    var result = await _userManager.CreateAsync(user, Input.Password);

                    if (!result.Succeeded)
                    {
                        ErrorMessage = string.Join(" ", result.Errors.Select(e => e.Description));
                        await transaction.RollbackAsync();
                        return Page();
                    }

                    // After successful user creation, store password in PasswordHistory table
                    var passwordHistory = new PasswordHistory
                    {
                        UserId = user.Id,                  // Link to IdentityUser
                        PasswordHash = user.PasswordHash,  // Store hashed password
                        ChangedDate = DateTime.UtcNow      // Timestamp of creation
                    };

                    _context.PasswordHistories.Add(passwordHistory);
                    await _context.SaveChangesAsync();  // Save PasswordHistory before creating Member



                    // Now, create the Member with the hashed password from IdentityUser
                    var member = new Member
                    {
                        FirstName = WebUtility.HtmlEncode(Input.FirstName),
                        LastName = WebUtility.HtmlEncode(Input.LastName),
                        Gender = Input.Gender,
                        NRIC = encryptedNRIC,
                        NRIC_IV = nricIV,
                        Email = WebUtility.HtmlEncode(Input.Email), // Encode Email as well
                        DateOfBirth = Input.DateOfBirth,
                        WhoAmI = WebUtility.HtmlEncode(Input.WhoAmI),
                        ResumeName = resumeName,
                        ResumePath = resumePath,
                        PasswordHash = user.PasswordHash
                    };


                    // Add Member after setting PasswordHash
                    _context.Members.Add(member);
                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();
                    return RedirectToPage("/Home");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    ErrorMessage = $"An error occurred during registration: {ex.InnerException?.Message ?? ex.Message}";
                    return Page();
                }
            }

        }
        // Place the JsonResult method here for real-time email checking
        public JsonResult OnGetCheckEmail(string email)
        {
            bool isEmailTaken = _context.Members.Any(m => m.Email == email);
            return new JsonResult(new { isEmailTaken });
        }
        // Password Strength Checker (returns strength level 0-5)
        private int CheckPasswordStrength(string password)
        {
            int score = 0;

            if (password.Length >= 12)
                score++;
            if (password.Any(char.IsLower) && password.Any(char.IsUpper))
                score++;
            if (password.Any(char.IsDigit))
                score++;
            if (password.Any(ch => "!@#$%^&*()_+-=[]{}|;:',.<>/?".Contains(ch)))
                score++;
            if (password.Length >= 16)
                score++;  // Extra point for very long passwords

            return score;
        }



    }
}
