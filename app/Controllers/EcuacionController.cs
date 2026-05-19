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

            return Ok(vm);
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
}
