using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace BibliotekaWeb.Models
{
    public class Czytelnik
    {
        // Identifikator czytelnika
        public int Id { get; set; }

        // Imie czytelnika (wymagane)
        [Required(ErrorMessage = "Imię jest wymagane.")]
        [Display(Name = "Imię")]
        public string Imie { get; set; }

        // Nazwisko czytelnika (wymagane)
        [Required(ErrorMessage = "Nazwisko jest wymagane.")]
        [Display(Name = "Nazwisko")]
        public string Nazwisko { get; set; }

        // Numer karty bibliotecznej (wymagany)
        [Required(ErrorMessage = "Numer karty jest wymagany.")]
        [Display(Name = "Numer Karty")]
        public string NumerKarty { get; set; }

        // Powiązanie z użytkownikiem Identity
        public string? UserId { get; set; }
        public virtual IdentityUser? User { get; set; }

        // Przechowuje tytuł książki, którą czytelnik wypożyczył
        public string? KsiazkaTytul { get; set; }
        // Przechowuje datę wypożyczenia książki, jeżeli została wypożyczona
        public DateTime? TerminZwrotu { get; set; }
    }
}