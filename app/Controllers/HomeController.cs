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
        var sessionId = HttpContext.Session.Id;
        var vm = _sesion.ObtenerEcuacionVM(sessionId) ?? new EcuacionParseadaVM();

        // La vista renderiza el paso 2 (configuración) de forma dinámica
        // en función de la ecuación parseada. Si todavía no hay ecuación
        // válida en sesión, el paso 2 queda oculto por completo.
        ViewBag.Ecuacion = _sesion.ObtenerEcuacion(sessionId);
        return View(vm);
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
