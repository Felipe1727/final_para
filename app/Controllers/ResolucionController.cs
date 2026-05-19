using app.Models.ViewModels;
using app.Services;
using Microsoft.AspNetCore.Mvc;

namespace app.Controllers;

[Route("[controller]/[action]")]
public class ResolucionController : Controller
{
    private readonly ServicioResolucion _servicio;
    private readonly ServicioSesion _sesion;
    private readonly ILogger<ResolucionController> _logger;

    public ResolucionController(
        ServicioResolucion servicio,
        ServicioSesion sesion,
        ILogger<ResolucionController> logger)
    {
        _servicio = servicio;
        _sesion = sesion;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Iniciar([FromBody] ConfiguracionProblemaVM config)
    {
        HttpContext.Session.SetString("inicializada", "1");
        var sessionId = HttpContext.Session.Id;
        var ecuacion = _sesion.ObtenerEcuacion(sessionId);
        if (ecuacion is null)
        {
            return BadRequest(new { error = "No hay ecuación válida en sesión. Vuelva al paso 1." });
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(new
            {
                error = "Configuración inválida.",
                detalles = ModelState
                    .Where(kv => kv.Value!.Errors.Count > 0)
                    .ToDictionary(kv => kv.Key, kv => kv.Value!.Errors.Select(e => e.ErrorMessage).ToArray())
            });
        }

        try
        {
            var resultado = await _servicio.ResolverAsync(sessionId, ecuacion, config, HttpContext.RequestAborted);
            _sesion.GuardarResultado(sessionId, resultado);
            return Ok(resultado);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al resolver la EDP");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet]
    public IActionResult Resultados()
    {
        var sessionId = HttpContext.Session.Id;
        var resultado = _sesion.ObtenerResultado(sessionId);
        if (resultado is null) return NotFound();
        return Ok(resultado);
    }

    [HttpGet]
    public string SessionId()
    {
        HttpContext.Session.SetString("inicializada", "1");
        return HttpContext.Session.Id;
    }
}
