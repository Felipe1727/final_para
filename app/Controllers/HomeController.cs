using System.Diagnostics;
using app.Models;
using app.Models.ViewModels;
using app.Services;
using Microsoft.AspNetCore.Mvc;

namespace app.Controllers;

public class HomeController : Controller
{
    private readonly ServicioSesion _sesion;

    public HomeController(ServicioSesion sesion)
    {
        _sesion = sesion;
    }

    public IActionResult Index()
    {
        HttpContext.Session.SetString("inicializada", "1");
        return RedirectToAction("Ecuacion", "Wizard");
    }

    [HttpPost]
    public IActionResult Reiniciar()
    {
        var sessionId = HttpContext.Session.Id;
        _sesion.Limpiar(sessionId);
        return RedirectToAction(nameof(Index));
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
