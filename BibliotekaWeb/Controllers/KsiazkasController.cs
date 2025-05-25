using BibliotekaWeb.Data;
using BibliotekaWeb.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Text.RegularExpressions; // Dodano dla Regex
using System.Threading.Tasks;

namespace BibliotekaWeb.Controllers
{
    // Każdy użytkownik musi być autoryzowany, aby uzyskać dostęp do tego kontrolera
    [Authorize]
    public class KsiazkasController : Controller
    {
        // Kontekst bazy danych i menedżer użytkowników
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        // Konstruktor z kontekstem bazy danych i menedżerem użytkowników
        public KsiazkasController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        }

        // Metoda pomocnicza do walidacji numeru ISBN
        private bool IsValidIsbn(string isbn)
        {
            if (string.IsNullOrWhiteSpace(isbn))
            {
                return false;
            }

            string cleanedIsbn = isbn.Replace("-", "").Replace(" ", "");

            if (cleanedIsbn.Length == 10)
            {
                return Regex.IsMatch(cleanedIsbn, @"^\d{9}[\dX]$", RegexOptions.IgnoreCase);
            }
            else if (cleanedIsbn.Length == 13)
            {
                return Regex.IsMatch(cleanedIsbn, @"^\d{13}$");
            }

            return false;
        }

        // Metoda do wyświetlania katalogu książek
        [Authorize(Roles = "Administrator, Bibliotekarz, Czytelnik")]
        public async Task<IActionResult> Katalog(string searchString, Tematyka? tematyka, string author, string isbn, bool? available)
        {
            // Pobieranie wszystkich książek z bazy danych
            var books = from b in _context.Ksiazka
                        select b;

            // Filtrowanie po tytule książki, tematyce, autorze, ISBN i dostępności
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

            // Przekazywanie dostępnych tematyk do widoku
            ViewBag.Tematyki = Enum.GetValues(typeof(Tematyka)).Cast<Tematyka>().ToList();

            // Widok listy książek
            return View(await books.ToListAsync());
        }

        // Metoda do wypożyczania książki (dla zalogowanego użytkownika)
        [Authorize(Roles = "Czytelnik")]
        public async Task<IActionResult> Wypozyczone()
        {
            // Pobieranie zalogowanego użytkownika na postawie jego nazwy użytkownika
            var user = await _userManager.FindByNameAsync(User.Identity.Name);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Nie udało się znaleźć zalogowanego użytkownika.";
                return RedirectToAction(nameof(Katalog));
            }

            // Pobieranie wypożyczeń zalogowanego użytkownika z bazy danych
            var wypozyczenia = await _context.Wypozyczenie
                .Include(w => w.Ksiazka)
                .Where(w => w.CzytelnikId == user.Id && !w.CzyZwrocona) // Pokazuj tylko aktywne wypożyczenia
                .ToListAsync();

