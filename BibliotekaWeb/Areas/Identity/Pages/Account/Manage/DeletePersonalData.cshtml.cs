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
using BibliotekaWeb.Data; // Upewnij się, że to poprawna przestrzeń nazw dla Twojego ApplicationDbContext
using Microsoft.EntityFrameworkCore; // Dla AnyAsync()

namespace BibliotekaWeb.Areas.Identity.Pages.Account.Manage
{
    public class DeletePersonalDataModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly ILogger<DeletePersonalDataModel> _logger;
        private readonly ApplicationDbContext _context; // Dodane pole dla DbContext

        public DeletePersonalDataModel(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            ILogger<DeletePersonalDataModel> logger,
            ApplicationDbContext context) // Dodany parametr ApplicationDbContext
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            _context = context; // Przypisanie DbContext
        }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public class InputModel
        {
            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required(ErrorMessage = "Hasło jest wymagane.")]
            [DataType(DataType.Password)]
            [Display(Name = "Hasło")]
            public string Password { get; set; }
        }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public bool RequirePassword { get; set; }

        public async Task<IActionResult> OnGet()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Nie można załadować użytkownika o ID '{_userManager.GetUserId(User)}'.");
            }

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

            RequirePassword = await _userManager.HasPasswordAsync(user);
            if (RequirePassword)
            {
                if (!await _userManager.CheckPasswordAsync(user, Input.Password))
                {
                    ModelState.AddModelError(string.Empty, "Nieprawidłowe hasło.");
                    return Page();
                }
            }


            var hasActiveLoans = await _context.Wypozyczenie
                                         .AnyAsync(w => w.CzytelnikId == user.Id && !w.CzyZwrocona);

            if (hasActiveLoans)
            {
                _logger.LogWarning($"Użytkownik {user.Id} ({user.UserName}) próbował usunąć konto, ale ma aktywne wypożyczenia.");
                ModelState.AddModelError(string.Empty, "Nie możesz usunąć konta, ponieważ masz niezwrócone książki. Prosimy najpierw zwrócić wszystkie wypożyczone pozycje.");
                return Page(); // Zwróć stronę z błędem, nie usuwaj konta
            }


            var result = await _userManager.DeleteAsync(user);
            var userId = await _userManager.GetUserIdAsync(user);
            if (!result.Succeeded)
            {
                // logowanie błedów
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