using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;  // For IFormFile

namespace dit220958p_AS.ViewModels
{
    public class RegisterView
    {
        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }

        [Required]
        public string Gender { get; set; }

        [Required]
        public string NRIC { get; set; }


        [Required, EmailAddress]
        public string Email { get; set; }

        [Required, DataType(DataType.Password)]
        public string Password { get; set; }

        [Required, DataType(DataType.Password), Compare("Password", ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; }

        [Required, DataType(DataType.Date)]
        public DateTime DateOfBirth { get; set; }

        public string WhoAmI { get; set; }

        [Required]
        public IFormFile Resume { get; set; }
    }
}
