using BibliotekaWeb.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace BibliotekaWeb.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        // Akcja do wy�wietlania strony g��wnej
        public IActionResult Index()
        {
            return View();
        }

        // Akcja do wy�wietlania strony z informacjami o prywatno�ci (nie u�ywana)
        public IActionResult Privacy()
        {
            return View();
        }

        // Akcja do wy�wietlania strony z b��dem
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            // Obiekt z informacjami o b��dzie
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
