using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace dit220958p_AS.Models
{
    public class Member
    {
        [Key]
        public int Id { get; set; }

        [Required, StringLength(50)]
        public string FirstName { get; set; }

        [Required, StringLength(50)]
        public string LastName { get; set; }

        [Required]
        public string Gender { get; set; }

        [Required, StringLength(600)]
        public string NRIC { get; set; }  // Must be encrypted

        [Required]
        public string NRIC_IV { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; }  // Must be unique

        [Required]
        public string PasswordHash { get; set; }  // Store hashed password

        [NotMapped]
        [Required, Compare("PasswordHash", ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; }  // Used only for validation, not stored in DB

        [Required, Column(TypeName = "date")]
        public DateTime DateOfBirth { get; set; }

        [StringLength(255)]
        public string ResumeName { get; set; }  // Original file name

        [StringLength(255)]
        public string ResumePath { get; set; }  // File path on the server

        [DataType(DataType.MultilineText)]
        public string WhoAmI { get; set; }  // Allow special characters

        public string CurrentSessionId { get; set; } = string.Empty;
    }
}
