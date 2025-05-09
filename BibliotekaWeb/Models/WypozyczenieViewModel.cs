﻿using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BibliotekaWeb.Models
{
    public class WypozyczenieViewModel
    {
        [Required(ErrorMessage = "Identyfikator czytelnika jest wymagany.")]
        public string CzytelnikId { get; set; }

        [Required(ErrorMessage = "Proszę wybrać książkę.")]
        [Range(1, int.MaxValue, ErrorMessage = "Proszę wybrać książkę.")]
        public int KsiazkaId { get; set; }

        [Required(ErrorMessage = "Adres e-mail jest wymagany.")]
        [EmailAddress(ErrorMessage = "Nieprawidłowy format adresu e-mail.")]
        public string Email { get; set; }

        public List<Ksiazka> Ksiazki { get; set; }
    }
}