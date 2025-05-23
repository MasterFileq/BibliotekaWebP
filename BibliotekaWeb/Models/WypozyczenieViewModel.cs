using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BibliotekaWeb.Models
{
    public class WypozyczenieViewModel
    {
        // Id czytelnika, któremu przypisujemy książke
        public string CzytelnikId { get; set; }
        // Email czytelnika, któremu przypisujemy książke
        public string Email { get; set; }
        // Id książki, którą wypożyczamy
        [Required(ErrorMessage = "Proszę wybrać książkę.")]
        [Range(1, int.MaxValue, ErrorMessage = "Proszę wybrać książkę.")]
        public int KsiazkaId { get; set; }
        // Lista książek do wyboru
        public List<Ksiazka> Ksiazki { get; set; }
        // Lista tematyk książek
        public List<Tematyka> Tematyki { get; set; }
    }
}