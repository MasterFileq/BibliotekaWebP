using BibliotekaWeb.Data;
using BibliotekaWeb.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BibliotekaWeb.Controllers
{
    [Authorize]
    public class CzytelniksController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public CzytelniksController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _roleManager = roleManager ?? throw new ArgumentNullException(nameof(roleManager));
        }

        [Authorize(Roles = "Administrator, Bibliotekarz")]
        public async Task<IActionResult> Index()
        {
            var czytelnicy = await _userManager.GetUsersInRoleAsync("Czytelnik");
            var czytelnikViewModels = new List<CzytelnikViewModel>();

            foreach (var user in czytelnicy)
            {
                var wypozyczenie = await _context.Wypozyczenie
                    .Include(w => w.Ksiazka)
                    .Where(w => w.CzytelnikId == user.Id)
                    .OrderByDescending(w => w.DataWypozyczenia)
                    .FirstOrDefaultAsync();

                czytelnikViewModels.Add(new CzytelnikViewModel
                {
                    User = user,
                    KsiazkaTytul = wypozyczenie?.Ksiazka?.Tytul,
                    TerminZwrotu = wypozyczenie?.TerminZwrotu
                });
            }

            return View(czytelnikViewModels);
        }

        [Authorize(Roles = "Administrator")]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Create(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Nieprawidłowe dane formularza.";
                return View(model);
            }

            var user = new IdentityUser { UserName = model.Email, Email = model.Email };
            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                await EnsureCzytelnikRoleExistsAsync();
                await _userManager.AddToRoleAsync(user, "Czytelnik");

                TempData["SuccessMessage"] = "Nowy czytelnik został dodany.";
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            TempData["ErrorMessage"] = "Nie udało się utworzyć nowego czytelnika.";
            return View(model);
        }

        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

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

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(model.UserName) || string.IsNullOrWhiteSpace(model.Email))
            {
                ModelState.AddModelError(string.Empty, "Nazwa użytkownika i email nie mogą być puste.");
                return View(model);
            }

            user.UserName = model.UserName;
            user.Email = model.Email;

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Dane czytelnika zostały zaktualizowane.";
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                TempData["ErrorMessage"] = "Nieprawidłowy identyfikator czytelnika.";
                return RedirectToAction(nameof(Index));
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Nie znaleziono czytelnika.";
                return RedirectToAction(nameof(Index));
            }

            return View(user);
        }

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

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Nie znaleziono czytelnika.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                var wypozyczenia = await _context.Wypozyczenie
                    .Where(w => w.CzytelnikId == id)
                    .Include(w => w.Ksiazka)
                    .ToListAsync();

                foreach (var wypozyczenie in wypozyczenia)
                {
                    if (wypozyczenie.Ksiazka != null)
                    {
                        wypozyczenie.Ksiazka.Dostepnosc = true;
                        _context.Update(wypozyczenie.Ksiazka);
                    }
                }

                if (wypozyczenia.Any())
                {
                    _context.Wypozyczenie.RemoveRange(wypozyczenia);
                }

                await _context.SaveChangesAsync();

                var result = await _userManager.DeleteAsync(user);
                if (!result.Succeeded)
                {
                    var errors = result.Errors.Select(e => e.Description).ToList();
                    System.Diagnostics.Debug.WriteLine($"Błędy Identity: {string.Join("; ", errors)}");
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
                System.Diagnostics.Debug.WriteLine($"Błąd podczas usuwania czytelnika: {ex.Message}\nInnerException: {ex.InnerException?.Message}\nStackTrace: {ex.StackTrace}");
                TempData["ErrorMessage"] = "Wystąpił błąd podczas usuwania czytelnika. Spróbuj ponownie.";
                return RedirectToAction(nameof(Index));
            }
        }

        [Authorize(Roles = "Administrator, Bibliotekarz")]
        public async Task<IActionResult> AssignBook(string czytelnikId)
        {
            var ksiazki = await _context.Ksiazka.Where(k => k.Dostepnosc).ToListAsync();
            var czytelnik = await _userManager.FindByIdAsync(czytelnikId);

            if (czytelnik == null)
            {
                TempData["ErrorMessage"] = "Nie znaleziono czytelnika.";
                return RedirectToAction(nameof(Index));
            }

            var model = new WypozyczenieViewModel
            {
                CzytelnikId = czytelnikId,
                Email = czytelnik.Email,
                Ksiazki = ksiazki
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator, Bibliotekarz")]
        public async Task<IActionResult> AssignBook(WypozyczenieViewModel model)
        {
            ModelState.Remove("Ksiazki");

            if (ModelState.IsValid)
            {
                var czytelnik = await _userManager.FindByIdAsync(model.CzytelnikId);
                if (czytelnik == null)
                {
                    TempData["ErrorMessage"] = "Nie znaleziono czytelnika z podanym identyfikatorem.";
                    model.Ksiazki = await _context.Ksiazka.Where(k => k.Dostepnosc).ToListAsync();
                    return View(model);
                }

                var existingWypozyczenie = await _context.Wypozyczenie
                    .AnyAsync(w => w.CzytelnikId == model.CzytelnikId && w.TerminZwrotu >= DateTime.Now);
                if (existingWypozyczenie)
                {
                    TempData["ErrorMessage"] = "Czytelnik ma już wypożyczoną książkę.";
                    model.Ksiazki = await _context.Ksiazka.Where(k => k.Dostepnosc).ToListAsync();
                    return View(model);
                }

                var ksiazka = await _context.Ksiazka.FindAsync(model.KsiazkaId);
                if (ksiazka == null || !ksiazka.Dostepnosc)
                {
                    TempData["ErrorMessage"] = "Wybrana książka jest niedostępna.";
                    model.Ksiazki = await _context.Ksiazka.Where(k => k.Dostepnosc).ToListAsync();
                    return View(model);
                }

                var wypozyczenie = new Wypozyczenie
                {
                    KsiazkaId = model.KsiazkaId,
                    CzytelnikId = model.CzytelnikId,
                    DataWypozyczenia = DateTime.Now,
                    TerminZwrotu = DateTime.Now.AddDays(14)
                };

                _context.Add(wypozyczenie);
                ksiazka.Dostepnosc = false;
                _context.Update(ksiazka);

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Książka została przypisana czytelnikowi.";
                return RedirectToAction(nameof(Index));
            }

            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            System.Diagnostics.Debug.WriteLine($"Validation errors: {string.Join("; ", errors)}");
            TempData["ErrorMessage"] = string.Join("; ", errors) + " Proszę upewnić się, że wybrano książkę.";
            model.Ksiazki = await _context.Ksiazka.Where(k => k.Dostepnosc).ToListAsync();
            return View(model);
        }

        private async Task EnsureCzytelnikRoleExistsAsync()
        {
            if (!await _roleManager.RoleExistsAsync("Czytelnik"))
            {
                await _roleManager.CreateAsync(new IdentityRole("Czytelnik"));
            }
        }
    }
}