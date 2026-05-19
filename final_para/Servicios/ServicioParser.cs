using System.Text.Json;
using System.Text.RegularExpressions;
using final_para.Interfaces;
using final_para.Ecuaciones;

namespace final_para.Servicios;

public class ServicioParser : IParse
{
    private readonly Dictionary<string, string> _regexFunc;
    private readonly Dictionary<string, string> _regexLatex;

    public ServicioParser()
    {
        _regexFunc = CargarJson("Config/regexFunc.json");
        _regexLatex = CargarJson("Config/regexLatex.json");
    }

    public ServicioParser(
        Dictionary<string, string> regexFunc,
        Dictionary<string, string> regexLatex)
    {
        _regexFunc = regexFunc;
        _regexLatex = regexLatex;
    }

    private static Dictionary<string, string> CargarJson(string ruta)
    {
        try
        {
            var rutaCompleta = Path.Combine(AppContext.BaseDirectory, ruta);
            var json = File.ReadAllText(rutaCompleta);
            return JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? [];
        }
        catch (Exception ex)
        {
            // Si falla cargar, retorna diccionario vacío
            System.Diagnostics.Debug.WriteLine($"Advertencia al cargar {ruta}: {ex.Message}");
            return [];
        }
    }

    public string ParseLatex(string expresion)
    {
        var resultado = AplicarTransformaciones(expresion, _regexLatex);

        resultado = Regex.Replace(resultado, @"u_([a-z]+)", m =>
        {
            var subindices = m.Groups[1].Value;
            var orden = subindices.Length;

            if (orden == 1)
            {
                return $@"\frac{{\partial u}}{{\partial {subindices[0]}}}";
            }
            else
            {
                var denominador = string.Join(" ", subindices
                    .GroupBy(c => c)
                    .Select(g => g.Count() == 1 ? $@"\partial {g.Key}" : $@"\partial {g.Key}^{{{g.Count()}}}"));
                return $@"\frac{{\partial^{orden} u}}{{{denominador}}}";
            }
        });

        return resultado;
    }

    public Ecuacion ParseFunc(string expresion)
    {
        var partes = expresion.Split('=');
        if (partes.Length != 2)
            throw new ArgumentException("La ecuación debe contener exactamente un '='");

        var ladoIzquierdo = partes[0].Trim();
        var ladoDerecho = partes[1].Trim();

        var terminosIzq = TokenizarTerminos(ladoIzquierdo);
        var terminosDer = TokenizarTerminos(ladoDerecho);

        // Negar los términos del lado derecho (pasar al lado izquierdo)
        var terminosDerNegados = terminosDer
            .Select(t => new Termino(t.Expresion, !t.EsPositivo))
            .ToList();

        var todosLosTerminos = terminosIzq.Concat(terminosDerNegados).ToArray();

        // Aplicar transformaciones regex a cada término
        var terminosTransformados = todosLosTerminos
            .Select(t => t with { Expresion = AplicarTransformaciones(t.Expresion, _regexFunc) })
            .ToArray();

        // Detectar variables
        var variablesDependientes = new[] { "u" };
        var variablesIndependientes = ExtraerVariablesIndependientes(terminosTransformados);
        var orden = CalcularOrden(terminosTransformados);
        var dependeT = variablesIndependientes.Contains("t");

        return new Ecuacion(
            terminos: terminosTransformados,
            variablesDependientes: variablesDependientes,
            variablesIndependientes: variablesIndependientes,
            orden: orden,
            condicionesIniciales: Array.Empty<string>(),
            condicionesFrontera: Array.Empty<string>(),
            lineal: true,
            geometria: Geometria.Parabolica,
            dependenciaTiempo: dependeT
        );
    }

    private List<Termino> TokenizarTerminos(string lado)
    {
        var terminos = new List<Termino>();
        var expresion = lado.Replace(" ", "");

        if (string.IsNullOrEmpty(expresion))
            return terminos;

        // Insertar + al inicio si no hay signo
        if (expresion[0] != '+' && expresion[0] != '-')
            expresion = "+" + expresion;

        // Regex para capturar signo + expresión
        var matches = Regex.Matches(expresion, @"([+-])([^+-]+)");

        foreach (Match match in matches)
        {
            var signo = match.Groups[1].Value == "+";
            var expr = match.Groups[2].Value.Trim();

            if (!string.IsNullOrEmpty(expr))
            {
                terminos.Add(new Termino(expr, signo));
            }
        }

        return terminos;
    }

    private string[] ExtraerVariablesIndependientes(Termino[] terminos)
    {
        var variables = new HashSet<char>();

        foreach (var termino in terminos)
        {
            // Encontrar variables que no sean 'u': x, y, z, t, etc.
            var match = Regex.Matches(termino.Expresion, @"[a-z]");
            foreach (Match m in match)
            {
                var c = m.Value[0];
                if (c != 'u')
                    variables.Add(c);
            }
        }

        return variables.OrderBy(v => v).Select(v => v.ToString()).ToArray();
    }

    private byte CalcularOrden(Termino[] terminos)
    {
        byte ordenMax = 0;

        foreach (var termino in terminos)
        {
            // Buscar patrones u_xyz y contar subíndices
            var matches = Regex.Matches(termino.Expresion, @"u_([a-z]+)");
            foreach (Match match in matches)
            {
                var subindices = match.Groups[1].Value.Length;
                if (subindices > ordenMax)
                    ordenMax = (byte)subindices;
            }
        }

        return ordenMax;
    }

    private string AplicarTransformaciones(string expr, Dictionary<string, string> reglas)
    {
        foreach (var (patron, reemplazo) in reglas)
        {
            expr = Regex.Replace(expr, patron, reemplazo);
        }
        return expr;
    }
}