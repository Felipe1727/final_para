using System.ComponentModel.DataAnnotations;

namespace app.Models.ViewModels;

public class ConfiguracionProblemaVM
{
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
}
