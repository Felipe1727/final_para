using System.ComponentModel.DataAnnotations;

namespace app.Models.ViewModels;

public class ConfiguracionProblemaVM
{
    // Estructura dinámica. Las claves de Min/Max/N son nombres de variables
    // independientes ("x", "y", "t"); las claves de las condiciones son
    // descriptores humanos ("u(x,0)", "u(xMin)", ...).
    public Dictionary<string, double> Min { get; set; } = new();
    public Dictionary<string, double> Max { get; set; } = new();
    public Dictionary<string, int> N { get; set; } = new();

    public Dictionary<string, string> CondicionesIniciales { get; set; } = new();
    public Dictionary<string, string> CondicionesFrontera { get; set; } = new();

    // Propiedades legacy: proyecciones sobre los diccionarios. Se conservan
    // mientras GeneradorMalla / ServicioResolucion / EcuacionMapper sigan
    // leyéndolas. Al asignarlas se escribe el diccionario subyacente; al
    // leerlas, si el diccionario tiene la clave la usamos, si no devolvemos
    // el default histórico.
    [Range(typeof(double), "-1000", "1000")]
    public double XMin
    {
        get => Min.TryGetValue("x", out var v) ? v : 0.0;
        set => Min["x"] = value;
    }

    [Range(typeof(double), "-1000", "1000")]
    public double XMax
    {
        get => Max.TryGetValue("x", out var v) ? v : 1.0;
        set => Max["x"] = value;
    }

    [Range(typeof(double), "-1000", "1000")]
    public double TMin
    {
        get => Min.TryGetValue("t", out var v) ? v : 0.0;
        set => Min["t"] = value;
    }

    [Range(typeof(double), "-1000", "1000")]
    public double TMax
    {
        get => Max.TryGetValue("t", out var v) ? v : 1.0;
        set => Max["t"] = value;
    }

    [Range(3, 300)]
    public int Nx
    {
        get => N.TryGetValue("x", out var v) ? v : 30;
        set => N["x"] = value;
    }

    [Range(3, 300)]
    public int Nt
    {
        get => N.TryGetValue("t", out var v) ? v : 30;
        set => N["t"] = value;
    }

    private const string KeyCondicionInicial = "u(x,0)";
    private const string KeyFronteraIzq = "u(xMin)";
    private const string KeyFronteraDer = "u(xMax)";

    public string CondicionInicial
    {
        get => CondicionesIniciales.TryGetValue(KeyCondicionInicial, out var v) ? v : "sin(pi*x)";
        set => CondicionesIniciales[KeyCondicionInicial] = value;
    }

    public string CondicionFronteraIzq
    {
        get => CondicionesFrontera.TryGetValue(KeyFronteraIzq, out var v) ? v : "0";
        set => CondicionesFrontera[KeyFronteraIzq] = value;
    }

    public string CondicionFronteraDer
    {
        get => CondicionesFrontera.TryGetValue(KeyFronteraDer, out var v) ? v : "0";
        set => CondicionesFrontera[KeyFronteraDer] = value;
    }

    public string AlgoritmoFDM { get; set; } = "BackwardEuler";
    public string EsquemaTemporal { get; set; } = "Implicito";
    public string AlgoritmoFEM { get; set; } = "Galerkin";
    public string TipoElemento { get; set; } = "Cuadrilateral";
}
