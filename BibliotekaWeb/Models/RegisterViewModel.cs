﻿using System.ComponentModel.DataAnnotations;

namespace BibliotekaWeb.Models
{
    public class RegisterViewModel
    {
        // Email użytkownika
        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }

        //  Hasło użytkownika
        // minimum 6 znaków, maksymalnie 100 znaków
        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        // Maskowanie hasła
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        // Potwierdzenie hasła użytkownika
        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }
    }
}
