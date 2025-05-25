using System.ComponentModel.DataAnnotations;

namespace BibliotekaWeb.Models
{
    // Enum do reprezentacji tematyk książek
    public enum Tematyka
    {
        Ekonomia,
        Matematyka,
        Geografia,
        [Display(Name = "Literatura piękna")]
        LiteraturaPiekna,
        Fantastyka,
        Kryminał,
        Horror,
        Romans,
        [Display(Name = "Literatura młodzieżowa")]
        LiteraturaMlodziezowa,
        [Display(Name = "Literatura dziecięca")]
        LiteraturaDziecieca,
        [Display(Name = "Literatura faktu")]
        LiteraturaFaktu,
        Poradniki,
        [Display(Name = "Religia i duchowość")]
        ReligiaIDuchowosc,
        [Display(Name = "Nauka i technika")]
        NaukaITechnika,
        [Display(Name = "Historia i polityka")]
        HistoriaIPolityka,
        [Display(Name = "Komiks i powieść graficzna")]
        KomiksIPowiescGraficzna,
        Poezja,
        Dramat,
        Antologie,
        [Display(Name = "Satyra i humor")]
        SatyraIHumor,
        [Display(Name = "Literatura podróżnicza")]
        LiteraturaPodroznicza,
        [Display(Name = "Literatura militarna")]
        LiteraturaMilitarna,
        [Display(Name = "Literatura regionalna")]
        LiteraturaRegionalna,
        Inne
    }

    public class Ksiazka
    {
        // Id książki
        public int Id { get; set; }

        // Tytuł książki (wymagany)
        [Required(ErrorMessage = "Tytuł jest wymagany.")]
        [Display(Name = "Tytuł")]
        public string Tytul { get; set; }

        // Autor książki (wymagany)
        [Required(ErrorMessage = "Autor jest wymagany.")]
        [Display(Name = "Autor")]
        public string Autor { get; set; }

        // Numer ISBN książki (wymagane)
        [Required(ErrorMessage = "ISBN jest wymagany.")]
        [StringLength(13, MinimumLength = 10, ErrorMessage = "ISBN musi mieć od 10 do 13 znaków.")]
        [Display(Name = "ISBN")]
        public string ISBN { get; set; }

        // Ilość egzemplarzy książki (wymagana)
        [Required(ErrorMessage = "Ilość egzemplarzy jest wymagana.")]
        [Range(1, int.MaxValue, ErrorMessage = "Ilość egzemplarzy musi być większa od 0.")]
        [Display(Name = "Ilość egzemplarzy")]
        public int IloscEgzemplarzy { get; set; }

        // Całkowita ilość dostępnych egzemplarzy książki (wymagana)
        [Display(Name = "Dostępne egzemplarze")]
        public int DostepneEgzemplarze { get; set; }

        // Tematyka książki (wymagana)
        [Required(ErrorMessage = "Tematyka jest wymagana.")]
        [Display(Name = "Tematyka")]
        public Tematyka Tematyka { get; set; }
    }
}