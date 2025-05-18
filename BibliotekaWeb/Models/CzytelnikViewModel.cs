using Microsoft.AspNetCore.Identity;

namespace BibliotekaWeb.Models
{
    public class CzytelnikViewModel
    {
        public string? UserId { get; set; }
        public virtual IdentityUser? User { get; set; }
        public string? KsiazkaTytul { get; set; }
        public DateTime? TerminZwrotu { get; set; }
        public int IloscWypozyczonychKsiazek { get; set; }
    }
}