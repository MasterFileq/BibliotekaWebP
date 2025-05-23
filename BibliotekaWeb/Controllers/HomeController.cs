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

        // Akcja do wyświetlania strony głównej
        public IActionResult Index()
        {
            return View();
        }

        // Akcja do wyświetlania strony z informacjami o prywatności (nie używana)
        public IActionResult Privacy()
        {
            return View();
        }

        // Akcja do wyświetlania strony z błędem
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            // Obiekt z informacjami o błędzie
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
