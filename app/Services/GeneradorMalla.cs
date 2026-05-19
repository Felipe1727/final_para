using app.Models.ViewModels;
using final_para.Ecuaciones;
using final_para.Servicios;

namespace app.Services;

/// <summary>
/// Construye mallas jagged double[][] con la convención que esperan FDM y FEM en la librería.
///
/// API dinámica (Unit 5):
/// - Detecta las dos variables independientes ordenadas alfabéticamente: primero la
///   espacial menor (típicamente "x"), luego la segunda (sea "t" o "y").
/// - Lee Min/Max/N por nombre de variable desde `ConfiguracionProblemaVM`.
/// - Siembra C.F. y C.I. usando `EvaluadorExpresion` sobre cada nodo frontera/inicial.
///
/// Para FDM: malla 2D N[var1] × N[var2] donde cada celda guarda u en ese nodo.
/// Para FEM: malla cuadrada lado² × 2 donde cada fila son las coordenadas (var1, var2).
/// </summary>
public class GeneradorMalla
{
    public (double[][] mallaFDM, double[] ejeVar1, double[] ejeVar2, string nombreEje2)
        ConstruirMallaFDM(ConfiguracionProblemaVM cfg, Ecuacion ecuacion)
    {
        // Asegura que los diccionarios estén poblados (compat con form legacy).
        cfg.SincronizarDesdeLegacy(ecuacion.VariablesIndependientes);

        var (var1, var2) = DetectarVariables(ecuacion);

        double min1 = cfg.Min.GetValueOrDefault(var1, 0.0);
        double max1 = cfg.Max.GetValueOrDefault(var1, 1.0);
        int n1 = cfg.N.GetValueOrDefault(var1, 30);

        double min2 = cfg.Min.GetValueOrDefault(var2, 0.0);
        double max2 = cfg.Max.GetValueOrDefault(var2, 1.0);
        int n2 = cfg.N.GetValueOrDefault(var2, 30);

        var ejeVar1 = LinSpace(min1, max1, n1);
        var ejeVar2 = LinSpace(min2, max2, n2);

        // Inicializa malla en ceros: double[N[var1]][N[var2]].
        var malla = new double[n1][];
        for (int i = 0; i < n1; i++)
            malla[i] = new double[n2];

        // Sembrar C.F. de la variable 1 (espacial, asumido "x" o similar): extremos i=0, i=n1-1.
        SembrarFronteraVar1(malla, cfg, var1, var2, ejeVar1, ejeVar2, n1, n2);

        // Sembrar C.F. de la variable 2 si NO es temporal (Laplace 2D: y también espacial).
        // Si var2 == "t", las C.F. en var2 no aplican (las C.I. sí, ver abajo).
        if (var2 != "t")
            SembrarFronteraVar2(malla, cfg, var1, var2, ejeVar1, ejeVar2, n1, n2);

        // Si la 2ª variable es temporal: sembrar C.I. en j=0 sobre cada nodo en i.
        if (var2 == "t")
            SembrarCondicionInicial(malla, cfg, var1, ejeVar1, ejeVar2, n1);

        return (malla, ejeVar1, ejeVar2, var2);
    }

    /// <summary>
    /// Malla FEM como lista de nodos (var1, var2) en cuadrícula cuadrada.
    /// El lado se ajusta al cuadrado perfecto más cercano a N[var1] para satisfacer EsMallaCuadrada.
    /// </summary>
    public (double[][] mallaFEM, int lado, string nombreEje2)
        ConstruirMallaFEM(ConfiguracionProblemaVM cfg, Ecuacion ecuacion)
    {
        cfg.SincronizarDesdeLegacy(ecuacion.VariablesIndependientes);

        var (var1, var2) = DetectarVariables(ecuacion);

        double min1 = cfg.Min.GetValueOrDefault(var1, 0.0);
        double max1 = cfg.Max.GetValueOrDefault(var1, 1.0);
        int n1 = cfg.N.GetValueOrDefault(var1, 30);

        double min2 = cfg.Min.GetValueOrDefault(var2, 0.0);
        double max2 = cfg.Max.GetValueOrDefault(var2, 1.0);

        int lado = Math.Max(3, (int)Math.Round(Math.Sqrt(n1)));
        int n = lado * lado;
        var malla = new double[n][];

        double d1 = (max1 - min1) / (lado - 1);
        double d2 = (max2 - min2) / (lado - 1);

        for (int i = 0; i < lado; i++)
        {
            for (int j = 0; j < lado; j++)
            {
                int idx = i * lado + j;
                malla[idx] = new double[]
                {
                    min1 + j * d1,
                    min2 + i * d2
                };
            }
        }

        return (malla, lado, var2);
    }

    // ---- Detección de variables ----

