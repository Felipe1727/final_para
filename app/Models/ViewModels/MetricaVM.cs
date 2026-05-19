namespace app.Models.ViewModels;

public class MetricaVM
{
    public string MetodoNombre { get; set; } = string.Empty;
    public double TiempoSegundos { get; set; }
    public double Residuo { get; set; }
    public double Error { get; set; }
    public uint NumIteraciones { get; set; }
    public int TamanoMalla { get; set; }
}
