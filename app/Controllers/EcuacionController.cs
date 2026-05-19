using app.Models.Mapeadores;
using app.Models.ViewModels;
using app.Services;
using final_para.Ecuaciones;
using final_para.Servicios;
using Microsoft.AspNetCore.Mvc;

namespace app.Controllers;

[Route("[controller]/[action]")]
public class EcuacionController : Controller
{
    private readonly ServicioParser _parser;
    private readonly ServicioSesion _sesion;

    public EcuacionController(ServicioParser parser, ServicioSesion sesion)
    {
        _parser = parser;
        _sesion = sesion;
    }

    [HttpPost]
    public IActionResult Parsear([FromBody] EcuacionInputVM input)
    {
        HttpContext.Session.SetString("inicializada", "1");
        var sessionId = HttpContext.Session.Id;

        if (!ModelState.IsValid || string.IsNullOrWhiteSpace(input.TextoPlano))
        {
            return BadRequest(new EcuacionParseadaVM
            {
                TextoPlano = input.TextoPlano ?? string.Empty,
                EsValida = false,
                Errores = new[] { "La ecuación es obligatoria." }
            });
        }

        try
        {
            Ecuacion ecuacion = EcuacionMapper.NormalizarTrasParseo(_parser.ParseFunc(input.TextoPlano));
            string latex = _parser.ParseLatex(input.TextoPlano);
            var validacion = ValidadorEcuacion.Validar(ecuacion);
            var vm = EcuacionMapper.DesdeParseo(ecuacion, input.TextoPlano, latex, validacion);

            if (validacion.EsValida)
            {
                _sesion.GuardarEcuacion(sessionId, ecuacion, vm);
            }

            return Ok(new
            {
                vm.TextoPlano,
                vm.Latex,
                vm.VariablesDependientes,
                vm.VariablesIndependientes,
                vm.Orden,
                vm.EsValida,
                vm.Errores,
                siguienteUrl = vm.EsValida ? "/Wizard/Configuracion" : null
            });
        }
        catch (Exception ex)
        {
            return Ok(new EcuacionParseadaVM
            {
                TextoPlano = input.TextoPlano,
                EsValida = false,
                Errores = new[] { $"Error al parsear: {ex.Message}" }
            });
        }
    }

    [HttpGet]
    public IActionResult Plantillas()
    {
        var plantillas = new[]
        {
            new { id = "onda",       nombre = "Onda 1D",    texto = "u_tt = c^2 * u_xx" },
            new { id = "calor",      nombre = "Calor 1D",   texto = "u_t = alpha * u_xx" },
            new { id = "laplace",    nombre = "Laplace 2D", texto = "u_xx + u_yy = 0" },
            new { id = "transporte", nombre = "Transporte", texto = "u_t + a * u_x = 0" },
            new { id = "burgers",    nombre = "Burgers",    texto = "u_t + u * u_x = nu * u_xx" }
        };
        return Ok(plantillas);
    }
}
