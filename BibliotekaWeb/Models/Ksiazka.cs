using System.ComponentModel.DataAnnotations;

namespace BibliotekaWeb.Models
{
    public enum Tematyka
    {
        Ekonomia,
        Matematyka,
        Geografia,
        Inne
    }

    public class Ksiazka
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tytuł jest wymagany.")]
        [Display(Name = "Tytuł")]
        public string Tytul { get; set; }

        [Required(ErrorMessage = "Autor jest wymagany.")]
        [Display(Name = "Autor")]
        public string Autor { get; set; }

        [Required(ErrorMessage = "ISBN jest wymagany.")]
        [StringLength(13, MinimumLength = 10, ErrorMessage = "ISBN musi mieć od 10 do 13 znaków.")]
        [Display(Name = "ISBN")]
        public string ISBN { get; set; }

        [Required(ErrorMessage = "Ilość egzemplarzy jest wymagana.")]
        [Range(1, int.MaxValue, ErrorMessage = "Ilość egzemplarzy musi być większa od 0.")]
        [Display(Name = "Ilość egzemplarzy")]
        public int IloscEgzemplarzy { get; set; }

        [Display(Name = "Dostępne egzemplarze")]
        public int DostepneEgzemplarze { get; set; } // Nowe pole

        [Required(ErrorMessage = "Tematyka jest wymagana.")]
        [Display(Name = "Tematyka")]
        public Tematyka Tematyka { get; set; }
    }
}