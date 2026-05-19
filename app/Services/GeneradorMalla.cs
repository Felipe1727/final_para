using app.Models.ViewModels;

namespace app.Services;

/// <summary>
/// Construye mallas jagged double[][] con la convención que esperan FDM y FEM en la librería.
/// - Para FDM: malla 2D nx × nt donde cada celda guarda el valor de u en ese nodo
///   (precondicionada con la condición inicial y las condiciones de frontera).
/// - Para FEM (Cuadrilateral/Triangular): malla cuadrada n × 2 donde cada fila es (x, y)
///   del nodo. n = lado² para que pase el check EsMallaCuadrada de FEM.
/// </summary>
public class GeneradorMalla
{
    public (double[][] mallaFDM, double[] ejeX, double[] ejeT) ConstruirMallaFDM(ConfiguracionProblemaVM cfg)
    {
        var variableEspacial = ResolverVariableEspacial(cfg);
        var variableTemporal = ResolverVariableTemporal(cfg, variableEspacial);

        int nx = ObtenerN(cfg, variableEspacial, cfg.Nx);
        int nt = ObtenerN(cfg, variableTemporal, cfg.Nt);
        double xMin = ObtenerMin(cfg, variableEspacial, cfg.XMin);
        double xMax = ObtenerMax(cfg, variableEspacial, cfg.XMax);
        double tMin = ObtenerMin(cfg, variableTemporal, cfg.TMin);
        double tMax = ObtenerMax(cfg, variableTemporal, cfg.TMax);

        var ejeX = LinSpace(xMin, xMax, nx);
        var ejeT = LinSpace(tMin, tMax, nt);

        var malla = new double[nx][];
        double valIzq = EvaluarConstante(ObtenerCondicionFrontera(cfg, variableEspacial, "Min", cfg.CondicionFronteraIzq));
        double valDer = EvaluarConstante(ObtenerCondicionFrontera(cfg, variableEspacial, "Max", cfg.CondicionFronteraDer));
        string condicionInicial = ObtenerCondicionInicial(cfg, cfg.CondicionInicial);

        for (int i = 0; i < nx; i++)
        {
            malla[i] = new double[nt];
            double valorInicial = EvaluarCondicionInicial(condicionInicial, ejeX[i]);
            for (int j = 0; j < nt; j++)
            {
                if (j == 0)
                    malla[i][j] = valorInicial;
                else if (i == 0)
                    malla[i][j] = valIzq;
                else if (i == nx - 1)
                    malla[i][j] = valDer;
                else
                    malla[i][j] = valorInicial;
            }
        }

        return (malla, ejeX, ejeT);
    }

    /// <summary>
    /// Malla FEM como lista de nodos (x,y) en cuadrícula cuadrada. El lado se ajusta
    /// al cuadrado perfecto más cercano a cfg.Nx para satisfacer EsMallaCuadrada.
    /// </summary>
    public (double[][] mallaFEM, int lado) ConstruirMallaFEM(ConfiguracionProblemaVM cfg)
    {
        var variableEspacial = ResolverVariableEspacial(cfg);
        var variableTemporal = ResolverVariableTemporal(cfg, variableEspacial);
        int lado = Math.Max(3, (int)Math.Round(Math.Sqrt(ObtenerN(cfg, variableEspacial, cfg.Nx))));
        int n = lado * lado;
        var malla = new double[n][];

        double xMin = ObtenerMin(cfg, variableEspacial, cfg.XMin);
        double xMax = ObtenerMax(cfg, variableEspacial, cfg.XMax);
        double yMin = ObtenerMin(cfg, variableTemporal, cfg.TMin);
        double yMax = ObtenerMax(cfg, variableTemporal, cfg.TMax);
        double dx = (xMax - xMin) / (lado - 1);
        double dy = (yMax - yMin) / (lado - 1);

        for (int i = 0; i < lado; i++)
        {
            for (int j = 0; j < lado; j++)
            {
                int idx = i * lado + j;
                malla[idx] = new double[]
                {
                    xMin + j * dx,
                    yMin + i * dy
                };
            }
        }

        return (malla, lado);
    }

    private static string ResolverVariableEspacial(ConfiguracionProblemaVM cfg)
    {
        if (ContieneVariable(cfg, "x")) return "x";
        return VariablesConfiguradas(cfg).FirstOrDefault(v => !EsTemporal(v)) ?? "x";
    }

    private static string ResolverVariableTemporal(ConfiguracionProblemaVM cfg, string variableEspacial)
    {
        if (ContieneVariable(cfg, "t")) return "t";
        return VariablesConfiguradas(cfg).FirstOrDefault(v => v != variableEspacial) ?? "t";
    }

    private static bool ContieneVariable(ConfiguracionProblemaVM cfg, string v)
    {
        return cfg.Min.ContainsKey(v) || cfg.Max.ContainsKey(v) || cfg.N.ContainsKey(v);
    }

