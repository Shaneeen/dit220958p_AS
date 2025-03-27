using Microsoft.AspNetCore.Identity;

public class PasswordHistory
{
    public int Id { get; set; }
    public string UserId { get; set; }  // Foreign Key to IdentityUser or Member
    public string PasswordHash { get; set; }
    public DateTime ChangedDate { get; set; }

    public IdentityUser User { get; set; }  // Navigation property (if using IdentityUser)
}
