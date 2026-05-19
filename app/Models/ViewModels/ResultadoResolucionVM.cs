namespace app.Models.ViewModels;

public class ResultadoResolucionVM
{
    public string Id { get; set; } = string.Empty;
    public double[][] MallaFDM { get; set; } = [];
    public double[][] MallaFEM { get; set; } = [];
    public double[] EjeX { get; set; } = [];
    public double[] EjeY { get; set; } = [];
    // Nombre de la 2ª variable independiente (ej. "t", "y", "z"). Usado por el
    // frontend para etiquetar el eje. Default "t" por compat con la convención previa.
    public string NombreEje2 { get; set; } = "t";
    public MetricaVM MetricasFDM { get; set; } = new();
    public MetricaVM MetricasFEM { get; set; } = new();
    public ComparacionVM Comparacion { get; set; } = new();
}
