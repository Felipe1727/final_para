using app.Models.ViewModels;
using app.Services;
using Microsoft.AspNetCore.Mvc;

namespace app.Controllers;

[Route("[controller]/[action]")]
public class WizardController : Controller
{
    private readonly ServicioSesion _sesion;

    public WizardController(ServicioSesion sesion)
    {
        _sesion = sesion;
    }

    [HttpGet]
    public IActionResult Ecuacion()
    {
        HttpContext.Session.SetString("inicializada", "1");
        var sid = HttpContext.Session.Id;
        var vm = _sesion.ObtenerEcuacionVM(sid) ?? new EcuacionParseadaVM();
        return View(vm);
    }

    [HttpGet]
    public IActionResult Configuracion()
    {
        var sid = HttpContext.Session.Id;
        var ec = _sesion.ObtenerEcuacionVM(sid);
        if (ec is null || !ec.EsValida) return RedirectToAction(nameof(Ecuacion));

        var cfg = _sesion.ObtenerConfiguracion(sid) ?? new ConfiguracionProblemaVM();
        ViewBag.Ecuacion = ec;
        return View(cfg);
    }

    [HttpGet]
    public IActionResult Progreso()
    {
        var sid = HttpContext.Session.Id;
        var cfg = _sesion.ObtenerConfiguracion(sid);
        var ec = _sesion.ObtenerEcuacionVM(sid);
        if (cfg is null)
        {
            // Si todavía no hay config guardada, la usaremos directo del formulario via JS.
            // Pero requerimos al menos ecuación válida para llegar aquí.
            if (ec is null || !ec.EsValida) return RedirectToAction(nameof(Ecuacion));
            return RedirectToAction(nameof(Configuracion));
        }
        ViewBag.Ecuacion = ec;
        return View(cfg);
    }

    [HttpGet]
    public IActionResult Resultados()
    {
        var sid = HttpContext.Session.Id;
        var r = _sesion.ObtenerResultado(sid);
        if (r is null)
        {
            var cfg = _sesion.ObtenerConfiguracion(sid);
            if (cfg is null) return RedirectToAction(nameof(Configuracion));
            return RedirectToAction(nameof(Progreso));
        }
        return View(r);
    }

    [HttpGet]
    public IActionResult Comparacion()
    {
        var sid = HttpContext.Session.Id;
        var r = _sesion.ObtenerResultado(sid);
        if (r is null) return RedirectToAction(nameof(Resultados));
        return View(r);
    }

    [HttpPost]
    public IActionResult GuardarConfiguracion([FromBody] ConfiguracionProblemaVM config)
    {
        var sid = HttpContext.Session.Id;
        var ec = _sesion.ObtenerEcuacionVM(sid);
        if (ec is null || !ec.EsValida)
        {
            return BadRequest(new { error = "No hay ecuación válida en sesión." });
        }

        if (!ModelState.IsValid)
        {
            var errores = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();
            return BadRequest(new { error = "Configuración inválida.", detalles = errores });
        }

        _sesion.GuardarConfiguracion(sid, config);
        return Ok(new { ok = true, siguienteUrl = "/Wizard/Progreso" });
    }

    [HttpPost]
    public IActionResult Reiniciar()
    {
        _sesion.Limpiar(HttpContext.Session.Id);
        return RedirectToAction(nameof(Ecuacion));
    }
}
