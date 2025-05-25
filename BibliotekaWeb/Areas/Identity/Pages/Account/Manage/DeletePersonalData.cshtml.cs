// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using BibliotekaWeb.Data;
using Microsoft.EntityFrameworkCore;

namespace BibliotekaWeb.Areas.Identity.Pages.Account.Manage
{
    public class DeletePersonalDataModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly ILogger<DeletePersonalDataModel> _logger;
        private readonly ApplicationDbContext _context;

        // Nowe właściwości do kontrolowania widoku
        public bool CanAttemptToDelete { get; private set; } = true;
        public string CannotDeleteReason { get; private set; }

        public DeletePersonalDataModel(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            ILogger<DeletePersonalDataModel> logger,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            _context = context;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Hasło jest wymagane.")]
            [DataType(DataType.Password)]
            [Display(Name = "Hasło")]
            public string Password { get; set; }
        }

        public bool RequirePassword { get; set; }

        private async Task CheckIfUserCanBeDeleted(IdentityUser user)
        {
            if (user == null)
            {
                CanAttemptToDelete = false;
                CannotDeleteReason = "Nie można zidentyfikować użytkownika.";
                return;
            }

            // Sprawdzenie ról Administrator lub Bibliotekarz
            if (await _userManager.IsInRoleAsync(user, "Administrator") || await _userManager.IsInRoleAsync(user, "Bibliotekarz"))
            {
                CanAttemptToDelete = false;
                CannotDeleteReason = "Administratorzy oraz Bibliotekarze nie mogą samodzielnie usuwać swoich kont za pomocą tej funkcji. Skontaktuj się z innym administratorem w celu zarządzania kontem.";
                _logger.LogWarning($"Użytkownik {user.Id} ({user.UserName}) z rolą Administrator/Bibliotekarz próbował uzyskać dostęp do strony usuwania własnego konta.");
                return; // Zakończ sprawdzanie, jeśli rola blokuje
            }

            // Sprawdzenie aktywnych wypożyczeń (jeśli CanAttemptToDelete jest nadal true)
            var hasActiveLoans = await _context.Wypozyczenie
                                     .AnyAsync(w => w.CzytelnikId == user.Id && !w.CzyZwrocona);
            if (hasActiveLoans)
            {
                CanAttemptToDelete = false; // Technicznie mogą próbować, ale dostaną błąd. Tu ustawiamy dla spójności komunikatu OnGet
                CannotDeleteReason = "Nie możesz usunąć konta, ponieważ masz niezwrócone książki. Prosimy najpierw zwrócić wszystkie wypożyczone pozycje.";
                // Nie logujemy tu Warning, bo to standardowa blokada
            }
        }


        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Nie można załadować użytkownika o ID '{_userManager.GetUserId(User)}'.");
            }

            await CheckIfUserCanBeDeleted(user); // Sprawdź, czy użytkownik może próbować usunąć konto

            RequirePassword = await _userManager.HasPasswordAsync(user);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Nie można załadować użytkownika o ID '{_userManager.GetUserId(User)}'.");
            }

            // Ponowne sprawdzenie ról jako zabezpieczenie serwerowe na POST
            if (await _userManager.IsInRoleAsync(user, "Administrator") || await _userManager.IsInRoleAsync(user, "Bibliotekarz"))
            {
                _logger.LogWarning($"Użytkownik {user.Id} ({user.UserName}) z rolą Administrator/Bibliotekarz próbował usunąć swoje konto (POST).");
                ModelState.AddModelError(string.Empty, "Administratorzy oraz Bibliotekarze nie mogą samodzielnie usuwać swoich kont za pomocą tej funkcji. Skontaktuj się z innym administratorem w celu zarządzania kontem.");
                await CheckIfUserCanBeDeleted(user); // Ustaw flagi dla widoku
                return Page();
            }

            RequirePassword = await _userManager.HasPasswordAsync(user);
            if (RequirePassword)
            {
                if (!await _userManager.CheckPasswordAsync(user, Input.Password))
                {
                    ModelState.AddModelError(string.Empty, "Nieprawidłowe hasło.");
                    await CheckIfUserCanBeDeleted(user); // Ustaw flagi dla widoku
                    return Page();
                }
            }

            // Sprawdzenie aktywnych wypożyczeń
            var hasActiveLoans = await _context.Wypozyczenie
                                         .AnyAsync(w => w.CzytelnikId == user.Id && !w.CzyZwrocona);

            if (hasActiveLoans)
            {
                _logger.LogInformation($"Użytkownik {user.Id} ({user.UserName}) próbował usunąć konto, ale ma aktywne wypożyczenia (POST).");
                ModelState.AddModelError(string.Empty, "Nie możesz usunąć konta, ponieważ masz niezwrócone książki. Prosimy najpierw zwrócić wszystkie wypożyczone pozycje.");
                await CheckIfUserCanBeDeleted(user); // Ustaw flagi dla widoku
                return Page();
            }

            var result = await _userManager.DeleteAsync(user);
            var userId = await _userManager.GetUserIdAsync(user);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    _logger.LogError("Błąd podczas usuwania użytkownika {UserId}: {ErrorCode} - {ErrorDescription}", userId, error.Code, error.Description);
                }
                throw new InvalidOperationException($"Wystąpił nieoczekiwany błąd podczas usuwania użytkownika o ID '{userId}'. Szczegóły błędów zostały zapisane w logach.");
            }

            await _signInManager.SignOutAsync();

            _logger.LogInformation("Użytkownik o ID '{UserId}' usunął swoje konto.", userId);

            return Redirect("~/");
        }
    }
}