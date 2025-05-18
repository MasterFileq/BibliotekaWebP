using BibliotekaWeb.Data;
using BibliotekaWeb.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace BibliotekaWeb.Controllers
{
    [Authorize(Roles = "Administrator, Bibliotekarz")]
    public class BibliotekarzController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<BibliotekarzController> _logger;

        public BibliotekarzController(ApplicationDbContext context, ILogger<BibliotekarzController> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IActionResult> Index(string searchTitle, string searchEmail, DateTime? startDate, DateTime? endDate)
        {
            var wypozyczenia = _context.Wypozyczenie
                .Include(w => w.Ksiazka)
                .Include(w => w.Czytelnik)
                .Where(w => !w.CzyZwrocona) 
                .AsQueryable();

            // Filtrowanie
            if (!string.IsNullOrEmpty(searchTitle))
            {
                wypozyczenia = wypozyczenia.Where(w => w.Ksiazka.Tytul.Contains(searchTitle));
                ViewData["searchTitle"] = searchTitle;
            }

            if (!string.IsNullOrEmpty(searchEmail))
            {
                wypozyczenia = wypozyczenia.Where(w => w.Czytelnik.Email.Contains(searchEmail));
                ViewData["searchEmail"] = searchEmail;
            }

            if (startDate.HasValue)
            {
                wypozyczenia = wypozyczenia.Where(w => w.DataWypozyczenia >= startDate.Value);
                ViewData["startDate"] = startDate.Value.ToString("yyyy-MM-dd");
            }

            if (endDate.HasValue)
            {
                wypozyczenia = wypozyczenia.Where(w => w.DataWypozyczenia <= endDate.Value);
                ViewData["endDate"] = endDate.Value.ToString("yyyy-MM-dd");
            }

            // Dane dla wykresu bieżących wypożyczeń (tylko aktywne, CzyZwrocona = false)
            var biezaceWypozyczeniaRaw = await _context.Wypozyczenie
                .Where(w => !w.CzyZwrocona)
                .GroupBy(w => new { w.DataWypozyczenia.Year, w.DataWypozyczenia.Month })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    LiczbaWypozyczen = g.Count()
                })
                .OrderBy(g => g.Year)
                .ThenBy(g => g.Month)
                .ToListAsync();

            var biezaceWypozyczenia = biezaceWypozyczeniaRaw
                .Select(g => new
                {
                    Miesiac = $"{g.Year}-{g.Month:00}",
                    g.LiczbaWypozyczen
                })
                .ToList();

            // Logowanie danych bieżących wypożyczeń
            _logger.LogInformation("Bieżące wypożyczenia: {Data}", System.Text.Json.JsonSerializer.Serialize(biezaceWypozyczenia));
            ViewBag.BiezaceWypozyczenia = biezaceWypozyczenia;

            // Dane dla wykresu wypożyczeń z ostatnich 30 dni (aktywne i zwrócone)
            var data30DniTemu = DateTime.Now.AddDays(-30);
            var wypozyczeniaOstatnie30DniRaw = await _context.Wypozyczenie
                .Where(w => w.DataWypozyczenia >= data30DniTemu)
                .GroupBy(w => new { w.DataWypozyczenia.Year, w.DataWypozyczenia.Month })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    LiczbaWypozyczen = g.Count(),
                    LiczbaAktywnych = g.Count(w => !w.CzyZwrocona),
                    LiczbaZwroconych = g.Count(w => w.CzyZwrocona)
                })
                .OrderBy(g => g.Year)
                .ThenBy(g => g.Month)
                .ToListAsync();

            var wypozyczeniaOstatnie30Dni = wypozyczeniaOstatnie30DniRaw
                .Select(g => new
                {
                    Miesiac = $"{g.Year}-{g.Month:00}",
                    g.LiczbaWypozyczen,
                    g.LiczbaAktywnych,
                    g.LiczbaZwroconych
                })
                .ToList();

            // Logowanie danych ostatnich 30 dni
            _logger.LogInformation("Wypożyczenia ostatnie 30 dni: {Data}", System.Text.Json.JsonSerializer.Serialize(wypozyczeniaOstatnie30Dni));
            ViewBag.WypozyczeniaOstatnie30Dni = wypozyczeniaOstatnie30Dni;

            return View(await wypozyczenia.ToListAsync());
        }
    }
}