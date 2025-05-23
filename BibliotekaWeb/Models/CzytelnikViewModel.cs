using Microsoft.AspNetCore.Identity;

namespace BibliotekaWeb.Models
{
    public class CzytelnikViewModel
    {
        // Identifikator czytelnika
        public string? UserId { get; set; }
        public virtual IdentityUser? User { get; set; }
        // Książka, którą czytelnik wypożyczył
        public string? KsiazkaTytul { get; set; }
        public DateTime? TerminZwrotu { get; set; }
        // Liczba wypożyczonych książek przez czytelnika
        public int IloscWypozyczonychKsiazek { get; set; }
        // Suma kar za przetrzymanie książek
        public decimal SumaKar { get; set; }
    }
}