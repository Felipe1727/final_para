using System.Text.RegularExpressions;

namespace final_para.Ecuaciones;

public static class ValidadorEcuacion
{
    private static readonly Regex DerivadaRegex = new(@"\bu_([a-z]+)\b");
    private static readonly Regex VariableRegex = new(@"\b([a-z])\b");
    private static readonly Regex NoLinealRegex = new(@"u_[a-z]+\s*\^\s*[2-9]|u_[a-z]+\s*\*\s*u_[a-z]+|u\s*\^\s*[2-9]");

    public record ResultadoValidacion(bool EsValida, IReadOnlyList<string> Errores);

    public static ResultadoValidacion Validar(Ecuacion ecuacion)
    {
        var errores = new List<string>();

        if (ecuacion.Terminos.Length == 0)
            errores.Add("La ecuación debe tener al menos un término.");

        if (ecuacion.VariablesIndependientes.Length == 0)
            errores.Add("La ecuación debe tener al menos una variable independiente.");

        byte ordenDetectado = 0;
        foreach (var termino in ecuacion.Terminos)
        {
            var matches = DerivadaRegex.Matches(termino.Expresion);
            foreach (Match m in matches)
            {
                int subindices = m.Groups[1].Value.Length;
                if (subindices > ordenDetectado) ordenDetectado = (byte)subindices;
            }
        }
        if (ordenDetectado > ecuacion.Orden)
            errores.Add($"Orden declarado ({ecuacion.Orden}) es menor que el orden detectado en términos ({ordenDetectado}).");

        bool tieneDerivadaT = ecuacion.Terminos.Any(t => Regex.IsMatch(t.Expresion, @"u_[a-z]*t[a-z]*"));
        if (tieneDerivadaT && !ecuacion.DependenciaTiempo)
            errores.Add("La ecuación contiene derivadas temporales pero DependenciaTiempo es false.");

        if (ecuacion.DependenciaTiempo && ecuacion.CondicionesIniciales.Length == 0)
            errores.Add("Una ecuación con dependencia temporal requiere al menos una condición inicial.");

        if (ecuacion.Lineal)
        {
            foreach (var termino in ecuacion.Terminos)
            {
                if (NoLinealRegex.IsMatch(termino.Expresion))
                {
                    errores.Add($"La ecuación está marcada como lineal pero contiene un término no lineal: '{termino.Expresion}'.");
                    break;
                }
            }
        }

        var variablesEnTerminos = new HashSet<string>();
        foreach (var termino in ecuacion.Terminos)
        {
            var sinDerivadas = DerivadaRegex.Replace(termino.Expresion, "");
            var vars = VariableRegex.Matches(sinDerivadas);
            foreach (Match m in vars)
            {
                var v = m.Groups[1].Value;
                if (v != "u") variablesEnTerminos.Add(v);
            }
        }

        var declaradas = new HashSet<string>(ecuacion.VariablesIndependientes);
        foreach (var v in variablesEnTerminos)
        {
            if (!declaradas.Contains(v))
                errores.Add($"La variable '{v}' aparece en los términos pero no está declarada en VariablesIndependientes.");
        }

        return new ResultadoValidacion(errores.Count == 0, errores);
    }
}
