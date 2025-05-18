using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BibliotekaWeb.Models
{
    public class WypozyczenieViewModel
    {
        public string CzytelnikId { get; set; }
        public string Email { get; set; }

        [Required(ErrorMessage = "Proszę wybrać książkę.")]
        [Range(1, int.MaxValue, ErrorMessage = "Proszę wybrać książkę.")]
        public int KsiazkaId { get; set; }

        public List<Ksiazka> Ksiazki { get; set; }
        public List<Tematyka> Tematyki { get; set; }
    }
}