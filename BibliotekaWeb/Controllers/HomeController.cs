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

        // Akcja do wyœwietlania strony g³ównej
        public IActionResult Index()
        {
            return View();
        }

        // Akcja do wyœwietlania strony z informacjami o prywatnoœci (nie u¿ywana)
        public IActionResult Privacy()
        {
            return View();
        }

        // Akcja do wyœwietlania strony z b³êdem
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            // Obiekt z informacjami o b³êdzie
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
