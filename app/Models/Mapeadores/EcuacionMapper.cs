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

    public static Ecuacion NormalizarTrasParseo(Ecuacion ecuacion) => ecuacion;

    public static Ecuacion ConConfiguracion(Ecuacion baseEcuacion, ConfiguracionProblemaVM config)
    {
        bool dependeT = baseEcuacion.VariablesIndependientes.Contains("t");

        var ci = config.CondicionesIniciales
            .Where(par => !string.IsNullOrWhiteSpace(par.Value))
            .OrderBy(par => par.Key, StringComparer.Ordinal)
            .Select(par => $"{par.Key}={par.Value}")
            .ToArray();

        var cf = config.CondicionesFrontera
            .Where(par => !string.IsNullOrWhiteSpace(par.Value))
            .OrderBy(par => par.Key, StringComparer.Ordinal)
            .Select(par => $"{par.Key}={par.Value}")
            .ToArray();

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
}
