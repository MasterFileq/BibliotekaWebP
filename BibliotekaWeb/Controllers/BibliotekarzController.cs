using BibliotekaWeb.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace BibliotekaWeb.Controllers
{
    [Authorize(Roles = "Administrator, Bibliotekarz")]
    public class BibliotekarzController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BibliotekarzController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var wypozyczenia = await _context.Wypozyczenie
                .Include(w => w.Ksiazka)
                .Include(w => w.Czytelnik)
                .ToListAsync();

            foreach (var wypozyczenie in wypozyczenia)
            {
                var overdueDays = (DateTime.Now - wypozyczenie.TerminZwrotu).Days;
                if (overdueDays > 0)
                {
                    wypozyczenie.Kara = overdueDays * 5;
                }
                else
                {
                    wypozyczenie.Kara = 0;
                }
            }
            await _context.SaveChangesAsync();

            var wypozyczeniaPoMiesiacach = wypozyczenia
                .GroupBy(w => new { w.DataWypozyczenia.Year, w.DataWypozyczenia.Month })
                .Select(g => new
                {
                    Miesiac = $"{g.Key.Year}-{g.Key.Month:D2}",
                    LiczbaWypozyczen = g.Count()
                })
                .OrderBy(g => g.Miesiac)
                .ToList();

            ViewBag.WypozyczeniaPoMiesiacach = wypozyczeniaPoMiesiacach;

            return View(wypozyczenia);
        }
    }
}