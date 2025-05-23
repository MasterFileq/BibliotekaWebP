using Microsoft.AspNetCore.Identity;
using Microsoft.CodeAnalysis;

namespace BibliotekaWeb.Models
{
    public class Wypozyczenie
    {
        // Id wypożyczenia
        public int Id { get; set; }

        // Id książki, którą wypożyczamy
        public int KsiazkaId { get; set; }

        // Id czytelnika, któremu przypisujemy książkę
        public string CzytelnikId { get; set; }

        // Data wypożyczenia
        public DateTime DataWypozyczenia { get; set; }

        // Termin do kiedy książka powinna być zwrócona
        public DateTime TerminZwrotu { get; set; }

        // Kara naliczona za przetrzymanie książki
        public decimal Kara { get; set; }
        // Liczba przedłużeń wypożyczenia
        public int LiczbaPrzedluzen { get; set; }

        // Status wypożyczenia, true jeśli książka została zwrócona, false jeśli nadal jest wypożyczona
        public bool CzyZwrocona { get; set; }

        // Obiekt książki powiązaną z wypożyczeniem
        public Ksiazka Ksiazka { get; set; }

        // Obiekt czytelnika powiązanego z wypożyczeniem
        public IdentityUser Czytelnik { get; set; }
        
    }
}