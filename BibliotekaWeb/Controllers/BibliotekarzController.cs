using BibliotekaWeb.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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

            return View(wypozyczenia);
        }
    }
}
