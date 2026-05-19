namespace app.Models.ViewModels;

public class ConfiguracionProblemaVM
{
    public Dictionary<string, double> Min { get; set; } = new()
    {
        ["x"] = 0.0,
        ["t"] = 0.0
    };

    public Dictionary<string, double> Max { get; set; } = new()
    {
        ["x"] = 1.0,
        ["t"] = 1.0
    };

    public Dictionary<string, int> N { get; set; } = new()
    {
        ["x"] = 30,
        ["t"] = 30
    };

    public Dictionary<string, string> CondicionesIniciales { get; set; } = new();
    public Dictionary<string, string> CondicionesFrontera { get; set; } = new();

    public string EsquemaTemporal { get; set; } = "Implicito";
    public string AlgoritmoFDM { get; set; } = "BackwardEuler";
    public string TipoElemento { get; set; } = "Cuadrilateral";
    public string AlgoritmoFEM { get; set; } = "Galerkin";

    public double XMin => Min.GetValueOrDefault("x", 0.0);
    public double XMax => Max.GetValueOrDefault("x", 1.0);
    public double TMin => Min.GetValueOrDefault("t", 0.0);
    public double TMax => Max.GetValueOrDefault("t", 1.0);
    public int Nx => N.GetValueOrDefault("x", 30);
    public int Nt => N.GetValueOrDefault("t", 30);

    public string CondicionInicial =>
        CondicionesIniciales.Count > 0
            ? CondicionesIniciales.OrderBy(p => p.Key, StringComparer.Ordinal).First().Value
            : string.Empty;

    public string CondicionFronteraIzq =>
        BuscarFronteraPorExtremo("Min") ?? string.Empty;

    public string CondicionFronteraDer =>
        BuscarFronteraPorExtremo("Max") ?? string.Empty;

    private string? BuscarFronteraPorExtremo(string extremo)
    {
        foreach (var par in CondicionesFrontera.OrderBy(p => p.Key, StringComparer.Ordinal))
        {
            if (par.Key.Contains(extremo, StringComparison.OrdinalIgnoreCase))
                return par.Value;
        }
        return null;
    }
}