    private static IEnumerable<string> VariablesConfiguradas(ConfiguracionProblemaVM cfg)
    {
        return cfg.Min.Keys
            .Concat(cfg.Max.Keys)
            .Concat(cfg.N.Keys)
            .Distinct(StringComparer.OrdinalIgnoreCase);
    }

    private static bool EsTemporal(string variable) =>
        string.Equals(variable, "t", StringComparison.OrdinalIgnoreCase);

    private static int ObtenerN(ConfiguracionProblemaVM cfg, string variable, int fallback)
    {
        if (cfg.N.TryGetValue(variable, out var n)) return n;
        return fallback;
    }

    private static double ObtenerMin(ConfiguracionProblemaVM cfg, string variable, double fallback)
    {
        if (cfg.Min.TryGetValue(variable, out var v)) return v;
        return fallback;
    }

    private static double ObtenerMax(ConfiguracionProblemaVM cfg, string variable, double fallback)
    {
        if (cfg.Max.TryGetValue(variable, out var v)) return v;
        return fallback;
    }

    private static string ObtenerCondicionInicial(ConfiguracionProblemaVM cfg, string fallback)
    {
        var valor = cfg.CondicionesIniciales
            .Where(kv => !string.IsNullOrWhiteSpace(kv.Value))
            .OrderBy(kv => kv.Key)
            .Select(kv => kv.Value)
            .FirstOrDefault();

        return string.IsNullOrWhiteSpace(valor) ? fallback : valor;
    }

    private static string ObtenerCondicionFrontera(
        ConfiguracionProblemaVM cfg,
        string variableEspacial,
        string extremo,
        string fallback)
    {
        var valor = cfg.CondicionesFrontera
            .Where(kv => !string.IsNullOrWhiteSpace(kv.Value))
            .FirstOrDefault(kv => kv.Key.Contains($"{variableEspacial}{extremo}", StringComparison.OrdinalIgnoreCase))
            .Value;

        if (!string.IsNullOrWhiteSpace(valor)) return valor;

        valor = cfg.CondicionesFrontera
            .Where(kv => !string.IsNullOrWhiteSpace(kv.Value))
            .FirstOrDefault(kv => kv.Key.Contains(extremo, StringComparison.OrdinalIgnoreCase))
            .Value;

        return string.IsNullOrWhiteSpace(valor) ? fallback : valor;
    }

    private static double[] LinSpace(double inicio, double fin, int n)
    {
        var arr = new double[n];
        if (n == 1) { arr[0] = inicio; return arr; }
        double paso = (fin - inicio) / (n - 1);
        for (int i = 0; i < n; i++) arr[i] = inicio + i * paso;
        return arr;
    }

    private static double EvaluarConstante(string expr)
    {
        if (double.TryParse(expr, System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out var v))
            return v;
        return 0.0;
    }

    /// <summary>
    /// Evalúa expresiones simples para condiciones iniciales. Soporta:
    /// - constantes numéricas
    /// - sin(pi*x), sin(k*pi*x), cos(pi*x), cos(k*pi*x)
    /// - polinomios x, x^2, x*(1-x)
    /// Si no reconoce, retorna 0.
    /// </summary>
    private static double EvaluarCondicionInicial(string expr, double x)
    {
        if (string.IsNullOrWhiteSpace(expr)) return 0.0;
        expr = expr.Replace(" ", "").ToLowerInvariant();

        if (double.TryParse(expr, System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out var c))
            return c;

        var sinMatch = System.Text.RegularExpressions.Regex.Match(expr,
            @"^sin\((-?\d*(?:\.\d+)?)\*?pi\*x\)$");
        if (sinMatch.Success)
        {
            var k = string.IsNullOrEmpty(sinMatch.Groups[1].Value) || sinMatch.Groups[1].Value == "-"
                ? (sinMatch.Groups[1].Value == "-" ? -1.0 : 1.0)
                : double.Parse(sinMatch.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
            return Math.Sin(k * Math.PI * x);
        }

        var cosMatch = System.Text.RegularExpressions.Regex.Match(expr,
            @"^cos\((-?\d*(?:\.\d+)?)\*?pi\*x\)$");
        if (cosMatch.Success)
        {
            var k = string.IsNullOrEmpty(cosMatch.Groups[1].Value) || cosMatch.Groups[1].Value == "-"
                ? (cosMatch.Groups[1].Value == "-" ? -1.0 : 1.0)
                : double.Parse(cosMatch.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
            return Math.Cos(k * Math.PI * x);
        }

        if (expr == "x") return x;
        if (expr == "x^2") return x * x;
        if (expr == "x*(1-x)" || expr == "(1-x)*x") return x * (1 - x);

        return 0.0;
    }
}
