using BibliotekaWeb.Data;
using BibliotekaWeb.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace BibliotekaWeb.Controllers
{
    [Authorize]
    public class KsiazkasController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public KsiazkasController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        }

        [Authorize(Roles = "Administrator, Bibliotekarz, Czytelnik")]
        public async Task<IActionResult> Katalog(string searchString, Tematyka? tematyka, string author, string isbn, bool? available)
        {
            var books = from b in _context.Ksiazka
                        select b;

            if (!string.IsNullOrEmpty(searchString))
            {
                books = books.Where(b => b.Tytul.Contains(searchString));
                ViewData["CurrentFilter"] = searchString;
            }

            if (tematyka.HasValue)
            {
                books = books.Where(b => b.Tematyka == tematyka.Value);
                ViewData["CurrentTematyka"] = tematyka.ToString();
            }

            if (!string.IsNullOrEmpty(author))
            {
                books = books.Where(b => b.Autor.Contains(author));
                ViewData["CurrentAuthor"] = author;
            }

            if (!string.IsNullOrEmpty(isbn))
            {
                books = books.Where(b => b.ISBN.Contains(isbn));
                ViewData["CurrentISBN"] = isbn;
            }

            if (available.HasValue)
            {
                books = available.Value
                    ? books.Where(b => b.DostepneEgzemplarze > 0)
                    : books.Where(b => b.DostepneEgzemplarze == 0);
                ViewData["CurrentAvailable"] = available.ToString();
            }

            ViewBag.Tematyki = Enum.GetValues(typeof(Tematyka)).Cast<Tematyka>().ToList();
            return View(await books.ToListAsync());
        }

        [Authorize(Roles = "Czytelnik")]
        public async Task<IActionResult> Wypozyczone()
        {
            var user = await _userManager.FindByNameAsync(User.Identity.Name);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Nie udało się znaleźć zalogowanego użytkownika.";
                return RedirectToAction(nameof(Katalog));
            }

            var wypozyczenia = await _context.Wypozyczenie
                .Include(w => w.Ksiazka)
                .Where(w => w.CzytelnikId == user.Id && !w.CzyZwrocona) // Pokazuj tylko aktywne wypożyczenia
                .ToListAsync();

            return View(wypozyczenia);
        }

        [Authorize(Roles = "Czytelnik")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Przedluz(int id)
        {
            var user = await _userManager.FindByNameAsync(User.Identity.Name);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Nie udało się znaleźć zalogowanego użytkownika.";
                return RedirectToAction(nameof(Wypozyczone));
            }

            var wypozyczenie = await _context.Wypozyczenie
                .FirstOrDefaultAsync(w => w.Id == id && w.CzytelnikId == user.Id && !w.CzyZwrocona);

            if (wypozyczenie == null)
            {
                TempData["ErrorMessage"] = "Nie znaleziono aktywnego wypożyczenia.";
                return RedirectToAction(nameof(Wypozyczone));
            }

            if (wypozyczenie.LiczbaPrzedluzen >= 1)
            {
                TempData["ErrorMessage"] = "Możesz przedłużyć wypożyczenie tylko raz.";
                return RedirectToAction(nameof(Wypozyczone));
            }

            wypozyczenie.TerminZwrotu = wypozyczenie.TerminZwrotu.AddDays(30);
            wypozyczenie.LiczbaPrzedluzen++;

            try
            {
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Wypożyczenie zostało przedłużone o 30 dni.";
            }
            catch (DbUpdateException ex)
            {
                TempData["ErrorMessage"] = $"Wystąpił błąd podczas przedłużania wypożyczenia: {ex.Message}";
                return RedirectToAction(nameof(Wypozyczone));
            }

            return RedirectToAction(nameof(Wypozyczone));
        }

        [Authorize(Roles = "Administrator, Bibliotekarz")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PrzedluzByAdmin(int id)
        {
            var wypozyczenie = await _context.Wypozyczenie
                .FirstOrDefaultAsync(w => w.Id == id && !w.CzyZwrocona);

            if (wypozyczenie == null)
            {
                TempData["ErrorMessage"] = "Nie znaleziono aktywnego wypożyczenia.";
                return RedirectToAction("Index", "Bibliotekarz");
            }

            wypozyczenie.TerminZwrotu = wypozyczenie.TerminZwrotu.AddDays(30);
            wypozyczenie.LiczbaPrzedluzen++;

            try
            {
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Wypożyczenie zostało przedłużone o 30 dni.";
            }
            catch (DbUpdateException ex)
            {
                TempData["ErrorMessage"] = $"Wystąpił błąd podczas przedłużania wypożyczenia: {ex.Message}";
                return RedirectToAction("Index", "Bibliotekarz");
            }

            return RedirectToAction("Index", "Bibliotekarz");
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var ksiazka = await _context.Ksiazka
                .FirstOrDefaultAsync(m => m.Id == id);
            if (ksiazka == null)
            {
                return NotFound();
            }

            return View(ksiazka);
        }

        [Authorize(Roles = "Administrator, Bibliotekarz")]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator, Bibliotekarz")]
        public async Task<IActionResult> Create([Bind("Id,Tytul,Autor,ISBN,IloscEgzemplarzy,Tematyka")] Ksiazka ksiazka)
        {
            if (await _context.Ksiazka.AnyAsync(k => k.ISBN == ksiazka.ISBN))
            {
                ModelState.AddModelError("ISBN", "Książka z podanym ISBN już istnieje.");
            }

            if (ModelState.IsValid)
            {
                ksiazka.DostepneEgzemplarze = ksiazka.IloscEgzemplarzy;
                _context.Add(ksiazka);
                try
                {
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Książka została pomyślnie dodana.";
                    return RedirectToAction(nameof(Katalog));
                }
                catch (DbUpdateException ex)
                {
                    TempData["ErrorMessage"] = $"Wystąpił błąd podczas dodawania książki: {ex.Message}";
                    return View(ksiazka);
                }
            }

            return View(ksiazka);
        }

        [Authorize(Roles = "Administrator, Bibliotekarz")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var ksiazka = await _context.Ksiazka.FindAsync(id);
            if (ksiazka == null)
            {
                return NotFound();
            }
            return View(ksiazka);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator, Bibliotekarz")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Tytul,Autor,ISBN,IloscEgzemplarzy,Tematyka")] Ksiazka ksiazka)
        {
            if (id != ksiazka.Id)
            {
                return NotFound();
            }

            if (await _context.Ksiazka.AnyAsync(k => k.ISBN == ksiazka.ISBN && k.Id != ksiazka.Id))
            {
                ModelState.AddModelError("ISBN", "Książka z podanym ISBN już istnieje.");
            }

            var existingKsiazka = await _context.Ksiazka.AsNoTracking().FirstOrDefaultAsync(k => k.Id == id);
            if (existingKsiazka == null)
            {
                return NotFound();
            }

            int wypozyczoneEgzemplarze = existingKsiazka.IloscEgzemplarzy - existingKsiazka.DostepneEgzemplarze;
            if (ksiazka.IloscEgzemplarzy < wypozyczoneEgzemplarze)
            {
                ModelState.AddModelError("IloscEgzemplarzy", $"Liczba egzemplarzy nie może być mniejsza niż liczba aktualnie wypożyczonych ({wypozyczoneEgzemplarze}).");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    existingKsiazka.Tytul = ksiazka.Tytul;
                    existingKsiazka.Autor = ksiazka.Autor;
                    existingKsiazka.ISBN = ksiazka.ISBN;
                    existingKsiazka.Tematyka = ksiazka.Tematyka;
                    int roznicaEgzemplarzy = ksiazka.IloscEgzemplarzy - existingKsiazka.IloscEgzemplarzy;
                    existingKsiazka.IloscEgzemplarzy = ksiazka.IloscEgzemplarzy;
                    existingKsiazka.DostepneEgzemplarze += roznicaEgzemplarzy;

                    _context.Update(existingKsiazka);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Książka została pomyślnie zaktualizowana.";
                    return RedirectToAction(nameof(Katalog));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!KsiazkaExists(ksiazka.Id))
                    {
                        return NotFound();
                    }
                    throw;
                }
                catch (DbUpdateException ex)
                {
                    TempData["ErrorMessage"] = $"Wystąpił błąd podczas aktualizacji książki: {ex.Message}";
                    return View(ksiazka);
                }
            }
            return View(ksiazka);
        }

        [Authorize(Roles = "Administrator, Bibliotekarz")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var ksiazka = await _context.Ksiazka
                .FirstOrDefaultAsync(m => m.Id == id);
            if (ksiazka == null)
            {
                return NotFound();
            }

            var activeWypozyczenia = await _context.Wypozyczenie
                .AnyAsync(w => w.KsiazkaId == id && !w.CzyZwrocona);
            if (activeWypozyczenia)
            {
                TempData["ErrorMessage"] = "Nie można usunąć książki, która jest aktualnie wypożyczona.";
                return RedirectToAction(nameof(Katalog));
            }

            return View(ksiazka);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator, Bibliotekarz")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var ksiazka = await _context.Ksiazka.FindAsync(id);
            if (ksiazka == null)
            {
                TempData["ErrorMessage"] = "Nie znaleziono książki.";
                return RedirectToAction(nameof(Katalog));
            }

            var activeWypozyczenia = await _context.Wypozyczenie
                .AnyAsync(w => w.KsiazkaId == id && !w.CzyZwrocona);
            if (activeWypozyczenia)
            {
                TempData["ErrorMessage"] = "Nie można usunąć książki, która jest aktualnie wypożyczona.";
                return RedirectToAction(nameof(Katalog));
            }

            try
            {
                _context.Ksiazka.Remove(ksiazka);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Książka została pomyślnie usunięta.";
            }
            catch (DbUpdateException ex)
            {
                TempData["ErrorMessage"] = $"Wystąpił błąd podczas usuwania książki: {ex.Message}";
                return RedirectToAction(nameof(Katalog));
            }

            return RedirectToAction(nameof(Katalog));
        }

        private bool KsiazkaExists(int id)
        {
            return _context.Ksiazka.Any(e => e.Id == id);
        }

        [Authorize(Roles = "Administrator, Bibliotekarz")]
        public async Task<IActionResult> ZwrocByAdmin(int id)
        {
            var wypozyczenie = await _context.Wypozyczenie
                .FirstOrDefaultAsync(w => w.KsiazkaId == id && !w.CzyZwrocona);

            if (wypozyczenie == null)
            {
                TempData["ErrorMessage"] = "Nie znaleziono aktywnego wypożyczenia dla tej książki.";
                return RedirectToAction("Index", "Bibliotekarz");
            }

            var ksiazka = await _context.Ksiazka.FindAsync(id);
            if (ksiazka == null)
            {
                TempData["ErrorMessage"] = "Nie znaleziono książki.";
                return RedirectToAction("Index", "Bibliotekarz");
            }

            wypozyczenie.CzyZwrocona = true;
            ksiazka.DostepneEgzemplarze++;

            try
            {
                _context.Update(wypozyczenie);
                _context.Update(ksiazka);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Książka została pomyślnie zwrócona.";
            }
            catch (DbUpdateException ex)
            {
                TempData["ErrorMessage"] = $"Wystąpił błąd podczas zwracania książki: {ex.Message}";
                return RedirectToAction("Index", "Bibliotekarz");
            }

            return RedirectToAction("Index", "Bibliotekarz");
        }
    }
}