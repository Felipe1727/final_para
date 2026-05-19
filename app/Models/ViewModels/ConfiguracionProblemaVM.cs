using System.ComponentModel.DataAnnotations;

namespace app.Models.ViewModels;

public class ConfiguracionProblemaVM
{
    // ---- Campos legacy (mantienen compat con el formulario actual) ----
    [Range(typeof(double), "-1000", "1000")]
    public double XMin { get; set; } = 0.0;

    [Range(typeof(double), "-1000", "1000")]
    public double XMax { get; set; } = 1.0;

    [Range(typeof(double), "-1000", "1000")]
    public double TMin { get; set; } = 0.0;

    [Range(typeof(double), "-1000", "1000")]
    public double TMax { get; set; } = 1.0;

    [Range(3, 300)]
    public int Nx { get; set; } = 30;

    [Range(3, 300)]
    public int Nt { get; set; } = 30;

    public string CondicionInicial { get; set; } = "sin(pi*x)";
    public string CondicionFronteraIzq { get; set; } = "0";
    public string CondicionFronteraDer { get; set; } = "0";

    public string AlgoritmoFDM { get; set; } = "BackwardEuler";
    public string EsquemaTemporal { get; set; } = "Implicito";
    public string AlgoritmoFEM { get; set; } = "Galerkin";
    public string TipoElemento { get; set; } = "Cuadrilateral";

    // ---- API dinámica (contrato compartido Unit 3) ----
    // Diccionarios por nombre de variable independiente.
    // Si están vacíos, los accesores legacy (XMin/XMax/Nx/TMin/TMax/Nt/...) los
    // alimentan automáticamente vía SincronizarDiccionarios() cuando GeneradorMalla lo necesita.
    public Dictionary<string, double> Min { get; set; } = new();
    public Dictionary<string, double> Max { get; set; } = new();
    public Dictionary<string, int> N { get; set; } = new();

    // Clave = descriptor humano legible.
    //   C.I. (solo si hay variable temporal): "u(x,0)", "u_t(x,0)", ...
    //   C.F. espacial: "u(XMin,*)", "u(XMax,*)" para cada variable espacial.
    public Dictionary<string, string> CondicionesIniciales { get; set; } = new();
    public Dictionary<string, string> CondicionesFrontera { get; set; } = new();

    // Sincroniza los diccionarios con los campos legacy si están vacíos.
    // Llamado por GeneradorMalla / EcuacionMapper para no exigir migración inmediata
    // del formulario antiguo.
    public void SincronizarDesdeLegacy(IEnumerable<string> variablesIndependientes)
    {
        var (var1, var2) = DetectarVariables(variablesIndependientes);

        if (!Min.ContainsKey(var1)) Min[var1] = XMin;
        if (!Max.ContainsKey(var1)) Max[var1] = XMax;
        if (!N.ContainsKey(var1)) N[var1] = Nx;

        if (!Min.ContainsKey(var2)) Min[var2] = TMin;
        if (!Max.ContainsKey(var2)) Max[var2] = TMax;
        if (!N.ContainsKey(var2)) N[var2] = Nt;

        if (CondicionesIniciales.Count == 0 && !string.IsNullOrWhiteSpace(CondicionInicial))
        {
            CondicionesIniciales[$"u({var1},0)"] = CondicionInicial;
        }

        if (CondicionesFrontera.Count == 0)
        {
            CondicionesFrontera[$"u({var1}Min,*)"] = CondicionFronteraIzq ?? "0";
            CondicionesFrontera[$"u({var1}Max,*)"] = CondicionFronteraDer ?? "0";
        }
    }

    private static (string var1, string var2) DetectarVariables(IEnumerable<string> variablesIndependientes)
    {
        var vars = variablesIndependientes
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Distinct()
            .ToArray();

        if (vars.Length == 0)
            return ("x", "t");

        if (vars.Length == 1)
            return (vars[0], vars[0] == "t" ? "x" : "t");

        var espaciales = vars.Where(v => v != "t").OrderBy(v => v, StringComparer.Ordinal).ToArray();
        if (espaciales.Length == 0)
        {
            Array.Sort(vars, StringComparer.Ordinal);
            return (vars[0], vars[1]);
        }

        var primera = espaciales[0];
        var segunda = espaciales.Length >= 2
            ? espaciales[1]
            : vars.Contains("t")
                ? "t"
                : vars.First(v => v != primera);

        return (primera, segunda);
    }
}