            return View(wypozyczenia);
        }
        // Przedlużanie książki 
        [Authorize(Roles = "Czytelnik")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Przedluz(int id)
        {
            // Pobieranie aktualnie zalogowanego użytkownika
            var user = await _userManager.FindByNameAsync(User.Identity.Name);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Nie udało się znaleźć zalogowanego użytkownika.";
                return RedirectToAction(nameof(Wypozyczone));
            }
            // Znajdujemy aktywne wypożyczenie należące do użytkownika
            var wypozyczenie = await _context.Wypozyczenie
                .FirstOrDefaultAsync(w => w.Id == id && w.CzytelnikId == user.Id && !w.CzyZwrocona);

            if (wypozyczenie == null)
            {
                TempData["ErrorMessage"] = "Nie znaleziono aktywnego wypożyczenia.";
                return RedirectToAction(nameof(Wypozyczone));
            }

            // Sprawdzamy, czy wypożyczenie zostało już przedłużone
            if (wypozyczenie.LiczbaPrzedluzen >= 1)
            {
                TempData["ErrorMessage"] = "Możesz przedłużyć wypożyczenie tylko raz.";
                return RedirectToAction(nameof(Wypozyczone));
            }

            // Przedłużamy termin zwrotu o 30 dni
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

        // Akcja PrzedluzByAdmin - przedłużanie wypożyczenia przez administratora lub bibliotekarza
        [Authorize(Roles = "Administrator, Bibliotekarz")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PrzedluzByAdmin(int id)
        {
            // Pobieranie wypożyczenia na podstawie ID
            var wypozyczenie = await _context.Wypozyczenie
                .FirstOrDefaultAsync(w => w.Id == id && !w.CzyZwrocona);

            if (wypozyczenie == null)
            {
                TempData["ErrorMessage"] = "Nie znaleziono aktywnego wypożyczenia.";
                return RedirectToAction("Index", "Bibliotekarz");
            }

            //Przedłużamy termin zwrotu o 30 dni oraz zwiększamy liczbę przedłużeń
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

        // Metoda do wyświetlania szczegółów książki na podstawie ID
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

        // Wyświetlanie formularza dodawania nowej książki
        [Authorize(Roles = "Administrator, Bibliotekarz")]
        public IActionResult Create()
        {
            return View();
        }

        // Akcja POST - dodawanie nowej książki
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator, Bibliotekarz")]
        public async Task<IActionResult> Create([Bind("Id,Tytul,Autor,ISBN,IloscEgzemplarzy,Tematyka")] Ksiazka ksiazka)
        {
            // Sprawdzanie, czy ISBN jest unikalny
            if (!string.IsNullOrWhiteSpace(ksiazka.ISBN) && await _context.Ksiazka.AnyAsync(k => k.ISBN == ksiazka.ISBN))
            {
                ModelState.AddModelError("ISBN", "Książka z podanym ISBN już istnieje.");
            }

            // Walidacja formatu i sumy kontrolnej ISBN
            if (!string.IsNullOrWhiteSpace(ksiazka.ISBN) && !IsValidIsbn(ksiazka.ISBN))
            {
                ModelState.AddModelError("ISBN", "Podany numer ISBN jest nieprawidłowy (błędny format lub suma kontrolna).");
            }

            if (ModelState.IsValid)
            {
                // Ustawiamy liczbę dostępnych egzemplarzy na podstawie całkowitej liczby egzemplarzy
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
                    // Nie zwracamy widoku z modelem, jeśli błąd jest związany z bazą danych po walidacji,
                    // ale jeśli ModelState nie byłby Valid, to zwrócenie View(ksiazka) jest poprawne.
                    // Tutaj błąd wystąpił przy SaveChangesAsync, więc ModelState.IsValid było true.
                    // Można dodać logowanie błędu.
                    return View(ksiazka); // Zwróć widok z modelem, aby użytkownik mógł poprawić dane
                }
            }

            return View(ksiazka);
        }

        // Akcja do edytowania książki na podstawie ID
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

        // Akcja POST - aktualizacja książki
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator, Bibliotekarz")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Tytul,Autor,ISBN,IloscEgzemplarzy,Tematyka")] Ksiazka ksiazka)
        {
            if (id != ksiazka.Id)
            {
                return NotFound();
            }

            var bookFromDb = await _context.Ksiazka.AsNoTracking().FirstOrDefaultAsync(k => k.Id == id);
            if (bookFromDb == null)
            {
                return NotFound();
            }

            // Sprawdzanie, czy ISBN jest unikalny (jeśli został zmieniony)
            if (!string.IsNullOrWhiteSpace(ksiazka.ISBN) && bookFromDb.ISBN != ksiazka.ISBN)
            {
                if (await _context.Ksiazka.AnyAsync(k => k.ISBN == ksiazka.ISBN && k.Id != ksiazka.Id))
                {
                    ModelState.AddModelError("ISBN", "Książka z podanym ISBN już istnieje.");
                }
            }

            // Walidacja formatu i sumy kontrolnej ISBN
            if (!string.IsNullOrWhiteSpace(ksiazka.ISBN) && !IsValidIsbn(ksiazka.ISBN))
            {
                ModelState.AddModelError("ISBN", "Podany numer ISBN jest nieprawidłowy (błędny format lub suma kontrolna).");
            }

            // Sprawdzanie, czy liczba egzemplarzy jest mniejsza niż liczba aktualnie wypożyczonych
            int wypozyczoneEgzemplarze = bookFromDb.IloscEgzemplarzy - bookFromDb.DostepneEgzemplarze;
            if (ksiazka.IloscEgzemplarzy < wypozyczoneEgzemplarze)
            {
                ModelState.AddModelError("IloscEgzemplarzy", $"Liczba egzemplarzy nie może być mniejsza niż liczba aktualnie wypożyczonych ({wypozyczoneEgzemplarze}).");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Przygotowanie obiektu do aktualizacji
                    var ksiazkaToUpdate = new Ksiazka
                    {
                        Id = ksiazka.Id,
                        Tytul = ksiazka.Tytul,
                        Autor = ksiazka.Autor,
                        ISBN = ksiazka.ISBN,
                        Tematyka = ksiazka.Tematyka,
                        IloscEgzemplarzy = ksiazka.IloscEgzemplarzy
                    };

                    // Obliczanie nowych dostępnych egzemplarzy
                    int roznicaEgzemplarzy = ksiazka.IloscEgzemplarzy - bookFromDb.IloscEgzemplarzy;
                    ksiazkaToUpdate.DostepneEgzemplarze = bookFromDb.DostepneEgzemplarze + roznicaEgzemplarzy;

                    // Dodatkowe zabezpieczenie (chociaż poprzednia walidacja powinna to wyłapać)
                    if (ksiazkaToUpdate.DostepneEgzemplarze < 0)
                    {
                        ModelState.AddModelError("IloscEgzemplarzy", "Aktualizacja liczby egzemplarzy spowodowałaby ujemną liczbę dostępnych egzemplarzy.");
                        return View(ksiazka);
                    }

                    _context.Update(ksiazkaToUpdate);
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
                    else
                    {
                        throw; // Rzuć wyjątek dalej, jeśli nie jest to problem z istnieniem książki
                    }
                }
                catch (DbUpdateException ex)
                {
                    TempData["ErrorMessage"] = $"Wystąpił błąd podczas aktualizacji książki: {ex.Message}";
                    // Logowanie błędu może być tu przydatne
                    return View(ksiazka); // Zwróć widok z modelem, aby użytkownik mógł poprawić dane
                }
            }
            return View(ksiazka); // Jeśli ModelState nie jest ważny, zwróć widok z błędami
        }


        // Akcja do usuwania książki na podstawie ID
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

            // Sprawdzanie, czy książka jest aktualnie wypożyczona
            var activeWypozyczenia = await _context.Wypozyczenie
                .AnyAsync(w => w.KsiazkaId == id && !w.CzyZwrocona);
            if (activeWypozyczenia)
            {
                TempData["ErrorMessage"] = "Nie można usunąć książki, która jest aktualnie wypożyczona.";
                return RedirectToAction(nameof(Katalog));
            }

            return View(ksiazka);
        }


        // Akcja POST - usuwanie książki
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

            // Ponowne sprawdzenie, czy książka jest aktualnie wypożyczona
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
                // Logowanie błędu
                return RedirectToAction(nameof(Katalog));
            }

            return RedirectToAction(nameof(Katalog));
        }

        // Metoda pomocnicza do sprawdzania, czy książka istnieje w bazie danych
        private bool KsiazkaExists(int id)
        {
            return _context.Ksiazka.Any(e => e.Id == id);
        }

        // Akcja do zwracania książki przez administratora lub bibliotekarza
        [Authorize(Roles = "Administrator, Bibliotekarz")]
        public async Task<IActionResult> ZwrocByAdmin(int id) // id tutaj to KsiazkaId
        {
            // Pobieranie wypożyczenia na podstawie ID książki
            var wypozyczenie = await _context.Wypozyczenie
                .FirstOrDefaultAsync(w => w.KsiazkaId == id && !w.CzyZwrocona);

            if (wypozyczenie == null)
            {
                TempData["ErrorMessage"] = "Nie znaleziono aktywnego wypożyczenia dla tej książki.";
                return RedirectToAction("Index", "Bibliotekarz");
            }

            // Pobieranie książki na podstawie ID
            var ksiazka = await _context.Ksiazka.FindAsync(id); // id to KsiazkaId
            if (ksiazka == null)
            {
                TempData["ErrorMessage"] = "Nie znaleziono książki.";
                return RedirectToAction("Index", "Bibliotekarz");
            }

            // Zmiana statusu wypożyczenia na zwrócone
            wypozyczenie.CzyZwrocona = true;
            wypozyczenie.TerminZwrotu = DateTime.Now; // Opcjonalnie: zapisz datę zwrotu
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
                // Logowanie błędu
                return RedirectToAction("Index", "Bibliotekarz");
            }

            return RedirectToAction("Index", "Bibliotekarz");
        }
    }
}