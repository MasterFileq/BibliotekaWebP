using Microsoft.AspNetCore.Identity;

namespace BibliotekaWeb.Models
{
    public class Wypozyczenie
    {
        public int Id { get; set; }
        public int KsiazkaId { get; set; }
        public string CzytelnikId { get; set; }
        public DateTime DataWypozyczenia { get; set; }
        public DateTime TerminZwrotu { get; set; }
        public decimal Kara { get; set; }
        public int LiczbaPrzedluzen { get; set; }
        public bool CzyZwrocona { get; set; }
        public Ksiazka Ksiazka { get; set; }
        public IdentityUser Czytelnik { get; set; }
    }
}