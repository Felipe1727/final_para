namespace app.Models.ViewModels;

public class ResultadoResolucionVM
{
    public string Id { get; set; } = string.Empty;
    public double[][] MallaFDM { get; set; } = [];
    public double[][] MallaFEM { get; set; } = [];
    public double[] EjeX { get; set; } = [];
    public double[] EjeY { get; set; } = [];
    public MetricaVM MetricasFDM { get; set; } = new();
    public MetricaVM MetricasFEM { get; set; } = new();
    public ComparacionVM Comparacion { get; set; } = new();
    public List<RegistroProgresoVM> RegistroProgreso { get; set; } = [];
    public List<EvolucionVM> EvolucionFDM { get; set; } = [];
    public List<EvolucionVM> EvolucionFEM { get; set; } = [];
}