    // Devuelve (var1, var2) ordenadas: primero la espacial alfabéticamente menor,
    // luego la otra (sea "t" o "y"). Si hay 1 sola variable, var2 = "t" por defecto.
    private static (string var1, string var2) DetectarVariables(Ecuacion ecuacion)
    {
        var vars = (ecuacion.VariablesIndependientes ?? Array.Empty<string>())
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Distinct()
            .ToArray();

        if (vars.Length == 0)
            return ("x", "t");

        if (vars.Length == 1)
        {
            // Si solo hay una, asumir x + t para no quebrar la malla 2D.
            return (vars[0], vars[0] == "t" ? "x" : "t");
        }

        // Espaciales = todas las que NO son "t".
        var espaciales = vars.Where(v => v != "t").OrderBy(v => v, StringComparer.Ordinal).ToArray();
        if (espaciales.Length == 0)
        {
            // Caso degenerado: solo "t" y otra ; usar orden alfabético.
            Array.Sort(vars, StringComparer.Ordinal);
            return (vars[0], vars[1]);
        }

        string var1 = espaciales[0]; // típicamente "x"
        // Segunda variable: si hay otra espacial, esa; si no, "t".
        string var2;
        if (espaciales.Length >= 2)
            var2 = espaciales[1];
        else if (vars.Contains("t"))
            var2 = "t";
        else
            var2 = vars.First(v => v != var1);

        return (var1, var2);
    }

    // ---- Sembrado de condiciones ----

    // Busca en cfg.CondicionesFrontera una clave que mencione `var` y `extremo` ("Min" o "Max")
    // de forma laxa. Devuelve la expresión o null si no se encontró.
    private static string? BuscarCFrontera(
        ConfiguracionProblemaVM cfg, string variable, string extremo)
    {
        var nombre = variable + extremo; // ej. "xMin", "xMax", "yMax", "tMin"
        foreach (var par in cfg.CondicionesFrontera)
        {
            if (par.Key.Contains(nombre, StringComparison.OrdinalIgnoreCase))
                return par.Value;
        }
        return null;
    }

    // Busca la C.I. (única o por orden) en cfg.CondicionesIniciales. Acepta varias claves
    // como "u(x,0)", "u(*,0)", o cualquier descriptor que contenga ",0)".
    private static string? BuscarCInicial(ConfiguracionProblemaVM cfg)
    {
        if (cfg.CondicionesIniciales.Count == 0)
            return null;

        foreach (var par in cfg.CondicionesIniciales)
        {
            if (par.Key.Contains(",0)", StringComparison.OrdinalIgnoreCase))
                return par.Value;
        }
        // Fallback: primera entrada en orden alfabético.
        return cfg.CondicionesIniciales.OrderBy(p => p.Key, StringComparer.Ordinal).First().Value;
    }

    private static void SembrarFronteraVar1(
        double[][] malla, ConfiguracionProblemaVM cfg,
        string var1, string var2, double[] ejeVar1, double[] ejeVar2, int n1, int n2)
    {
        string? exprMin = BuscarCFrontera(cfg, var1, "Min");
        string? exprMax = BuscarCFrontera(cfg, var1, "Max");

        for (int j = 0; j < n2; j++)
        {
            var vars = new Dictionary<string, double>
            {
                [var1] = ejeVar1[0],
                [var2] = ejeVar2[j]
            };
            if (exprMin != null)
                malla[0][j] = EvaluadorExpresion.Evaluar(exprMin, vars);

            vars[var1] = ejeVar1[n1 - 1];
            if (exprMax != null)
                malla[n1 - 1][j] = EvaluadorExpresion.Evaluar(exprMax, vars);
        }
    }

    private static void SembrarFronteraVar2(
        double[][] malla, ConfiguracionProblemaVM cfg,
        string var1, string var2, double[] ejeVar1, double[] ejeVar2, int n1, int n2)
    {
        string? exprMin = BuscarCFrontera(cfg, var2, "Min");
        string? exprMax = BuscarCFrontera(cfg, var2, "Max");

        for (int i = 0; i < n1; i++)
        {
            var vars = new Dictionary<string, double>
            {
                [var1] = ejeVar1[i],
                [var2] = ejeVar2[0]
            };
            if (exprMin != null)
                malla[i][0] = EvaluadorExpresion.Evaluar(exprMin, vars);

            vars[var2] = ejeVar2[n2 - 1];
            if (exprMax != null)
                malla[i][n2 - 1] = EvaluadorExpresion.Evaluar(exprMax, vars);
        }
    }

    private static void SembrarCondicionInicial(
        double[][] malla, ConfiguracionProblemaVM cfg,
        string var1, double[] ejeVar1, double[] ejeVar2, int n1)
    {
        string? expr = BuscarCInicial(cfg);
        if (expr == null) return;

        for (int i = 0; i < n1; i++)
        {
            var vars = new Dictionary<string, double>
            {
                [var1] = ejeVar1[i],
                ["t"] = ejeVar2[0]
            };
            malla[i][0] = EvaluadorExpresion.Evaluar(expr, vars);
        }
    }

    private static double[] LinSpace(double inicio, double fin, int n)
    {
        var arr = new double[n];
        if (n == 1) { arr[0] = inicio; return arr; }
        double paso = (fin - inicio) / (n - 1);
        for (int i = 0; i < n; i++) arr[i] = inicio + i * paso;
        return arr;
    }
}
