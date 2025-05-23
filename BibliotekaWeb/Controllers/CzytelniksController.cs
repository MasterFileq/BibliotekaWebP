﻿using BibliotekaWeb.Data;
using BibliotekaWeb.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace BibliotekaWeb.Controllers
{
    // Kontroler do zarządzania czytelnikami
    [Authorize]
    public class CzytelniksController : Controller
    {
        // Kontekst bazy danych, menedżer użytkowników, menedżer ról i logger
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogger<CzytelniksController> _logger;

        // Konstruktor z kontekstem bazy danych, menedż
        public CzytelniksController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ILogger<CzytelniksController> logger)
        {
            // Weryfikacja czy któryś z obiektów jest null
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _roleManager = roleManager ?? throw new ArgumentNullException(nameof(roleManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        // Metoda do wyświetlania listy czytelników z opcjami filtrowania
        [Authorize(Roles = "Administrator, Bibliotekarz")]
        public async Task<IActionResult> Index(string searchUserName, string searchEmail, int? minWypozyczenia, int? maxWypozyczenia)
        {
            // Pobieranie wszystkich czytelników z roli "Czytelnik"
            var czytelnicy = await _userManager.GetUsersInRoleAsync("Czytelnik");
            var czytelnikViewModels = new List<CzytelnikViewModel>();

            // Pobieranie wypożyczeń dla czytelników
            var wypozyczenia = await _context.Wypozyczenie
                .Where(w => czytelnicy.Select(u => u.Id).Contains(w.CzytelnikId))
                .GroupBy(w => w.CzytelnikId)
                .Select(g => new
                {
                    CzytelnikId = g.Key,
                    // Liczba aktywnych wypożyczeń (niezwroconych)
                    IloscAktywnych = g.Count(w => !w.CzyZwrocona),
                    // Suma kar dla wypożyczeń
                    SumaKar = g.Sum(w => w.Kara)
                })
                .ToDictionaryAsync(k => k.CzytelnikId, v => new { v.IloscAktywnych, v.SumaKar });

            // Filtrowanie czytelników na podstawie podanych kryteriów
            var filteredCzytelnicy = czytelnicy.AsQueryable();
            if (!string.IsNullOrEmpty(searchUserName))
            {
                filteredCzytelnicy = filteredCzytelnicy.Where(u => u.UserName.Contains(searchUserName, StringComparison.OrdinalIgnoreCase));
                ViewData["searchUserName"] = searchUserName;
            }

            if (!string.IsNullOrEmpty(searchEmail))
            {
                filteredCzytelnicy = filteredCzytelnicy.Where(u => u.Email.Contains(searchEmail, StringComparison.OrdinalIgnoreCase));
                ViewData["searchEmail"] = searchEmail;
            }

            // Filtrowanie po liczbie wypożyczeń
            foreach (var user in filteredCzytelnicy)
            {
                var daneWypozyczenia = wypozyczenia.ContainsKey(user.Id) ? wypozyczenia[user.Id] : new { IloscAktywnych = 0, SumaKar = 0m };

                if (minWypozyczenia.HasValue && daneWypozyczenia.IloscAktywnych < minWypozyczenia.Value)
                    continue;
                if (maxWypozyczenia.HasValue && daneWypozyczenia.IloscAktywnych > maxWypozyczenia.Value)
                    continue;
                // Dodanie czytelnika do listy widoków
                czytelnikViewModels.Add(new CzytelnikViewModel
                {
                    UserId = user.Id,
                    User = user,
                    IloscWypozyczonychKsiazek = daneWypozyczenia.IloscAktywnych,
                    SumaKar = daneWypozyczenia.SumaKar
                });
            }

            // Ustawienie filtrów w ViewData do ponownego użycia w widoku
            if (minWypozyczenia.HasValue)
                ViewData["minWypozyczenia"] = minWypozyczenia.Value.ToString();
            if (maxWypozyczenia.HasValue)
                ViewData["maxWypozyczenia"] = maxWypozyczenia.Value.ToString();
            // Logowanie informacji o załadowanych czytelnikach
            _logger.LogInformation("Załadowano {Count} czytelników z filtrami: UserName={UserName}, Email={Email}, MinWypozyczenia={Min}, MaxWypozyczenia={Max}",
                czytelnikViewModels.Count, searchUserName, searchEmail, minWypozyczenia, maxWypozyczenia);

            return View(czytelnikViewModels);
        }
        // Metoda do wyświetlania formularza dodawania nowego czytelnika (tylko dla administratora)
        [Authorize(Roles = "Administrator")]
        public IActionResult Create()
        {
            return View();
        }

        // Akcja POST - dodawanie nowego czytelnika
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Create(RegisterViewModel model)
        {
            // Sprawdzenie czy dane w formularzu są poprawne
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Nieprawidłowe dane formularza.";
                return View(model);
            }

            // Tworzenie nowego użytkownika
            var user = new IdentityUser { UserName = model.Email, Email = model.Email };
            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                // Sprawdzenie czy rola "Czytelnik" istnieje, jeśli nie to ją tworzymy
                await EnsureCzytelnikRoleExistsAsync();
                await _userManager.AddToRoleAsync(user, "Czytelnik");

                TempData["SuccessMessage"] = "Nowy czytelnik został dodany.";
                return RedirectToAction(nameof(Index));
            }
            // Jeżeli wystąpiły błędy podczas tworzenia użytkownika, dodajemy je do ModelState
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            TempData["ErrorMessage"] = "Nie udało się utworzyć nowego czytelnika.";
            return View(model);
        }

        // Metoda do wyświetlania szczegółów czytelnika (tylko dla administratora)
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Details(string id)
        {
            // Sprawdzenie czy identyfikator czytelnika jest poprawny
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            // Pobieranie czytelnika z bazy danych na podstawie identyfikatora
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // Metoda do edytowania danych czytelnika (tylko dla administratora)
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            // Pobieranie czytelnika z bazy danych na podstawie identyfikatora
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // Akcja POST - edytowanie danych czytelnika
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Edit(string id, IdentityUser model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Sprawdzenie czy identyfikator czytelnika jest poprawny
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }
            // Sprawdzenie czy dane w formularzu są poprawne (nazwam, email)
            if (string.IsNullOrWhiteSpace(model.UserName) || string.IsNullOrWhiteSpace(model.Email))
            {
                ModelState.AddModelError(string.Empty, "Nazwa użytkownika i email nie mogą być puste.");
                return View(model);
            }

            // Aktualizacja danych czytelnika
            user.UserName = model.UserName;
            user.Email = model.Email;

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Dane czytelnika zostały zaktualizowane.";
                return RedirectToAction(nameof(Index));
            }

            // Jeżeli wystąpiły błędy podczas aktualizacji użytkownika, dodajemy je do ModelState
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        // Metoda do usuwania czytelnika (tylko dla administratora)
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                TempData["ErrorMessage"] = "Nieprawidłowy identyfikator czytelnika.";
                return RedirectToAction(nameof(Index));
            }
            // Pobieranie czytelnika z bazy danych na podstawie identyfikatora
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Nie znaleziono czytelnika.";
                return RedirectToAction(nameof(Index));
            }

            return View(user);
        }

        // Akcja POST - potwierdzenie usunięcia czytelnika
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                TempData["ErrorMessage"] = "Nieprawidłowy identyfikator czytelnika.";
                return RedirectToAction(nameof(Index));
            }

            // Pobieranie czytelnika z bazy danych na podstawie identyfikatora
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Nie znaleziono czytelnika.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                // Pobieranie wypożyczeń czytelnika i aktualizacja dostępnych egzemplarzy książek
                var wypozyczenia = await _context.Wypozyczenie
                    .Where(w => w.CzytelnikId == id && !w.CzyZwrocona)
                    .Include(w => w.Ksiazka)
                    .ToListAsync();

                // Sprawdzenie czy są jakieś wypożyczenia do zwrotu
                foreach (var wypozyczenie in wypozyczenia)
                {
                    if (wypozyczenie.Ksiazka != null)
                    {
                        wypozyczenie.Ksiazka.DostepneEgzemplarze++;
                        wypozyczenie.CzyZwrocona = true;
                        _context.Update(wypozyczenie.Ksiazka);
                        _context.Update(wypozyczenie);
                    }
                }

                // Zapisanie zmian w bazie danych
                await _context.SaveChangesAsync();

                // Usunięcie czytelnika z bazy danych przy pomocy Identity
                var result = await _userManager.DeleteAsync(user);
                if (!result.Succeeded)
                {
                    var errors = result.Errors.Select(e => e.Description).ToList();
                    _logger.LogError("Błędy Identity przy usuwaniu użytkownika {UserId}: {Errors}", id, string.Join("; ", errors));
                    TempData["ErrorMessage"] = $"Błąd podczas usuwania czytelnika: {string.Join("; ", errors)}";
                    await transaction.RollbackAsync();
                    return RedirectToAction(nameof(Index));
                }

                await transaction.CommitAsync();

                TempData["SuccessMessage"] = "Czytelnik został pomyślnie usunięty.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas usuwania czytelnika {UserId}", id);
                TempData["ErrorMessage"] = "Wystąpił błąd podczas usuwania czytelnika. Spróbuj ponownie.";
                return RedirectToAction(nameof(Index));
            }
        }

        // Metoda do przypisywania książki do czytelnika (tylko dla administratora i bibliotekarza)
        [Authorize(Roles = "Administrator, Bibliotekarz")]
        public async Task<IActionResult> AssignBook(string czytelnikId, string searchTitle, string searchAuthor, string searchISBN, Tematyka? tematyka)
        {
            // Pobieranie wszystkich książek z bazy danych, które są dostępne
            var ksiazki = _context.Ksiazka
                .Where(k => k.DostepneEgzemplarze > 0)
                .AsQueryable();

            // Filtrowanie książek na podstawie podanych kryteriów
            if (!string.IsNullOrEmpty(searchTitle))
            {
                ksiazki = ksiazki.Where(k => k.Tytul.Contains(searchTitle));
                ViewData["searchTitle"] = searchTitle;
            }

            if (!string.IsNullOrEmpty(searchAuthor))
            {
                ksiazki = ksiazki.Where(k => k.Autor.Contains(searchAuthor));
                ViewData["searchAuthor"] = searchAuthor;
            }

            if (!string.IsNullOrEmpty(searchISBN))
            {
                ksiazki = ksiazki.Where(k => k.ISBN.Contains(searchISBN));
                ViewData["searchISBN"] = searchISBN;
            }

            if (tematyka.HasValue)
            {
                ksiazki = ksiazki.Where(k => k.Tematyka == tematyka.Value);
                ViewData["tematyka"] = tematyka.ToString();
            }

            // Sprawdzenie czy identyfikator czytelnika jest poprawny
            var czytelnik = await _userManager.FindByIdAsync(czytelnikId);
            if (czytelnik == null)
            {
                TempData["ErrorMessage"] = "Nie znaleziono czytelnika.";
                return RedirectToAction(nameof(Index));
            }

            // Przygotowanie modelu widoku do przypisania książki
            var model = new WypozyczenieViewModel
            {
                CzytelnikId = czytelnikId,
                Email = czytelnik.Email,
                Ksiazki = await ksiazki.ToListAsync(),
                Tematyki = Enum.GetValues(typeof(Tematyka)).Cast<Tematyka>().ToList()
            };

            _logger.LogInformation("Załadowano {Count} książek dla czytelnika {CzytelnikId} z filtrami: Tytuł={Title}, Autor={Author}, ISBN={ISBN}, Tematyka={Tematyka}",
                model.Ksiazki.Count, czytelnikId, searchTitle, searchAuthor, searchISBN, tematyka?.ToString());
            return View(model);
        }

        // Akcja POST - przypisywanie książki do czytelnika
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator, Bibliotekarz")]
        public async Task<IActionResult> AssignBook(WypozyczenieViewModel model)
        {
            // Usunięcie niepotrzebnych danych z ModelState, których nie walidujemy
            ModelState.Remove("Ksiazki");
            ModelState.Remove("Tematyki");

            if (ModelState.IsValid)
            {
                // Sprawdzenie czy identyfikator czytelnika jest poprawny
                var czytelnik = await _userManager.FindByIdAsync(model.CzytelnikId);
                if (czytelnik == null)
                {
                    TempData["ErrorMessage"] = "Nie znaleziono czytelnika z podanym identyfikatorem.";
                    model.Ksiazki = await _context.Ksiazka.Where(k => k.DostepneEgzemplarze > 0).ToListAsync();
                    model.Tematyki = Enum.GetValues(typeof(Tematyka)).Cast<Tematyka>().ToList();
                    return View(model);
                }

                // Sprawdzenie czy książka jest już wypożyczona przez tego czytelnika
                var existingWypozyczenie = await _context.Wypozyczenie
                    .AnyAsync(w => w.CzytelnikId == model.CzytelnikId && w.KsiazkaId == model.KsiazkaId && !w.CzyZwrocona);
                if (existingWypozyczenie)
                {
                    TempData["ErrorMessage"] = "Czytelnik ma już wypożyczoną tę książkę.";
                    model.Ksiazki = await _context.Ksiazka.Where(k => k.DostepneEgzemplarze > 0).ToListAsync();
                    model.Tematyki = Enum.GetValues(typeof(Tematyka)).Cast<Tematyka>().ToList();
                    return View(model);
                }

                // Sprawdzenie czy książka jest dostępna
                var ksiazka = await _context.Ksiazka.FindAsync(model.KsiazkaId);
                if (ksiazka == null || ksiazka.DostepneEgzemplarze <= 0)
                {
                    TempData["ErrorMessage"] = "Wybrana książka jest niedostępna.";
                    model.Ksiazki = await _context.Ksiazka.Where(k => k.DostepneEgzemplarze > 0).ToListAsync();
                    model.Tematyki = Enum.GetValues(typeof(Tematyka)).Cast<Tematyka>().ToList();
                    return View(model);
                }

                // Tworzenie nowego wypożyczenia
                var wypozyczenie = new Wypozyczenie
                {
                    KsiazkaId = model.KsiazkaId,
                    CzytelnikId = model.CzytelnikId,
                    DataWypozyczenia = DateTime.Now,
                    TerminZwrotu = DateTime.Now.AddDays(14),
                    Kara = 0,
                    LiczbaPrzedluzen = 0,
                    CzyZwrocona = false
                };

                // Zmniejszenie liczby dostępnych egzemplarzy książki
                ksiazka.DostepneEgzemplarze--;

                try
                {
                    using var transaction = await _context.Database.BeginTransactionAsync();
                    _context.Wypozyczenie.Add(wypozyczenie);
                    _context.Update(ksiazka);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    TempData["SuccessMessage"] = "Książka została przypisana czytelnikowi.";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateException ex)
                {
                    // Logowanie błędu i wyświetlenie komunikatu o błędzie
                    _logger.LogError(ex, "Błąd podczas zapisywania wypożyczenia dla czytelnika {CzytelnikId}, książki {KsiazkaId}", model.CzytelnikId, model.KsiazkaId);
                    TempData["ErrorMessage"] = "Wystąpił błąd podczas przypisywania książki. Spróbuj ponownie.";
                    model.Ksiazki = await _context.Ksiazka.Where(k => k.DostepneEgzemplarze > 0).ToListAsync();
                    model.Tematyki = Enum.GetValues(typeof(Tematyka)).Cast<Tematyka>().ToList();
                    return View(model);
                }
            }

            // Jeżeli wystąpiły błędy walidacji, dodajemy je do ModelState i wyświetlamy formularz ponownie
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            _logger.LogWarning("Błędy walidacji przy przypisywaniu książki: {Errors}", string.Join("; ", errors));
            TempData["ErrorMessage"] = string.Join("; ", errors) + " Proszę upewnić się, że wybrano książkę.";
            model.Ksiazki = await _context.Ksiazka.Where(k => k.DostepneEgzemplarze > 0).ToListAsync();
            model.Tematyki = Enum.GetValues(typeof(Tematyka)).Cast<Tematyka>().ToList();
            return View(model);
        }

        // Metoda do sprawdzenia czy rola "Czytelnik" istnieje, jeśli nie to ją tworzymy
        private async Task EnsureCzytelnikRoleExistsAsync()
        {
            if (!await _roleManager.RoleExistsAsync("Czytelnik"))
            {
                await _roleManager.CreateAsync(new IdentityRole("Czytelnik"));
            }
        }
    }
}