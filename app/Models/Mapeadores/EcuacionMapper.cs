using app.Models.ViewModels;
using final_para.Ecuaciones;

namespace app.Models.Mapeadores;

public static class EcuacionMapper
{
    public static EcuacionParseadaVM DesdeParseo(
        Ecuacion ecuacion,
        string textoPlano,
        string latex,
        ValidadorEcuacion.ResultadoValidacion validacion)
    {
        return new EcuacionParseadaVM
        {
            TextoPlano = textoPlano,
            Latex = latex,
            VariablesDependientes = ecuacion.VariablesDependientes,
            VariablesIndependientes = ecuacion.VariablesIndependientes,
            Orden = ecuacion.Orden,
            EsValida = validacion.EsValida,
            Errores = validacion.Errores
        };
    }

    /// <summary>
    /// El parser ya infiere DependenciaTiempo, pero las CondicionesIniciales reales
    /// se proveen en el paso 2 del flujo. Aquí se inyecta un placeholder de CI cuando
    /// hay dependencia temporal para que el paso 1 pase la validación estructural.
    /// </summary>
    public static Ecuacion NormalizarTrasParseo(Ecuacion ecuacion)
    {
        if (!ecuacion.DependenciaTiempo || ecuacion.CondicionesIniciales.Length > 0)
            return ecuacion;

        return new Ecuacion(
            terminos: ecuacion.Terminos,
            variablesDependientes: ecuacion.VariablesDependientes,
            variablesIndependientes: ecuacion.VariablesIndependientes,
            orden: ecuacion.Orden,
            condicionesIniciales: new[] { "<por_definir>" },
            condicionesFrontera: ecuacion.CondicionesFrontera,
            lineal: ecuacion.Lineal,
            geometria: ecuacion.Geometria,
            dependenciaTiempo: ecuacion.DependenciaTiempo);
    }

    /// <summary>
    /// Enriquece la Ecuacion parseada con las condiciones iniciales/frontera y la dependencia
    /// temporal que se infieren de la configuración del usuario.
    /// </summary>
    public static Ecuacion ConConfiguracion(Ecuacion baseEcuacion, ConfiguracionProblemaVM config)
    {
        bool dependeT = baseEcuacion.VariablesIndependientes.Contains("t");
        var ci = dependeT ? FormatearCondiciones(config.CondicionesIniciales) : Array.Empty<string>();
        if (dependeT && ci.Length == 0 && !string.IsNullOrWhiteSpace(config.CondicionInicial))
        {
            ci = new[] { $"u(x,0)={config.CondicionInicial}" };
        }

        var cf = FormatearCondiciones(config.CondicionesFrontera);
        if (cf.Length == 0)
        {
            cf = new[]
            {
                $"u({config.XMin},t)={config.CondicionFronteraIzq}",
                $"u({config.XMax},t)={config.CondicionFronteraDer}"
            };
        }

        return new Ecuacion(
            terminos: baseEcuacion.Terminos,
            variablesDependientes: baseEcuacion.VariablesDependientes,
            variablesIndependientes: baseEcuacion.VariablesIndependientes,
            orden: baseEcuacion.Orden,
            condicionesIniciales: ci,
            condicionesFrontera: cf,
            lineal: baseEcuacion.Lineal,
            geometria: baseEcuacion.Geometria,
            dependenciaTiempo: dependeT);
    }

    private static string[] FormatearCondiciones(IReadOnlyDictionary<string, string>? condiciones)
    {
        if (condiciones is null || condiciones.Count == 0)
            return Array.Empty<string>();

        return condiciones
            .Where(kv => !string.IsNullOrWhiteSpace(kv.Key) && !string.IsNullOrWhiteSpace(kv.Value))
            .OrderBy(kv => kv.Key)
            .Select(kv => $"{kv.Key}={kv.Value.Trim()}")
            .ToArray();
    }
}
