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
        int nx = cfg.Nx;
        int nt = cfg.Nt;

        var ejeX = LinSpace(cfg.XMin, cfg.XMax, nx);
        var ejeT = LinSpace(cfg.TMin, cfg.TMax, nt);

        var malla = new double[nx][];
        double valIzq = EvaluarConstante(cfg.CondicionFronteraIzq);
        double valDer = EvaluarConstante(cfg.CondicionFronteraDer);

        for (int i = 0; i < nx; i++)
        {
            malla[i] = new double[nt];
            double valorInicial = EvaluarCondicionInicial(cfg.CondicionInicial, ejeX[i]);
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
        int lado = Math.Max(3, (int)Math.Round(Math.Sqrt(cfg.Nx)));
        int n = lado * lado;
        var malla = new double[n][];

        double dx = (cfg.XMax - cfg.XMin) / (lado - 1);
        double dy = (cfg.TMax - cfg.TMin) / (lado - 1);

        for (int i = 0; i < lado; i++)
        {
            for (int j = 0; j < lado; j++)
            {
                int idx = i * lado + j;
                malla[idx] = new double[]
                {
                    cfg.XMin + j * dx,
                    cfg.TMin + i * dy
                };
            }
        }

        return (malla, lado);
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
